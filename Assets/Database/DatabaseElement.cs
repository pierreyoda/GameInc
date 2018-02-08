using System;
using UnityEngine;

namespace Database {

[Serializable]
public abstract class DatabaseElement {
    [SerializeField] private string id;
    public string Id => id;

    [SerializeField] private string name;
    public string Name => name;

    protected DatabaseElement(string id, string name) {
        this.id = id;
        this.name = name;
    }

    public virtual bool IsValid() {
        if (id.Trim() == "") {
            Debug.LogError("DatabaseElement : empty ID.");
            return false;
        }
        if (id.Contains(" ")) {
            Debug.LogError($"DatabaseElement(ID = {id} : invalid ID.");
            return false;
        }
        if (name.Trim() == "") {
            Debug.LogError($"DatabaseElement(ID = {id}) : empty name.");
            return false;
        }
        return true;
    }
}

}
