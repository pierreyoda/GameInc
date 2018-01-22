using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Database.Database;

public class World : MonoBehaviour {
    [Header("Game World")]
    [SerializeField] private WorldController worldController;
    [SerializeField] private GameDevCompany playerCompany;
    [SerializeField] private Building companyBuilding;
    [SerializeField] private Database.Database database;
    [SerializeField] private Market globalMarket;

    [Header("Date Simulation")]
    [SerializeField] private bool firstDay = true;
    [SerializeField] [Range(100, 10000)] private int millisecondsPerDay = 1000;
    [SerializeField] [Range(1970, 2020)] private int gameStartYear = 1982;
    [SerializeField] [Range(1, 12)] private int gameStartMonth = 1;
    [SerializeField] [Range(1, 31)] private int gameStartDay = 1;
    [SerializeField] private int previousDayMonth;
    [SerializeField] private DateTime gameDateTime;
    [SerializeField] private float dayPercentage;

    [Header("Simulation Speed")]
    [SerializeField] private bool simulationRunning = false;
    [SerializeField] private Button pauseButton;

    [SerializeField] private int simulationSpeedMultiplier = 1;
    [SerializeField] private Button speedButton;

    [Header("User Interface")]
    [SerializeField] private GameMenu gameMenu;
    [SerializeField] private BuildRoomSelectionMenu buildRoomSelectionMenu;

    private void Start() {
        // Database loading
        Debug.Log("Loading the game database...", gameObject);
        database = new Database.Database();
        const string filesPrefix = "Assets/Resources/Core";
        database = database.AddDataFile($"{filesPrefix}/events.json", DataFileType.Event)
            .AddDataFolder("Assets/Resources/Core/news", DataFileType.News)
            .AddDataFile($"{filesPrefix}/genres.json", DataFileType.GameGenre)
            .AddDataFile($"{filesPrefix}/themes.json", DataFileType.GameTheme)
            .AddDataFolder("Assets/Resources/Core/platforms", DataFileType.GamingPlatform)
            .AddDataFolder("Assets/Resources/Core/games", DataFileType.GameSeries)
            .AddDataFile($"{filesPrefix}/engine_features.json", DataFileType.EngineFeature)
            .AddDataFile($"{filesPrefix}/rooms.json", DataFileType.Room)
            .AddDataFile($"{filesPrefix}/objects.json", DataFileType.RoomObject)
            .AddDataFile($"{filesPrefix}/skills.json", DataFileType.Skill)
            .AddDataFile($"{filesPrefix}/hiring.json", DataFileType.HiringMethod)
            .AddDataFolder("Assets/Resources/Core/names", DataFileType.CommonNames)
            .Load();
        if (database == null) {
            Debug.LogError($"World.Start() : Database loading error.");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
            Application.Quit();
            return;
        }
        database.PrintDatabaseInfo();

        Debug.Log("Instanciating the game world...", gameObject);

        dayPercentage = 0f;
        gameDateTime = new DateTime(gameStartYear, gameStartMonth, gameStartDay);

        globalMarket.Init(gameDateTime, database.GameSeries.Collection);
        worldController.OnGameStarted(database, gameDateTime, playerCompany);
        worldController.OnDateModified(gameDateTime);
        worldController.OnPlayerCompanyModified();

        var employeesParentObject = transform.Find("Employees");
        for (int i = 0; i < employeesParentObject.childCount; i++) {
            playerCompany.AddEmployee(employeesParentObject.GetChild(i).GetComponent<Employee>());
        }

        gameMenu.ShowMenu();
    }

    private void OnGameStart() {
        string startingDate = gameDateTime.ToString("yyyy/MM/dd");
        Debug.Log($"World.OnGameStart : starting date = {startingDate}");

        GameEngine defaultEngine = new GameEngine("No Game Engine",
            new DateTime(1980, 1, 1),
            new [] { "PC" });
        defaultEngine.AddFeature("Graphics_2D_1");
        defaultEngine.AddFeature("Audio_Mono");
        playerCompany.AddGameEngine(defaultEngine);

        GameEngine basicEngine = new GameEngine("Basic Game Engine",
            new DateTime(1982, 1, 1),
            new [] { "PC", "NES" });
        basicEngine.AddFeature("Graphics_2D_1");
        basicEngine.AddFeature("Graphics_2D_2");
        basicEngine.AddFeature("Audio_Mono");
        playerCompany.AddGameEngine(basicEngine);

        for (int i = 1; i <= 5; i++) {
            GameProject previousGame = new GameProject($"Previous Game {i}",
                database.Genres.FindById("RPG"),
                database.Themes.FindById("HighFantasy"),
                defaultEngine,
                new List<string> { "PC", "NES" });
            playerCompany.StartProject(previousGame, gameDateTime);
            playerCompany.CompleteCurrentProject();
            worldController.OnProjectCompleted(playerCompany, previousGame);
        }

        GameProject testGame = new GameProject("Test Game",
            database.Genres.FindById("Action"),
            database.Themes.FindById("Far West"),
            defaultEngine,
            new List<string> { "PC" });
        playerCompany.StartProject(testGame, gameDateTime);

        companyBuilding.InitStartingRooms(database.Rooms);

        firstDay = false;
        simulationRunning = true;
    }

    private void Update() {
        if (!simulationRunning) {
            if (firstDay) OnGameStart();
            else return;
        }

        float elapsedTime = Time.deltaTime; // in s

        // time simulation advance
        dayPercentage += simulationSpeedMultiplier * 1000 * elapsedTime / millisecondsPerDay;
        if (dayPercentage >= 1f) {
            NewDay();
            dayPercentage = 0f;
            worldController.OnPlayerCompanyModified();
        }

        // building mode : display a ghost of the required item under the mouse if possible
        if (!companyBuilding.IsCurrentlyBuilding) return;
        companyBuilding.UpdateConstruction();
    }

    private void NewDay() {
        previousDayMonth = gameDateTime.Month;
        gameDateTime = gameDateTime.AddDays(1.0);

        globalMarket.OnNewDay(gameDateTime);

        companyBuilding.OnNewDay();
        playerCompany.OnNewDay(worldController);
        if (gameDateTime.Month != previousDayMonth)
            playerCompany.OnNewMonth(companyBuilding.Rent, companyBuilding.Upkeep());

        worldController.OnDateModified(gameDateTime);
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

    public void ShowBuildRoomSelectionMenu() {
        gameMenu.ShowMenu();
        buildRoomSelectionMenu.OpenSelectionMenu(database.Rooms.Collection);
    }

    public void BuildNewRoom(string roomId) {
        var roomInfo = database.Rooms.FindById(roomId);
        if (roomInfo == null) {
            Debug.LogError($"World.BuildNewRoom : invalid Room ID \"{roomId}\".");
            return;
        }

        var roomGameObject = new GameObject();
        var room = roomGameObject.AddComponent<Room>();
        roomGameObject.name = $"Room_{room.Id}";
        room.SetInfo(roomInfo);
        companyBuilding.StartConstruction(room, "Room");

        gameMenu.HideMenu();
        Debug.Log($"World.BuildNewRoom : build order for {roomInfo.Name}.");
    }

    public void OnConstructionStarted(float constructionCost) {
        playerCompany.Charge(constructionCost);
        worldController.OnPlayerCompanyModified();
    }
}
