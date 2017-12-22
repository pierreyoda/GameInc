using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building : MonoBehaviour {
    [SerializeField] private WorldController gameController;

    [SerializeField] private float rent;
    public float Rent => rent;

    /// First dimension : Y (horizontal), second dimension : X (vertical).
    [HideInInspector] private BuildingTile[,] tiles;

    [SerializeField] private int width = 10;
    [SerializeField] private int height = 1;

    [SerializeField] private GameObject wallModelGameObject;
    [SerializeField] private GameObject ceilingModelGameObject;
    [SerializeField] private float ceilingHeight;

    [SerializeField] private Sprite EmptyTileSprite;

    [SerializeField] private List<Room> rooms = new List<Room>();

    private void Start() {
        tiles = new BuildingTile[height, width];
        var tilesParentObject = transform.Find("Tiles").gameObject;
        for (uint y = 0; y < height; y++) {
            for (uint x = 0; x < width; x++) {
                var tileObject = new GameObject($"BuildingTile_{x}_{y}");
                var tile = tileObject.AddComponent<BuildingTile>();
                tileObject.transform.position = new Vector3(x, 2 * y, 0);

                var tileRenderer = tileObject.AddComponent<SpriteRenderer>();
                tileRenderer.sprite = EmptyTileSprite;

                tiles[y, x] = tile;
                tileObject.transform.parent = tilesParentObject.transform;
            }
        }

        var collider = gameObject.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(width, height);

        var roomsParentObject = transform.Find("Rooms").gameObject;
        for (int i = 0; i < roomsParentObject.transform.childCount; i++) {
            rooms.Add(roomsParentObject.transform.GetChild(i).GetComponent<Room>());
        }
        UpdateEmptyTiles();

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
    }

    public void OnMouseDown() {
        gameController.OnBuildingClicked();
    }

    public void OnNewDay() {
        foreach (var room in rooms) {
            room.OnNewDay();
        }
    }

    public bool IsRoomBuildable(Room room) {
        if (room.FloorNumber < 0 || room.FloorNumber >= height) return false;
        for (int x = room.PositionX; x < (room.PositionX + room.Info.Width) & 0 <= x && x < width; x++) {
            if (!tiles[room.FloorNumber, x].Empty)
                return false;
        }
        return true;
    }

    public void BuildRoom(Room room) {
        if (!IsRoomBuildable(room)) {
            Debug.LogWarning($"Building.BuildRoom : Room (ID = {room.Id}) cannot be built over occupied tiles.");
            return;
        }

        var roomsParentObject = transform.Find("Rooms").gameObject;
        room.gameObject.transform.parent = roomsParentObject.transform;
        rooms.Add(room);
        UpdateEmptyTiles();

        Debug.Log($"Added new Room to the building (ID = {room.Id}).");
    }

    public float Upkeep() {
        return rooms.Sum(room => room.Info.Upkeep);
    }

    private void UpdateEmptyTiles() {
        foreach (var room in rooms) {
            int y = room.FloorNumber;
            for (int x = room.PositionX; x < (room.PositionX + room.Info.Width) & 0 <= x && x < width; x++) {
                tiles[y, x].Empty = false;
            }
        }
    }
}