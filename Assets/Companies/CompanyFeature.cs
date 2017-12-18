using System;
using UnityEngine;

/// <summary>
/// A CompanyFeatures enables or forbids a GameDevCompany to perform certain actions.
/// </summary>
[Serializable]
public class CompanyFeature {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private bool enabled;
    public bool Enabled {
        get { return enabled; }
        set { enabled = value; }
    }

    public CompanyFeature(string name, bool enabled) {
        this.name = name;
        this.enabled = enabled;
    }
}
