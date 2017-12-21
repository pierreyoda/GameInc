using System;
using UnityEngine;

[Serializable]
public class Skill {
    [SerializeField] private int id;
    public int Id => id;

    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private int proficiency;
    public int Proficiency => proficiency;
}
