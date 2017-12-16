using System;
using UnityEngine;
using UnityEngine.UI;

public class World : MonoBehaviour {
    public Building CompanyBuilding;
    private Database.Database database;

    [SerializeField] [Range(100, 10000)] private int millisecondsPerDay = 1000;

    [SerializeField] [Range(1970, 2020)] private int gameStartYear = 1982;
    [SerializeField] [Range(1, 12)] private int gameStartMonth = 1;
    [SerializeField] [Range(1, 31)] private int gameStartDay = 1;

    [SerializeField] private DateTime gameDateTime;
    [SerializeField] private float dayPercentage;

    [SerializeField] private bool simulationRunning = true;
    [SerializeField] private Button pauseButton;

    [SerializeField] private int simulationSpeedMultiplier = 1;
    [SerializeField] private Button speedButton;

    void Start() {
        Debug.Log("Loading the game database...", gameObject);
        database = new Database.Database();
        database.AddPlatformsDataFile("Assets/Resources/Core/platforms.json")
            .AddRoomsDataFile("Assets/Resources/Core/rooms.json")
            .Load();

        Debug.Log("Instanciating the game world...", gameObject);

        gameDateTime = new DateTime(gameStartYear, gameStartMonth, gameStartDay);
        dayPercentage = 0f;
    }

    void Update() {
        if (!simulationRunning) return;

        float elapsedTime = Time.deltaTime; // in s

        // time simulation advance
        dayPercentage += simulationSpeedMultiplier * 1000 * elapsedTime / millisecondsPerDay;
        if (dayPercentage >= 1f) {
            NewDay();
            dayPercentage = 0f;
        }
    }

    private void NewDay() {
        gameDateTime = gameDateTime.AddDays(1.0);
        Debug.Log("New day, date = " + gameDateTime.ToString("yyyy/MM/dd"));

        CompanyBuilding.OnNewDay();
    }

    public void ToggleSimulation() {
        dayPercentage = 0f;
        simulationRunning = !simulationRunning;
        pauseButton.GetComponentInChildren<Text>().text = simulationRunning ? "Pause" : "Resume";
    }

    public void ToggleSimulationSpeed() {
        speedButton.GetComponentInChildren<Text>().text = $"x{simulationSpeedMultiplier}";
        simulationSpeedMultiplier = simulationSpeedMultiplier == 1 ? 2 : 1;
    }
}