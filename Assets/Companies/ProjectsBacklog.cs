using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectsBacklog {
    [SerializeField] private List<Project> projects = new List<Project>();
    public List<Project> Projects => projects;

    [SerializeField] private List<GameProject> games = new List<GameProject>();
    public List<GameProject> Games => games;

    public bool AddCompletedProject(Project project) {
        if (project.Completion != 100) {
            Debug.LogError($"ProjectsBacklog.AddPorject : unfinished Project (ID = {project.Id}, name = {project.Name}).");
            return false;
        }

        switch (project.Type()) {
            case Project.ProjectType.GameProject: games.Add(project as GameProject); break;
        }
        projects.Add(project);

        return true;
    }

    public List<GameProject> GamesWithEngineFeature(string engineFeatureId) {
        List<GameProject> gamesWithEngineFeature = new List<GameProject>();

        foreach (GameProject game in games) {
            if (game.Engine.HasFeature(engineFeatureId))
                gamesWithEngineFeature.Add(game);
        }

        return gamesWithEngineFeature;
    }

    public Project FindById(int id) {
        return projects.Find(p => p.Id == id);
    }
}
