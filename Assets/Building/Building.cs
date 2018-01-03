using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class Building : MonoBehaviour {
    [SerializeField] private WorldController gameController;

    [SerializeField] private float rent;
    public float Rent => rent;

    /// First dimension : Y (horizontal), second dimension : X (vertical).
    [HideInInspector] private BuildingTile[,] tiles;

    [SerializeField] private int width = 10;
    [SerializeField] private int height = 1;

    [Header("Rendering")]
    [SerializeField] private GameObject wallModelGameObject;
    [SerializeField] private GameObject ceilingModelGameObject;
    [SerializeField] private float ceilingHeight;

    [SerializeField] private Sprite emptyTileSprite;

    [SerializeField] private List<Room> rooms = new List<Room>();

    [Header("Construction")]
    [SerializeField] private Buildable currentlyBuilding;
    public bool IsCurrentlyBuilding => currentlyBuilding != null;
    [SerializeField] private string currentlyBuildingType = "";

    [SerializeField] private Color isBuildableColor;
    [SerializeField] private Color isNotBuildableColor;

    private void Start() {
        // Tiles
        tiles = new BuildingTile[height, width];
        var tilesParentObject = transform.Find("Tiles").gameObject;
        for (uint y = 0; y < height; y++) {
            for (uint x = 0; x < width; x++) {
                var tileObject = new GameObject($"BuildingTile_{x}_{y}");
                var tile = tileObject.AddComponent<BuildingTile>();
                tileObject.transform.position = new Vector3(x, 2 * y, 0);

                var tileRenderer = tileObject.AddComponent<SpriteRenderer>();
                tileRenderer.sprite = emptyTileSprite;

                tiles[y, x] = tile;
                tileObject.transform.parent = tilesParentObject.transform;
            }
        }

        // Collider
        var boxCollider = gameObject.GetComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(width, 2 * height);
        boxCollider.offset = new Vector2(width / 2, 0);

        // Walls
        var wallsParentObject = transform.Find("Walls").gameObject;
        for (int y = 0; y < height; y++) {
            var wallLeft = Instantiate(wallModelGameObject);
            wallLeft.transform.position = new Vector3(-1, 2 * y, 0);
            wallLeft.transform.parent = wallsParentObject.transform;

            var wallRight = Instantiate(wallModelGameObject);
            wallRight.transform.position = new Vector3(width + 1, 2 * y, 0);
            wallRight.transform.parent = wallsParentObject.transform;
        }
        wallModelGameObject.SetActive(false);

        // Ceilings
        ceilingHeight = ceilingModelGameObject.GetComponent<SpriteRenderer>()
            .sprite.textureRect.height;
        var ceilingsParentObject = transform.Find("Ceilings").gameObject;
        for (int y = 0; y < height; y++) {
            for (int x = -1; x < width + 1; x++) {
                var ceiling = Instantiate(ceilingModelGameObject);
                ceiling.transform.position = new Vector3(x, 2 * y + 1 + 1/2 * y * ceilingHeight / 50, 0);
                ceiling.transform.parent = ceilingsParentObject.transform;
            }
        }
        ceilingModelGameObject.SetActive(false);

        // Rooms
        GameObject roomsParentObject = transform.Find("Rooms").gameObject;
        for (int i = 0; i < roomsParentObject.transform.childCount; i++) {
            Room room = roomsParentObject.transform.GetChild(i).GetComponent<Room>();
            rooms.Add(room);
        }

        UpdateEmptyTiles();
    }

    public void InitStartingRooms(Database.Database.DatabaseCollection<Database.Room> dbRooms) {
        GameObject roomsParentObject = transform.Find("Rooms").gameObject;
        Room room0 = roomsParentObject.transform.Find("Room0").GetComponent<Room>();
        Room room1 = roomsParentObject.transform.Find("Room1").GetComponent<Room>();
        room0.SetInfo(dbRooms.FindById("GameDevSimple"));
        room1.SetInfo(dbRooms.FindById("RestroomDouble"));
        UpdateEmptyTiles();
    }

    public void OnMouseDown() {
        Vector2Int mousePosition = GetMousePosition();
        float constructionCost = OnBuildingClicked(mousePosition);
        if (constructionCost < 0) return;
        gameController.OnConstructionStarted(constructionCost);
    }

    public void OnNewDay() {
        foreach (var room in rooms) {
            room.OnNewDay();
        }
    }

    public void StartConstruction(Buildable buildable, string type) {
        Assert.IsTrue(type == "Room" || type == "Object");
        currentlyBuilding = buildable;
        currentlyBuildingType = type;
        UpdateConstruction();
    }

    public void UpdateConstruction() {
        if (!IsCurrentlyBuilding) return;
        Vector2Int buildingPosition = GetMousePosition();
        if (currentlyBuilding.PositionX == buildingPosition.x &&
            currentlyBuilding.PositionY == buildingPosition.y) return;
        currentlyBuilding.UpdatePosition(buildingPosition.x, buildingPosition.y);

        Color filterColor = IsBuildable(currentlyBuilding) ? isBuildableColor : isNotBuildableColor;
        currentlyBuilding.UpdateColor(filterColor);
    }

    private float OnBuildingClicked(Vector2Int buildingPosition) {
        if (!IsCurrentlyBuilding) return -1;
        if (!IsBuildable(currentlyBuilding)) {
            Debug.LogWarning($"Building.Build : {currentlyBuildingType} (ID = {currentlyBuilding.InfoId}) cannot be built over occupied tiles.");
            return -1;
        }

        currentlyBuilding.UpdatePosition(buildingPosition.x, buildingPosition.y);
        Room newRoom = currentlyBuilding as Room;
        Assert.IsNotNull(newRoom);
        FinishConstruction(newRoom, rooms);

        currentlyBuilding = null;
        return newRoom.Info.Cost;
    }

    private void FinishConstruction<T>(T buildable, ICollection<T> buildables)
        where T: Buildable {
        string type = currentlyBuildingType;
        GameObject parentObject = transform.Find($"{type}s").gameObject;
        Assert.IsNotNull(parentObject);
        buildable.gameObject.transform.parent = parentObject.transform;
        buildables.Add(buildable);
        buildable.UpdateColor(Color.white); // reset filter color

        UpdateEmptyTiles();
        Debug.Log($"Added new {type} to the building (ID = {buildable.InfoId}).");
    }

    private bool IsBuildable(Buildable buildable) {
        if (buildable.PositionX < 0 || buildable.PositionX >= width) return false;
        if (buildable.PositionY < 0 || buildable.PositionY >= height) return false;
        int y = buildable.PositionY;
        for (int x = buildable.PositionX; x < (buildable.PositionX + buildable.Width) && x < width; x++) {
            if (!tiles[y, x].Empty) {
                return false;
            }
        }
        return true;
    }

    public float Upkeep() {
        return rooms.Sum(room => room.Info.Upkeep);
    }

    private void UpdateEmptyTiles() {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                tiles[y, x].Empty = true;
            }
        }
        foreach (var room in rooms) {
            int y = room.PositionY;
            for (int x = room.PositionX; x < (room.PositionX + room.Info.Width) && 0 <= x && x < width; x++) {
                tiles[y, x].Empty = false;
            }
        }
    }

    private Vector2Int GetMousePosition() {
        Vector3 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2Int(Mathf.RoundToInt(targetPosition.x),
            Mathf.RoundToInt(targetPosition.y));
    }
}
