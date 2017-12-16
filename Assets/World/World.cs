using System;
using UnityEngine;

public class World : MonoBehaviour {
    public Building companyBuilding;

    [SerializeField] [Range(100, 10000)]
    private int millisecondsPerDay = 1000;

    [SerializeField] [Range(1970, 2020)]
    private int gameStartYear = 1982;
    [SerializeField] [Range(1, 12)]
    private int gameStartMonth = 1;
    [SerializeField] [Range(1, 31)]
    private int gameStartDay = 1;

    private DateTime gameDateTime;
    private float dayPercentage;

    void Start() {
        Debug.Log("Instanciating the game world...", gameObject);

        gameDateTime = new DateTime(gameStartYear, gameStartMonth, gameStartDay);
        dayPercentage = 0f;
    }

    void Update() {
        float elapsedTime = Time.deltaTime; // in s

        // time simulation advance
        dayPercentage += 1000 * elapsedTime / millisecondsPerDay;
        if (dayPercentage >= 1f) {
            newDay();
            dayPercentage = 0f;
        }
    }

    private void newDay() {
        gameDateTime = gameDateTime.AddDays(1.0);
        Debug.Log("New day, date = " + gameDateTime.ToString("yyyy/MM/dd"));
    }
}