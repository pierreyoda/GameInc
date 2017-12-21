using System;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public abstract class Project {
    public enum ProjectType {
        GameProject,
    }

    private static int INSTANCES_COUNT = 0;

    [SerializeField] private int id;
    public int Id => id;

    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private int completion = 0;
    public int Completion => completion;

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
}