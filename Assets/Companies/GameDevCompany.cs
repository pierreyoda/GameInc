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

    [SerializeField] private ProjectsBacklog completedProjects = new ProjectsBacklog();
    public ProjectsBacklog CompletedProjects => completedProjects;

    [SerializeField] private float money = 0;
    public float Money => money;

    [SerializeField] private bool neverBailedOut = true;
    public bool NeverBailedOut {
        get { return neverBailedOut; }
        set { neverBailedOut = value; }
    }

    [SerializeField] private string companyName;
    public string CompanyName => companyName;

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

    public void SetFeature(string name, bool enabled) {
        CompanyFeature feature = features.FirstOrDefault(f => f.Name == name);
        if (feature == null) {
            Debug.LogError($"GameDevCompany.SetFeature(\"{name}\", {enabled}) : unkown feature.");
            return;
        }
        feature.Enabled = enabled;
        Debug.Log($"GameDevCompany.Feature(\"{name}\") = {enabled}.");
    }

    public void Charge(float cost) {
        Assert.IsTrue(cost > 0);
        money -= cost;
    }

    public void Pay(float income) {
        Assert.IsTrue(income > 0);
        money += income;
    }
}
