using System;
using UnityEngine;

namespace Database {

/// <summary>
/// A room in a Building.
/// </summary>
[Serializable]
public class Room : DatabaseElement {
    [SerializeField] private string textureName;
    public string TextureName => textureName;

    [SerializeField] private int width;
    public int Width => width;

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

    public Room(string id, string name, string textureName, float cost,
        int constructionTime, float upkeep) : base(name, id) {
        this.textureName = textureName;
        this.cost = cost;
        this.constructionTime = constructionTime;
        this.upkeep = upkeep;
    }
}

}