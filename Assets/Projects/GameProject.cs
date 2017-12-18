using Database;
using UnityEngine;

public class GameProject : Project {
    [SerializeField] private Genre genre;
    public Genre Genre => genre;

    [SerializeField] private Theme theme;
    public Theme Theme => theme;

    [SerializeField] private GameEngine engine;
    public GameEngine Engine => engine;

    public GameProject(string name, Genre genre, Theme theme, GameEngine engine)
            : base(name) {
        this.genre = genre;
        this.theme = theme;
        this.engine = engine;
    }
}
