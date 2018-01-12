using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Database {

/// <summary>
/// Common Names database for a specific country, region or culture.
/// </summary>
[Serializable]
public class Names : DatabaseElement {
    [SerializeField] private string[] firstNamesMale;
    [SerializeField] private string[] firstNamesFemale;
    [SerializeField] private string[] lastNames;

    public Names(string id, string name, string[] firstNamesMale,
        string[] firstNamesFemale, string[] lastNames) : base(id, name) {
        this.firstNamesMale = firstNamesMale;
        this.firstNamesFemale = firstNamesFemale;
        this.lastNames = lastNames;
    }

    public string RandomFirstName(bool male) {
        return male ? RandomName(firstNamesMale) : RandomName(firstNamesFemale);
    }

    public string RandomLastName() {
        return RandomName(lastNames);
    }

    private string RandomName(string[] names) {
        int index = Random.Range(0, names.Length);
        return names[index];
    }
}

}