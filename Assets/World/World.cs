using System;
using UnityEngine;
using UnityEngine.UI;

public class World : MonoBehaviour {
    [SerializeField] private WorldController worldController;

    [SerializeField] private GameDevCompany playerCompany;
    [SerializeField] private Building companyBuilding;
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

    [SerializeField] private GameMenu gameMenu;
    [SerializeField] private BuildRoomSelectionMenu buildRoomSelectionMenu;

    [HideInInspector] private bool buildingMode = false;
    [HideInInspector] private Room buildingRoom;

    void Start() {
        Debug.Log("Loading the game database...", gameObject);
        database = new Database.Database();
        const string filesPrefix = "Assets/Resources/Core";
        database.AddGenresDataFile($"{filesPrefix}/genres.json")
            .AddThemesDataFile($"{filesPrefix}/themes.json")
            .AddPlatformsDataFile($"{filesPrefix}/platforms.json")
            .AddRoomsDataFile($"{filesPrefix}/rooms.json")
            .AddObjectsDataFile($"{filesPrefix}/objects.json")
            .Load();

        Debug.Log("Instanciating the game world...", gameObject);

        dayPercentage = 0f;
        gameDateTime = new DateTime(gameStartYear, gameStartMonth, gameStartDay);
        worldController.OnDateModified(gameDateTime);

        playerCompany.Pay(100);
        worldController.OnPlayerCompanyModified(playerCompany);

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
        }

        // building mode : display a ghost of the required item under the mouse if possible
        if (!buildingMode) return;
        var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        buildingRoom.UpdatePosition(Mathf.RoundToInt(targetPosition.x), Mathf.RoundToInt(targetPosition.y));
    }

    private void NewDay() {
        gameDateTime = gameDateTime.AddDays(1.0);
        Debug.Log("New day, date = " + gameDateTime.ToString("yyyy/MM/dd"));

        companyBuilding.OnNewDay();

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
        worldController.OnPlayerCompanyModified(playerCompany);

        buildingMode = false;
        buildingRoom = null;
    }
}