using System.Collections.Generic;
using UnityEngine;

public class GameEngine {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private string[] supportedPlatformsIDs;
    public string[] SupportedPlatformsIDs => supportedPlatformsIDs;

    public GameEngine(string name, string[] supportedPlatformsIDs) {
        this.name = name;
        this.supportedPlatformsIDs = supportedPlatformsIDs;
    }
}
