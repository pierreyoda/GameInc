using System;
using UnityEngine;

public abstract class Project {
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
}