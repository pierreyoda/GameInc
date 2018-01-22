using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Random = UnityEngine.Random;

[Serializable]
public class Market : MonoBehaviour {
    [SerializeField] private int marketSize; // number of potential customers
    [SerializeField] private int minimumWeeksOnSale = 1; // minimum number (inclusive) of weeks a Product will stay on sale
    [SerializeField] private float salesCutOffPercentage = 0.2f; // minimum number of sales (in percentages \ first week of sales) to stay on the market
    [SerializeField] private float popularityFromScoreFactor = 1f / 50f;
    [SerializeField] private List<GameSeries> competitorSeries = new List<GameSeries>();
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
        SimulateDailyMarket();
    }

    public void OnNewWeek() {
        List<Product> stillActiveProducts = new List<Product>();
        foreach (Product activeProduct in activeProducts) {
            bool dropped = false;
            if (++activeProduct.WeeksSinceRelease == 1)
                activeProduct.FirstWeekSales = activeProduct.CurrentWeekSales;
            float salesPercentage = (float)activeProduct.CurrentWeekSales
                                    / activeProduct.FirstWeekSales;
            if (salesPercentage < salesCutOffPercentage &&
                activeProduct.WeeksSinceRelease > minimumWeeksOnSale)
                dropped = true;
            if (!dropped) {
                activeProduct.OnNewWeek();
                activeProduct.CurrentWeekSales = 0;
                stillActiveProducts.Add(activeProduct);
                continue;
            }
            Debug.Log($"Market.OnNewWeek : dropped product {activeProduct.Name} after " +
                      $"{activeProduct.WeeksSinceRelease} weeks and {activeProduct.Sales} sales.");
        }
        activeProducts = stillActiveProducts;
    }

    private void SimulateDailyMarket() {
        foreach (Product activeProduct in activeProducts) {
            float share = (float)activeProduct.Sales / marketSize;
            float score = activeProduct.AverageScore;
            float popularity = Random.Range(score * popularityFromScoreFactor,
                                   score * 1.5f*popularityFromScoreFactor) / 100f;
            int sales = Mathf.RoundToInt(popularity * marketSize * (1-share) * activeProduct.Freshness);
            if (sales + activeProduct.Sales >= marketSize) {
                sales = marketSize - activeProduct.Sales;
            }
            activeProduct.CurrentWeekSales += sales;
            activeProduct.Sales += sales;
            Debug.LogWarning($"sh={share},sc={score},p={popularity},f={activeProduct.Freshness} => {sales} sales");
        }
    }

    private void ReleaseCompetitorGame(string seriesName, Game game) {
        int reviewersScore = Random.Range(game.RatingsDistribution.Item1,
            game.RatingsDistribution.Item2 + 1); // upper bound is inclusive by convention
        int usersScore = Random.Range(game.RatingsDistribution.Item1,
            game.RatingsDistribution.Item2 + 1);
        // Released Game
        string beforeName = game.BeforeName != "" ? $"{game.BeforeName} " : "";
        string afterName = game.AfterName != "" ? $" {game.AfterName}" : "";
        string releaseName = $"{beforeName}{seriesName}{afterName}";
        Debug.Log($"Market : Released Competitor Game \"{releaseName}\" with ratings : " +
                  $"reviewers = {reviewersScore}, users = {usersScore}.");
        float averageScore = (reviewersScore + usersScore) / 2f;
        Product product = new Product(ProductType.Game, releaseName, averageScore);
        activeProducts.Add(product);
    }
}
