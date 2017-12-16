using System;
using UnityEngine;

namespace Database {

/// <summary>
/// A room in a Building.
/// </summary>
[Serializable]
public class Room : DatabaseElement {
    [SerializeField] private string name;
    public string Name => name;

    /// <summary>
    /// Upfront construction cost.
    /// </summary>
    [SerializeField] private float cost;
    public float Cost => cost;

    /// <summary>
    /// Construction time, in days.
    /// </summary>
    [SerializeField] private int constructionTime;
    public int ConstructionTime => constructionTime;

    /// <summary>
    /// Monthly upkeep.
    /// </summary>
    [SerializeField] private float upkeep;
    public float Upkeep => upkeep;

    public Room(string id, string name, float cost, int constructionTime,
        float upkeep) : base(id) {
        this.name = name;
        this.cost = cost;
        this.constructionTime = constructionTime;
        this.upkeep = upkeep;
    }
}

}