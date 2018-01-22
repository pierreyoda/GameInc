using System;
using UnityEngine;

[Serializable]
public enum ProductType {
    Platform,
    GameEngine,
    Game,
}

/// <summary>
/// Modelizes a Product (Game, Gaming Platform, Game Engine) being actively
/// sold in the Market.
/// </summary>
[Serializable]
public class Product {
    [SerializeField] private ProductType type;
    public ProductType Type => type;

    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private int weeksSinceRelease = 0;
    public int WeeksSinceRelease {
        get { return weeksSinceRelease; }
        set { weeksSinceRelease = value; }
    }

    [SerializeField] private float freshness = 1f;
    public float Freshness => freshness;

    [SerializeField] private float averageScore;
    public float AverageScore {
        get { return averageScore; }
        set { averageScore = value; }
    }

    [SerializeField] private int sales = 1; // avoid / 0 in market simulation
    public int Sales {
        get { return sales; }
        set { sales = value; }
    }

    [SerializeField] private int firstWeekSales = 0;
    public int FirstWeekSales {
        get { return firstWeekSales; }
        set { firstWeekSales = value; }
    }

    [SerializeField] private int currentWeekSales = 0;
    public int CurrentWeekSales {
        get { return currentWeekSales; }
        set { currentWeekSales = value; }
    }

    public Product(ProductType type, string name, float averageScore) {
        this.type = type;
        this.name = name;
        this.averageScore = averageScore;
    }

    public void OnNewWeek() {
        freshness = ProductFreshness();
    }

    private float ProductFreshness() {
        float tau; // time constant ; after tau weeks, freshness ~= 0.63
        switch (Type) {
            case ProductType.Platform: tau = 60f; break;
            case ProductType.GameEngine: tau = 40f; break;
            case ProductType.Game: tau = 4f; break;
            default: throw new ArgumentOutOfRangeException();
        }
        return Mathf.Exp(-WeeksSinceRelease/tau);
    }
}
