using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

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

    public void StartProject(Project project) {
        if (currentProject != null) {
            Debug.LogWarning("GameDevCompany.StartProject : another project is already in progress.");
            return;
        }
        Debug.Log($"GameDevCompany : started a new Project named \"{project.Name}\".");
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

    public void SetFeature(string name, bool enabled) {
        CompanyFeature feature = features.FirstOrDefault(f => f.Name == name);
        if (feature == null) {
            Debug.LogError($"GameDevCompany.SetFeature(\"{name}\", {enabled}) : unkown feature.");
            return;
        }
        feature.Enabled = enabled;
        Debug.Log($"GameDevCompany.Feature(\"{name}\") = {enabled}.");
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
    }

    public void AddGameEngine(GameEngine gameEngine) {
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

    public void OnNewDay() {
    }

    public void OnNewMonth(float rent) {
        Assert.IsTrue(rent >= 0);
        float salaries = employees.Sum(employee => employee.Salary);
        money -= rent + salaries;
        Debug.Log($"Company.OnNewMonth : monthly rent was {rent}k, monthly salaries was {salaries}k.");
    }

    public GameProject CurrentGame() {
        if (currentProject == null || currentProject.Type() != Project.ProjectType.GameProject) {
            Debug.LogError("GameDevCompany.CurrentGame : no current game project.");
            return null;
        }
        return currentProject as GameProject;
    }
}
