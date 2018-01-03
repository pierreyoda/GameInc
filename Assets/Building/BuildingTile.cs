using System;
using UnityEngine;

[Serializable]
public class BuildingTile : MonoBehaviour {
    [SerializeField] private bool empty = true;
    public bool Empty {
        get { return empty; }
        set { empty = value; }
    }
}
