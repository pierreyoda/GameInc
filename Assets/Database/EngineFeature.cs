using System;
using UnityEngine;

namespace Database {

[Serializable]
public class EngineFeature : DatabaseElement {
    [SerializeField] private string descriptionEnglish;
    public string DescriptionEnglish => descriptionEnglish;

    /// <summary>
    /// List of other EngineFeatures' IDs that will be removed from an Engine
    /// when enabling this EngineFeature.
    /// </summary>
    [SerializeField] private string[] disables;
    public string[] Disables => disables;

    [SerializeField] private string[] requirements;
    public string[] Requirements => requirements;

    [SerializeField] private string[] effects;
    public string[] Effects => effects;

    public EngineFeature(string id, string name, string descriptionEnglish,
        string[] disables, string[] requirements, string[] effects) : base(id, name) {
        this.descriptionEnglish = descriptionEnglish;
        this.disables = disables;
        this.requirements = requirements;
        this.effects = effects;
    }
}

}
