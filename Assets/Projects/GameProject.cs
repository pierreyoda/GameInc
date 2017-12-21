using System;
using Database;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class GameProject : Project {
    public static string[] GAME_SCORES = new[] {
        "Graphics", "Audio", "Gameplay", "Creativity"
    };

    [SerializeField] private Genre genre;
    public Genre Genre => genre;

    [SerializeField] private Theme theme;
    public Theme Theme => theme;

    [SerializeField] private GameEngine engine;
    public GameEngine Engine => engine;

    [SerializeField] private float[] scores = new float[GAME_SCORES.Length];
    public float[] Scores => scores;

    public GameProject(string name, Genre genre, Theme theme, GameEngine engine)
            : base(name) {
        this.genre = genre;
        this.theme = theme;
        this.engine = engine;
    }

    public float Score(string scoreName) {
        int scoreIndex = ScoreIndexFromName(scoreName);
        if (scoreIndex < 0 || scoreIndex >= GAME_SCORES.Length) {
            Debug.LogError($"GameProject.Score({scoreName}) : unkown score name.");
            return 0f;
        }

        return scores[scoreIndex];
    }

    public void ModifyScore(string scoreName, float difference) {
        int scoreIndex = ScoreIndexFromName(scoreName);
        if (scoreIndex < 0 || scoreIndex >= GAME_SCORES.Length) {
            Debug.LogError($"GameProject.ModifyScore({scoreName}, {difference}) : unkown score name.");
            return;
        }

        scores[scoreIndex] += difference;
    }

    private static int ScoreIndexFromName(string scoreName) {
        for (int i = 0; i < GAME_SCORES.Length; i++) {
            if (GAME_SCORES[i] == scoreName) return i;
        }
        return -1;
    }

    public override ProjectType Type() {
        return ProjectType.GameProject;
    }
}
