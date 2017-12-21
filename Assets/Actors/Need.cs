using System;
using UnityEngine;

[Serializable]
public class Need {
    [SerializeField] private string label;
    public string Label => label;

    [SerializeField] private int level;
    public int Level => level;

    public Need(string label, int level) {
        this.label = label;
        this.level = level;
    }
}
