using System;

namespace Database {

[Serializable]
public class Theme : DatabaseElement {
    public Theme(string id, string name) : base(id, name) { }
}

}
