using System.Collections.Generic;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

public class Building : MonoBehaviour {
    /// First dimension : Y (horizontal), second dimension : X (vertical).
    private BuildingTile[,] tiles;

    [SerializeField] private int width = 10;

    [SerializeField] private int height = 1;

    public Sprite EmptyTileSprite;

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

        var roomsParentObject = transform.Find("Rooms").gameObject;
        for (int i = 0; i < roomsParentObject.transform.childCount; i++) {
            rooms.Add(roomsParentObject.transform.GetChild(i).GetComponent<Room>());
        }
        UpdateEmptyTiles();
    }

    public void OnNewDay() {
        foreach (var room in rooms) {
            room.OnNewDay();
        }
    }

    public void BuildRoom(Database.Room roomInfo) {
        // TODO
        UpdateEmptyTiles();
    }

    private void UpdateEmptyTiles() {
        foreach (var room in rooms) {
            int y = room.FloorNumber;
            for (int x = room.PositionX; x < room.PositionX + room.Width & 0 <= x && x < width; x++) {
                tiles[y, x].Empty = false;
            }
        }
    }
}