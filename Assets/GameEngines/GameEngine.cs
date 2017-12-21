using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class GameEngine {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private string[] supportedPlatformsIDs;
    public string[] SupportedPlatformsIDs => supportedPlatformsIDs;

    [SerializeField] private List<string> supportedFeaturesIDs = new List<string>();
    public IReadOnlyList<string> SupportedFeaturesIDs => supportedFeaturesIDs.AsReadOnly();

    public GameEngine(string name, string[] supportedPlatformsIDs) {
        this.name = name;
        this.supportedPlatformsIDs = supportedPlatformsIDs;;
    }

    public void AddFeature(string featureId) {
        supportedFeaturesIDs.Add(featureId);
    }

    public bool HasFeature(string featureId) {
        return SupportedFeaturesIDs.Contains(featureId);
    }
}
