using System;
using System.Collections.Generic;
using Database;
using UnityEngine;

[Serializable]
public class GameProject : Project {
    [SerializeField] private Genre genre;
    public Genre Genre => genre;

    [SerializeField] private Theme theme;
    public Theme Theme => theme;

    [SerializeField] private GameEngine engine;
    public GameEngine Engine => engine;

    [SerializeField] private List<string> platformIDs;
    public IReadOnlyList<string> PlatformIDs => platformIDs.AsReadOnly();

    public GameProject(string name, Genre genre, Theme theme, GameEngine engine,
        List<string> platformIDs)
            : base(name) {
        this.genre = genre;
        this.theme = theme;
        this.engine = engine;
        this.platformIDs = platformIDs;
    }

    public override ProjectType Type() {
        return ProjectType.GameProject;
    }
}
