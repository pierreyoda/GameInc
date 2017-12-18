using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectsBacklog {
    [SerializeField] private List<Project> projects = new List<Project>();
    public List<Project> Projects => projects;

    [SerializeField] private List<GameProject> games = new List<GameProject>();
    public List<GameProject> Games => games;

    public bool AddCompletedGame(GameProject game) {
        if (!AddProject(game)) return false;
        games.Add(game);
        return true;
    }

    public bool AddProject(Project project) {
        if (project.Completion != 100) {
            Debug.LogError($"ProjectsBacklog.AddPorject : unfinished Project (ID = {project.Id}, name = {project.Name}).");
            return false;
        }
        return true;
    }

    public Project FindById(int id) {
        return projects.Find(p => p.Id == id);
    }
}
