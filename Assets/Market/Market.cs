using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Market : MonoBehaviour {
    [SerializeField] private int marketSize; // number of potential customers
    [SerializeField] private int minimumWeeksOnSale = 4; // minimum number of weeks a Product will stay on sale
    [SerializeField] private float salesCutOffPercentage = 1.0f; // minimum number of sales (in percentages \ first week of sales) to stay on the market
    [SerializeField] private List<GameSeries> competitorSeries = new List<GameSeries>();
    [SerializeField] private List<ReleasedGame> releasedGames = new List<ReleasedGame>();
    [SerializeField] private List<Product> activeProducts = new List<Product>();

    public void Init(DateTime startingGameDate, List<GameSeries> gameSeries) {
        DateTime startingDate = startingGameDate.Date;
        foreach (GameSeries series in gameSeries) {
            int unreleasedGames = series.Releases.Count(
                g => g.ReleaseDate.Date >= startingDate);
            if (unreleasedGames <= 0) continue;
            competitorSeries.Add(series);
        }
    }

    public void OnNewDay(DateTime gameDate) {
        // New releases
        DateTime date = gameDate.Date;
        foreach (GameSeries series in competitorSeries) {
            foreach (Game game in series.Releases) {
                if (game.ReleaseDate.Date == date)
                    ReleaseCompetitorGame(series.Name, game);
            }
        }
        // Market simulation
        SimulateMarket();
    }

    private void SimulateMarket() {

    }

    private void ReleaseCompetitorGame(string seriesName, Game game) {
        int reviewersScore = Random.Range(game.RatingsDistribution.Item1,
            game.RatingsDistribution.Item2 + 1); // upper bound is inclusive by convention
        int usersScore = Random.Range(game.RatingsDistribution.Item1,
            game.RatingsDistribution.Item2 + 1);
        string beforeName = game.BeforeName != "" ? $"{game.BeforeName} " : "";
        string afterName = game.AfterName != "" ? $" {game.AfterName}" : "";
        string releaseName = $"{beforeName}{seriesName}{afterName}";
        ReleasedGame releasedGame = new ReleasedGame(releaseName, reviewersScore,
            usersScore);
        Debug.Log($"Market : Released Competitor Game \"{releaseName}\" with ratings : " +
                  $"reviewers = {reviewersScore}, users = {usersScore}.");
        releasedGames.Add(releasedGame);
    }
}
