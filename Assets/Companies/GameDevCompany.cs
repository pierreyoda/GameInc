using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Linq;
using Database;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class GameDevCompany : MonoBehaviour {
    public static string[] SUPPORTED_FEATURES = {
        "Engine.CanDevelop",
    };

    [SerializeField] private CompanyFeature[] features = new CompanyFeature[0];
    public CompanyFeature[] Features => features;

    [SerializeField] private float money = 0;
    public float Money => money;

    [SerializeField] private bool neverBailedOut = true;
    public bool NeverBailedOut {
        get { return neverBailedOut; }
        set { neverBailedOut = value; }
    }

    [SerializeField] private string companyName;
    public string CompanyName => companyName;

    [SerializeField] private List<Employee> employees = new List<Employee>();
    public IReadOnlyList<Employee> Employees => employees.AsReadOnly();

    [SerializeField] private Transform employeesParentObject;

    [SerializeField] private Staff employeesManager;
    public Staff EmployeesManager => employeesManager;

    [SerializeField] private DateTime hiringEndingDate = new DateTime(0, 0, 0);
    [SerializeField] private Action<List<Employee>> onHiringEnded;

    [SerializeField] private int fans = 0;
    public int Fans => fans;

    [SerializeField] private List<string> allowedEngineFeatures = new List<string>();
    public IReadOnlyList<string> AllowedEngineFeatures => allowedEngineFeatures.AsReadOnly();

    [SerializeField] private List<GameEngine> gameEngines = new List<GameEngine>();
    public IReadOnlyList<GameEngine> GameEngines => gameEngines.AsReadOnly();

    [SerializeField] private Project currentProject;
    public Project CurrentProject => currentProject;

    [SerializeField] private ProjectsBacklog completedProjects = new ProjectsBacklog();
    public ProjectsBacklog CompletedProjects => completedProjects;

    public GameDevCompany(string companyName) {
        this.companyName = companyName;
    }

    private void Start() {
        List<CompanyFeature> features = new List<CompanyFeature>();
        foreach (string featureName in SUPPORTED_FEATURES) {
            features.Add(new CompanyFeature(featureName, false));
        }
        this.features = features.ToArray();
    }

    public void Init(Database.Database.DatabaseCollection<Skill> skills) {
        employeesManager.InitSkillsTypes(skills);
    }

    public void StartProject(Project project) {
        if (currentProject != null) {
            Debug.LogWarning("GameDevCompany.StartProject : another project is already in progress.");
            return;
        }
        Debug.Log($"GameDevCompany : started a new {project.Type()} named \"{project.Name}\".");
        currentProject = project;
    }

    public void CompleteCurrentProject() {
        if (currentProject == null) {
            Debug.LogWarning("GameDevCompany.CompleteCurrentProject : no current project.");
            return;
        }
        currentProject.AddCompletion(100);
        completedProjects.AddCompletedProject(currentProject);
        currentProject = null;
    }

    public void SetFeature(string featureName, bool featureEnabled) {
        CompanyFeature feature = features.FirstOrDefault(f => f.Name == featureName);
        if (feature == null) {
            Debug.LogError($"GameDevCompany.SetFeature(\"{featureName}\", {featureEnabled}) : unkown feature.");
            return;
        }
        feature.Enabled = featureEnabled;
        Debug.Log($"GameDevCompany.Feature(\"{featureName}\") = {featureEnabled}.");
    }

    public void AllowEngineFeature(string featureId) {
        if (allowedEngineFeatures.Contains(featureId)) {
            Debug.LogWarning($"GameDevCompany.AllowEngineFeature(\"{featureId}\") : engine feature already allowed.");
            return;
        }
        allowedEngineFeatures.Add(featureId);
    }

    public void AddEmployee(Employee employee) {
        employees.Add(employee);
        employee.transform.parent = employeesParentObject;
    }

    public void StartHiring(DateTime currentDate, HiringMethod method,
        Action<List<Employee>> callback) {
        onHiringEnded = callback;

        int candidatesNumber = Random.Range(method.CandidatesDistribution.Item1,
            method.CandidatesDistribution.Item2 + 1); // upper bound is inclusive by convention
        if (candidatesNumber <= 0) {
            Debug.LogWarning($"GameDevCompany.StartHiring : no candidates with Method \"{method.Name}\".");
            callback(null);
        }
        List<Employee> candidates = new List<Employee>();
        for (int i = 0; i < candidatesNumber; i++) {
            candidates.Add(GenerateCandidate(method));
        }

        hiringEndingDate = currentDate.AddDays(method.DurationInDays);
    }

    private Employee GenerateCandidate(HiringMethod method) {
        foreach (var skillDistribution in method.SkillsDistribution) {

        }
        return null;
    }

    private void FinishHiring() {
        onHiringEnded(null);
    }

    public void AddGameEngine(GameEngine gameEngine) {
        foreach (GameEngine existingEngine in gameEngines) {
            if (existingEngine.Name == gameEngine.Name) {
                Debug.LogWarning($"GameDevCompany.AddGameEngine : engine \"{gameEngine.Name}\" already exists.");
                return;
            }
        }
        gameEngines.Add(gameEngine);
    }

    public void Charge(float cost) {
        Assert.IsTrue(cost > 0);
        money -= cost;
    }

    public void Pay(float income) {
        Assert.IsTrue(income > 0);
        money += income;
    }

    public void SetMoney(float amount) {
        money = amount;
    }

    public void OnNewDay(DateTime gameDate) {
        if (gameDate == hiringEndingDate) {
            FinishHiring();
        }
    }

    public void OnNewMonth(float rent, float upkeep) {
        Assert.IsTrue(rent >= 0 && upkeep >= 0);
        float salaries = employees.Sum(employee => employee.Salary);
        money -= rent + salaries;
        Debug.Log($"Company.OnNewMonth : rent = {rent}k, upkeep = {upkeep}, salaries = {salaries}k.");
    }

    public GameProject CurrentGame() {
        if (currentProject == null || currentProject.Type() != Project.ProjectType.GameProject) {
            Debug.LogError("GameDevCompany.CurrentGame : no current game project.");
            return null;
        }
        return currentProject as GameProject;
    }
}
