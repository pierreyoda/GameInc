using System;
using UnityEngine;

namespace Database {

/// <summary>
/// A News item that will be triggered on a specified day.
/// </summary>
[Serializable]
public class News : DatabaseElement {
    [SerializeField] private string date;
    public DateTime Date => ParseDate(date);

    [SerializeField] private string textEnglish;
    public string TextEnglish => textEnglish;

    public News(string id, string name, string date, string textEnglish)
        : base(id, name) {
        this.date = date;
        this.textEnglish = textEnglish;
    }
}

}