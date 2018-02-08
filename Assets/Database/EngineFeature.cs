using System;
using UnityEngine;

namespace Database {

[Serializable]
public class EngineFeature : DatabaseElement {
    private const int MinExpectedYear = 1970;

    [SerializeField] private string descriptionEnglish;
    public string DescriptionEnglish => descriptionEnglish;

    /// <summary>
    /// List of other EngineFeatures' IDs that will be removed from an Engine
    /// when enabling this EngineFeature.
    /// </summary>
    [SerializeField] private string[] disables;
    public string[] Disables => disables;

    [SerializeField] private string requirement;
    public string Requirement => requirement;

    [SerializeField] private string effect;
    public string Effect => effect;

    [SerializeField] private int expectedYear;
    public int ExpectedYear => expectedYear;

    public EngineFeature(string id, string name, string descriptionEnglish,
        string[] disables, string requirement, string effect) : base(id, name) {
        this.descriptionEnglish = descriptionEnglish;
        this.disables = disables;
        this.requirement = requirement;
        this.effect = effect;
    }

    public override bool IsValid() {
        if (expectedYear < MinExpectedYear) {
            Debug.LogError($"EngineFeature with ID = {Id} : invalid expected year {expectedYear}.");
            return false;
        }
        return base.IsValid();
    }
}

}
