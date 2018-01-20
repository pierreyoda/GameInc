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

    public void ToggleSimulationPause() {
        world.ToggleSimulation();
    }

    public void ToggleSimulationSpeed() {
        world.ToggleSimulationSpeed();
    }

    public void BuildRoom(string roomId) {
        world.BuildNewRoom(roomId);
    }

    public void StartProject() {
        hudController.ShowNewProjectDialog(gameDateTime,
            Project.ProjectType.GameProject, database,
            playerCompany.GameEngines);
    }

    private List<GlobalVariable> GameVariables() {
        return new List<GlobalVariable> {
            // World
            new GlobalVariable("World.CurrentDate", SymbolType.Date,
                c => new DateSymbol(c.D())),
            new GlobalVariable("World.CurrentDate.Year", SymbolType.Integer,
            c => new IntegerSymbol(c.D().Year)),
            new GlobalVariable("World.CurrentDate.Month", SymbolType.Integer,
                c => new IntegerSymbol(c.D().Month)),
            new GlobalVariable("World.CurrentDate.Day", SymbolType.Integer,
                c => new IntegerSymbol(c.D().Day)),
            new GlobalVariable("World.CurrentDate.DayOfWeek", SymbolType.Integer,
                c => new IntegerSymbol((int) c.D().DayOfWeek)),
            // Company
            new GlobalVariable("Company.Money", SymbolType.Float,
                c => new FloatSymbol(c.C().Money)),
            new GlobalVariable("Company.NeverBailedOut", SymbolType.Boolean,
                c => new BooleanSymbol(c.C().NeverBailedOut)),
            new GlobalVariable("Company.Projects.CompletedGames.Count", SymbolType.Integer,
                c => new IntegerSymbol(c.C().CompletedProjects.Games.Count)),
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
        // set initial global variables values
        foreach (GlobalVariable globalVariable in scriptGlobalVariables) {
            ISymbol value = globalVariable.FromContext(this);
            if (value == null) {
                Debug.LogError($"WorldController.OnGameStarted : error while evaluating global variable \"${globalVariable.Name}\".");
                continue;
            }
            if (value.Type() != globalVariable.Type) {
                Debug.LogError($"WorldController.OnGameStarted : \"${globalVariable.Name}\" must be of type {globalVariable.Type} instead of {value.Type()}.");
                continue;
            }
            globalVariable.Value = value;
        }
        // additional local variables
        Assert.IsTrue(ScriptContext.AddLocalVariable(this,
            "Employee.HiringCost", new FloatSymbol(0)));
        Assert.IsTrue(ScriptContext.AddLocalVariable(this,
            "Employee.Salary", new FloatSymbol(0)));
        // parser context
        ParserContext parserContext = new ParserContext {
            Grammar = Grammar.DefaultGrammar(),
            LocalVariables = scriptVariables,
            GlobalVariables = scriptGlobalVariables,
            Functions = scriptFunctions,
        };

        // test
        Assert.IsTrue(ScriptContext.AddLocalVariable(this,
            "testVariable", new FloatSymbol(0f)));
        string script = @"
            let alpha : int = 3;
            let beta : int = (alpha + 7) / 2;
            let gamma : int = ToInt('4') + ToInt(-2.0);
            let str : string = 'beta=' + ToString(beta) + '; gamma=' + ToString(gamma) + ';'
        ";
        Executable executable = Executable.FromScript(script, parserContext);
        string scriptResult;
        executable.ExecuteExpecting(this, out scriptResult);
        Debug.LogWarning("===> executable result = " + scriptResult);

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

    public void OnDateModified(DateTime gameDateTime) {
        this.gameDateTime = gameDateTime;
        // World Events
        List<WorldEvent> triggeredEvents = eventsController.OnGameDateChanged(this);
        foreach (WorldEvent triggeredEvent in triggeredEvents) {
            hudController.OnEventTriggered(triggeredEvent);
        }
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

    public List<LocalVariable> LocalVariables() {
        return scriptVariables;
    }

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

        bool assigned = false;
        switch (variableName) {
            case "Company.Money":
                assigned = true;
                playerCompany.SetMoney((value as FloatSymbol).Value);
                break;
            case "Company.NeverBailedOut":
                assigned = true;
                playerCompany.NeverBailedOut = (value as BooleanSymbol).Value;
                break;
        }
        if (!assigned) {
            Debug.LogError($"WorldController.GetGlobalVariable(\"{variableName}\") : illegal assignment.");
            return false;
        }
        globalVariable.Value = value;
        return true;
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
