using System;
using Script;
using UnityEngine;

namespace Database {

/// <summary>
/// A News item that will be triggered on a specified day.
/// </summary>
[Serializable]
public class News : DatabaseElement {
    [SerializeField] private string date;
    public DateTime Date => Parser.ParseDate(date);

    [SerializeField] private string textEnglish;
    public string TextEnglish => textEnglish;

    [SerializeField] private string countries;
    public string Countries => countries;

    public News(string id, string name, string date, string textEnglish,
        string countries)
        : base(id, name) {
        this.date = date;
        this.textEnglish = textEnglish;
        this.countries = countries;
    }
}

}
