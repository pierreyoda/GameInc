using System;
using UnityEngine;
using UnityEngine.UI;
using static Database.Database;

public class World : MonoBehaviour {
    [Header("Game World")]
    [SerializeField] private WorldController worldController;
    [SerializeField] private GameDevCompany playerCompany;
    [SerializeField] private Building companyBuilding;
    [SerializeField] private Database.Database database;

    [Header("Date Simulation")]
    [SerializeField] [Range(100, 10000)] private int millisecondsPerDay = 1000;
    [SerializeField] [Range(1970, 2020)] private int gameStartYear = 1982;
    [SerializeField] [Range(1, 12)] private int gameStartMonth = 1;
    [SerializeField] [Range(1, 31)] private int gameStartDay = 1;
    [SerializeField] private int previousDayMonth;
    [SerializeField] private DateTime gameDateTime;
    [SerializeField] private float dayPercentage;

    [Header("Simulation Speed")]
    [SerializeField] private bool simulationRunning = true;
    [SerializeField] private Button pauseButton;

    [SerializeField] private int simulationSpeedMultiplier = 1;
    [SerializeField] private Button speedButton;

    [Header("User Interface")]
    [SerializeField] private GameMenu gameMenu;
    [SerializeField] private BuildRoomSelectionMenu buildRoomSelectionMenu;

    [Header("Construction")]
    [HideInInspector] private bool buildingMode = false;
    [HideInInspector] private Room buildingRoom;

    void Start() {
        Debug.Log("Loading the game database...", gameObject);
        database = new Database.Database();
        const string filesPrefix = "Assets/Resources/Core";
        database.AddDataFile($"{filesPrefix}/events.json", DataFileType.Event)
            .AddDataFolder("Assets/Resources/Core/news", DataFileType.News)
            .AddDataFile($"{filesPrefix}/genres.json", DataFileType.GameGenre)
            .AddDataFile($"{filesPrefix}/themes.json", DataFileType.GameTheme)
            .AddDataFolder("Assets/Resources/Core/platforms", DataFileType.GamingPlatform)
            .AddDataFile($"{filesPrefix}/engine_features.json", DataFileType.EngineFeature)
            .AddDataFile($"{filesPrefix}/rooms.json", DataFileType.Room)
            .AddDataFile($"{filesPrefix}/objects.json", DataFileType.RoomObject)
            .Load()
            .PrintDatabaseInfo();
        worldController.OnGameStarted(database, gameDateTime, playerCompany);

        Debug.Log("Instanciating the game world...", gameObject);

        dayPercentage = 0f;
        gameDateTime = new DateTime(gameStartYear, gameStartMonth, gameStartDay);
        worldController.OnDateModified(gameDateTime);

        playerCompany.Pay(100);
        worldController.OnPlayerCompanyModified(gameDateTime);

        var employeesParentObject = transform.Find("Employees");
        for (int i = 0; i < employeesParentObject.childCount; i++) {
            playerCompany.AddEmployee(employeesParentObject.GetChild(i).GetComponent<Employee>());
        }

        GameEngine defaultEngine = new GameEngine("No Game Engine", new [] { "PC" });
        defaultEngine.AddFeature("Graphics_2D_1");
        defaultEngine.AddFeature("Audio_Mono");
        playerCompany.AddGameEngine(defaultEngine);

        for (int i = 1; i <= 5; i++) {
            GameProject previousGame = new GameProject($"Previous Game {i}",
                database.Genres.FindById("RPG"),
                database.Themes.FindById("HighFantasy"),
                defaultEngine);
            playerCompany.StartProject(previousGame);
            playerCompany.CompleteCurrentProject();
            worldController.OnProjectCompleted(gameDateTime, playerCompany, previousGame);
        }

        GameProject game = new GameProject("Test Game",
            database.Genres.FindById("RPG"),
            database.Themes.FindById("HighFantasy"),
            defaultEngine);
        playerCompany.StartProject(game);

        gameMenu.ShowMenu();
    }

    void Update() {
        if (!simulationRunning) return;

        float elapsedTime = Time.deltaTime; // in s

        // time simulation advance
        dayPercentage += simulationSpeedMultiplier * 1000 * elapsedTime / millisecondsPerDay;
        if (dayPercentage >= 1f) {
            NewDay();
            dayPercentage = 0f;
            playerCompany.Charge(100);
            worldController.OnPlayerCompanyModified(gameDateTime);
        }

        // building mode : display a ghost of the required item under the mouse if possible
        if (!buildingMode) return;
        var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        buildingRoom.UpdatePosition(Mathf.RoundToInt(targetPosition.x), Mathf.RoundToInt(targetPosition.y));
    }

    private void NewDay() {
        previousDayMonth = gameDateTime.Month;
        gameDateTime = gameDateTime.AddDays(1.0);

        companyBuilding.OnNewDay();
        playerCompany.OnNewDay();
        if (gameDateTime.Month != previousDayMonth)
            playerCompany.OnNewMonth(companyBuilding.Rent);

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

        buildingRoom = room;
        buildingMode = true;
        gameMenu.HideMenu();
        Debug.Log($"World.BuildNewRoom : build order for {roomInfo.Name}.");
    }

    public void OnBuildingClicked() {
        if (!buildingMode) return;
        if (!companyBuilding.IsRoomBuildable(buildingRoom)) return;
        Debug.Log("can build!");

        var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        buildingRoom.UpdatePosition(Mathf.RoundToInt(targetPosition.x), Mathf.RoundToInt(targetPosition.y));
        companyBuilding.BuildRoom(buildingRoom);

        playerCompany.Charge(buildingRoom.Info.Cost);
        worldController.OnPlayerCompanyModified(gameDateTime);

        buildingMode = false;
        buildingRoom = null;
    }
}