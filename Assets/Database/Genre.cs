using System;

namespace Database {

/// <summary>
/// The main genre of a game.
/// </summary>
[Serializable]
public class Genre : DatabaseElement {
    public Genre(string id, string name) : base(id, name) { }
}

}
