using System;
using System.Collections.Generic;
using UnityEngine;

namespace Database {

/// <summary>
/// A placeable object that can be added to a room.
/// </summary>
[Serializable]
public class Object : DatabaseElement {
    [SerializeField] private string textureName;
    public string TextureName => textureName;

    /// <summary>
    /// Purchase cost.
    /// </summary>
    [SerializeField] private int cost;
    public int Cost => cost;

    /// <summary>
    /// Monthly upkeep.
    /// </summary>
    [SerializeField] private int upkeep;
    public int Upkeep => upkeep;

    public Object(string id, string name, string textureName, int cost,
        int upkeep) : base(id, name) {
        this.textureName = textureName;
        this.cost = cost;
        this.upkeep = upkeep;
    }
}

}