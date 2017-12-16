using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public class DatabaseElement {
    [SerializeField] private string id;
    public string Id => id;

    protected static CultureInfo cultureInfo = CultureInfo.InvariantCulture;

    protected DatabaseElement(string id) {
        this.id = id;
    }

    public static DateTime ParseDate(string date) {
        return DateTime.ParseExact(date, "yyyy/MM/dd", cultureInfo);
    }
}
