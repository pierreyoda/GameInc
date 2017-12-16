using System;
using UnityEngine;

/// <summary>
/// A gaming platform.
/// </summary>
[Serializable]
public class Platform : DatabaseElement {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private string manufacturerName;
    public string ManufacturerName => manufacturerName;

    [SerializeField] private string releaseDate;
    public DateTime ReleaseDate => ParseDate(releaseDate);

    public Platform(string id, string name, string manufacturerName,
            string releaseDate) : base(id) {
        this.name = name;
        this.manufacturerName = manufacturerName;
        this.releaseDate= releaseDate;
    }
}
