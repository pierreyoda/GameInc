using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public abstract class Project {
    [Serializable]
    public enum ProjectType {
        GameProject,
    }

    [Serializable]
    public class ProjectScore {
        [SerializeField] private string id;
        public string Id => id;

        public float score;

        public ProjectScore(string id, float score) {
            this.id = id;
            this.score = score;
        }
    }

    private static int INSTANCES_COUNT = 0;

    [SerializeField] private int id;
    public int Id => id;

    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private float completion = 0f; // in percents
    public float Completion => completion;

    [SerializeField] private List<ProjectScore> scores = new List<ProjectScore>();
    public List<ProjectScore> Scores => scores;

    [SerializeField] private DateTime startDate;
    public DateTime StartDate => startDate;

    [SerializeField] [Range(1, 10000)] private int durationInDays = 60;

    [SerializeField] private DateTime completionDate;
    public DateTime CompletionDate => completionDate;

    protected Project(string name) {
        id = INSTANCES_COUNT++;
        this.name = name;
    }

    public void StartProject(DateTime gameDate) {
        startDate = gameDate;
        foreach (string skillId in DefaultSkillsForType(Type())) {
            scores.Add(new ProjectScore(skillId, 0));
        }
    }

    public bool OnDateModified(DateTime gameDate) {
        if (completion >= 1f) return true;
        DateTime endDate = startDate.AddDays(durationInDays);
        float deltaInDays = (float) (endDate - gameDate).TotalDays;
        completion = (durationInDays - deltaInDays) / durationInDays;
        if (completion >= 1f) {
            completionDate = gameDate;
            return true;
        }
        return false;
    }

    public abstract ProjectType Type();
    private static string[] DefaultSkillsForType(ProjectType type) {
        switch (type) {
            case ProjectType.GameProject:
                return new[] {
                    "Engine", "Gameplay", "AI",
                    "GameDesign", "Graphics2D", "Graphics3D",
                    "SoundFX", "Soundtrack",
                };
            default: return null;
        }
    }
}
