using System;
using System.Globalization;
using UnityEngine;

namespace Database {

[Serializable]
public abstract class DatabaseElement {
    [SerializeField] private string id;
    public string Id => id;

    [SerializeField] private string name;
    public string Name => name;

    protected static CultureInfo CultureInfo = CultureInfo.InvariantCulture;

    protected DatabaseElement(string id, string name) {
        this.id = id;
        this.name = name;
    }

    public virtual bool IsValid() {
        return true;
    }

    public static DateTime ParseDate(string date) {
        return DateTime.ParseExact(date, "yyyy/MM/dd", CultureInfo);
    }
}

}
