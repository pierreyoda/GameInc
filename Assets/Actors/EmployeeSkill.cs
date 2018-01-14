using System;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class EmployeeSkill {
    [SerializeField] private string id;
    public string Id => id;

    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private float proficiency;
    public float Proficiency => proficiency;

    public EmployeeSkill(string id, string name, float proficiency) {
        Assert.IsTrue(proficiency >= 0);
        this.id = id;
        this.name = name;
        this.proficiency = proficiency;
    }
}
