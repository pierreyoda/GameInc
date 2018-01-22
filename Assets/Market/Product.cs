using System;
using UnityEngine;

/// <summary>
/// Modelizes a Product (Game, Gaming Platform, Game Engine) being actively
/// sold in the Market.
/// </summary>
[Serializable]
public class Product {
    [SerializeField] private int averageScore;
    public int AverageScore {
        get { return averageScore; }
        set { averageScore = value; }
    }

    [SerializeField] private int sales = 0;
    public int Sales {
        get { return sales; }
        set { sales = value; }
    }

    public Product(int averageScore) {
        this.averageScore = averageScore;
    }
}
