using System;
using System.Collections.Generic;
using Database;
using Script;
using UnityEngine;
using UnityEngine.Assertions;

public class WorldController : MonoBehaviour, IScriptContext {
    private Database.Database database;
    private DateTime gameDateTime;
    [SerializeField] private World world;
    [SerializeField] private GameDevCompany playerCompany;
    [SerializeField] private EngineFeaturesController engineFeaturesController;
    [SerializeField] private EventsController eventsController;
    [SerializeField] private NewsController newsController;
    [SerializeField] private GameHudController hudController;

    [Header("Scripting Engine")]
    [SerializeField] private Employee currentEmployee;
    [SerializeField] private List<IFunction> scriptFunctions = new List<IFunction>();
    [SerializeField] private List<LocalVariable> scriptVariables = new List<LocalVariable>();
    [SerializeField] private List<GlobalVariable> scriptGlobalVariables;

    public bool IsSimulationRunning => world.IsSimulationRunning;

    public void SetSimulationStatus(bool running) {
        world.SetSimulationStatus(running);
    }

    public void SetSimulationSpeed(int multiplier) {
        world.SetSimulationSpeed(multiplier);
    }

    public void BuildRoom(string roomId) {
        world.BuildNewRoom(roomId);
    }

    public void StartProject() {
        hudController.ShowNewProjectDialog(gameDateTime,
            Project.ProjectType.GameProject, database,
            playerCompany.GameEngines);
    }

    private static List<GlobalVariable> GameVariables() {
        return new List<GlobalVariable> {
            // World
            new GlobalVariable("World.CurrentDate",
                c => new DateSymbol(c.D()),
                SymbolType.Date),
            new GlobalVariable("World.CurrentDate.Year",
            c => new IntegerSymbol(c.D().Year),
                SymbolType.Integer),
            new GlobalVariable("World.CurrentDate.Month",
                c => new IntegerSymbol(c.D().Month),
                SymbolType.Integer),
            new GlobalVariable("World.CurrentDate.Day",
                c => new IntegerSymbol(c.D().Day),
                SymbolType.Integer),
            new GlobalVariable("World.CurrentDate.DayOfWeek",
                c => new IntegerSymbol((int) c.D().DayOfWeek),
                SymbolType.Integer),
            // Company
            new GlobalVariable("Company.Money",
                c => new FloatSymbol(c.C().Money),
                SymbolType.Float),
            new GlobalVariable("Company.NeverBailedOut",
                c => new BooleanSymbol(c.C().NeverBailedOut),
                SymbolType.Boolean),
            new GlobalVariable("Company.Projects.CompletedGames.Count",
                c => new IntegerSymbol(c.C().CompletedProjects.Games.Count),
                SymbolType.Integer),
        };
    }

    public void OnGameStarted(Database.Database db, DateTime date,
        GameDevCompany playedCompany) {
        database = db;
        playerCompany = playedCompany;
        gameDateTime = date;

        // load script functions
        scriptFunctions = Function<bool>.DefaultFunctions();
        scriptGlobalVariables = GameVariables();
        // additional local variables
        Assert.IsTrue(ScriptContext.AddLocalVariable(this,
            "Employee_HiringCost", new FloatSymbol(0), true));
        Assert.IsTrue(ScriptContext.AddLocalVariable(this,
            "Employee_Salary", new FloatSymbol(0), true));
        // parser context
        ParserContext parserContext = new ParserContext {
            Grammar = Grammar.DefaultGrammar(),
            LocalVariables = scriptVariables,
            GlobalVariables = scriptGlobalVariables,
            Functions = scriptFunctions,
        };

        // test
        const string script = @"
            //{
                let b: int = 2;
            //}
            b
        ";
        Executable executable = Executable.FromScript(script, parserContext);
        if (executable != null) {
            int result;
            executable.ExecuteExpecting(this, out result);
            Debug.LogWarning($"===> executable result = {result}");
        }

        // scripts parsing
        eventsController.CreateEvents(db.Events.Collection, parserContext);
        playerCompany.Init(database.Skills, parserContext);
        engineFeaturesController.CreateFeatures(db.EngineFeatures.Collection,
            parserContext);
        engineFeaturesController.CheckFeatures(this);
        newsController.CreateNews(db.News.Collection, date);

        // events OnInit calls
        Assert.IsTrue(eventsController.InitEvents(this));

        float hiringCost;
        Employee employee = playedCompany.EmployeesManager.GenerateRandomEmployee(
            this,
            db.HiringMethod.FindById("CompSciGraduates"),
            db.Names.FindById("CommonNamesUSA"),
            db.Skills,
            out hiringCost);
        playedCompany.AddEmployee(employee);
        Debug.Log($"Generated Random Employee : hiring cost = {hiringCost}.");
    }

    public void OnSimulationStarted() {
        hudController.OnSimulationStarted();
    }

    public void OnDateModified(DateTime gameDateTime) {
        this.gameDateTime = gameDateTime;
        // World Events
        eventsController.OnGameDateChanged(this);
        // World News
        News latestNews = newsController.OnGameDateChanged(gameDateTime);
        if (latestNews != null)
            hudController.PushLatestNews(latestNews);
        // HUD
        hudController.OnDateChanged(gameDateTime);
    }

    public void OnPlayerCompanyModified() {
        eventsController.OnPlayerCompanyChanged(this);
        hudController.OnCompanyChanged(playerCompany);
    }

    public void OnProjectStarted(Project newProject) {
        playerCompany.StartProject(newProject, gameDateTime);
        hudController.CanStartNewProject(false);
    }

    public void OnProjectCompleted(GameDevCompany company, Project project) {
        if (project.Type() == Project.ProjectType.GameProject)
            engineFeaturesController.CheckFeatures(this);
        hudController.CanStartNewProject(true);
    }

    public void OnConstructionStarted(float constructionCost) {
        world.OnConstructionStarted(constructionCost);
    }

    public int LoopsMaximumIterations() => 1000;

    public List<IFunction> Functions() => scriptFunctions;
    public List<LocalVariable> LocalVariables() => scriptVariables;

    public bool SetGlobalVariable(string variableName, ISymbol value) {
        GlobalVariable globalVariable = scriptGlobalVariables.Find(gv => gv.Name == variableName);
        if (globalVariable == null) {
            Debug.LogError($"WorldController.GetGlobalVariable(\"{variableName}\") : unkown global variable.");
            return false;
        }
        if (value.Type() != globalVariable.Type) {
            Debug.LogError($"WorldController.GetGlobalVariable(\"{variableName}\") : type mismatch " +
                           $"({value.Type()} instead of {globalVariable.Type}).");
            return false;
        }
        if (value.Type() == SymbolType.Array &&
            value.ArrayType() != globalVariable.ArrayType) {
            Debug.LogError($"WorldController.GetGlobalVariable(\"{variableName}\") : array type mismatch " +
                           $"({value.ArrayType()} instead of {globalVariable.ArrayType}).");
            return false;
        }

        bool assigned = true;
        switch (variableName) {
            case "Company.Money":
                playerCompany.SetMoney(((FloatSymbol) value).Value);
                break;
            case "Company.NeverBailedOut":
                playerCompany.NeverBailedOut = ((BooleanSymbol) value).Value;
                break;
            default:
                assigned = false;
                break;
        }
        if (assigned) return true;
        Debug.LogError($"WorldController.GetGlobalVariable(\"{variableName}\") : illegal assignment.");
        return false;
    }

    public DateTime D() {
        return gameDateTime;
    }
    public GameDevCompany C() {
        return playerCompany;
    }

    public Employee CurrentEmployee() { return currentEmployee; }
    public void SetCurrentEmployee(Employee e) { currentEmployee = e; }
}
