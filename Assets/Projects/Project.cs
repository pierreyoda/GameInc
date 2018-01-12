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
        public string id;
        public string name;
        public float score;
    }

    private static int INSTANCES_COUNT = 0;

    [SerializeField] private int id;
    public int Id => id;

    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private int completion = 0;
    public int Completion => completion;

    [SerializeField] private List<ProjectScore> scores = new List<ProjectScore>();
    public List<ProjectScore> Scores => scores;

    [SerializeField] private DateTime startDate;
    public DateTime StartDate => startDate;

    [SerializeField] private DateTime completionDate;
    public DateTime CompletionDate => completionDate;

    protected Project(string name) {
        id = INSTANCES_COUNT++;
        this.name = name;
    }

    public abstract ProjectType Type();

    public void AddCompletion(int added) {
        Assert.IsTrue(added > 0);
        completion += added;
    }

    public float Score(string scoreId) {
        ProjectScore projectScore = scores.Find(s => s.id == scoreId);
        if (projectScore == null) {
            Debug.LogError($"Project.Score({scoreId}) : unkown Skill ID.");
            return 0f;
        }
        return projectScore.score;
    }

    public void ModifyScore(string scoreId, float difference) {
        ProjectScore projectScore = scores.Find(s => s.id == scoreId);
        if (projectScore == null) {
            Debug.LogError($"Project.ModifyScore({scoreId}, {difference}) : unkown Skill ID.");
            return;
        }
        projectScore.score += difference;
    }
}