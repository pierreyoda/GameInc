using System;
using Script;
using UnityEngine;

namespace Database {

[Serializable]
public class Game {
    [SerializeField] private string date;
    public DateTime ReleaseDate => Parser.ParseDate(date);

    [SerializeField] private string[] platforms;
    public string[] Platforms => platforms;

    [SerializeField] private string ratingsDistribution;
    public string RatingsDistributionString => ratingsDistribution; // TODO : avoid this ?
    private Tuple<int, int> ratingsDistributionInt;
    public Tuple<int, int> RatingsDistribution => ratingsDistributionInt;

    [SerializeField] private string beforeName;
    public string BeforeName => beforeName;

    [SerializeField] private string afterName;
    public string AfterName => afterName;

    public void SetRatingsDistribution(Tuple<int, int> distribution) { // TODO : avoid this !
        ratingsDistributionInt = distribution;
    }
}

/// <summary>
/// Describe a Game Series released by other companies to the market.
/// </summary>
[Serializable]
public class GameSeries : DatabaseElement {
    [SerializeField] private Game[] releases;
    public Game[] Releases => releases;

    public GameSeries(string id, string name, Game[] releases)
        : base(id, name) {
        this.releases = releases;
    }

    public override bool IsValid() {
        for (int i = 0; i < releases.Length; i++) {
            if (IsGameValid(releases[i])) continue;
            Debug.LogError($"GameSeries with ID = {Id} : invalid Game (n°{i+1}).");
            return false;
        }
        return true;
    }

    private bool IsGameValid(Game game) {
        // Ratings distribution number check & parsing
        string[] tokens = game.RatingsDistributionString.Split('-');
        if (tokens.Length != 2) {
            Debug.LogError($"GameSeries with ID = {Id} : invalid ratings distribution.");
            return false;
        }
        int lowerBound, higherBound;
        if (!int.TryParse(tokens[0].Trim(), out lowerBound) ||
            !int.TryParse(tokens[1].Trim(), out higherBound)) {
            Debug.LogError($"GameSeries with ID = {Id} : cannot parse ratings number bound as an integer.");
            return false;
        }
        if (higherBound < 0 || lowerBound < 0 || higherBound > 100 ||
            lowerBound > 100 || lowerBound > higherBound) {
            Debug.LogError($"GameSeries with ID = {Id} : invalid ratings bounds.");
            return false;
        }
        game.SetRatingsDistribution(new Tuple<int, int>(lowerBound, higherBound));
        return true;
    }
}

}
