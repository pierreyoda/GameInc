using UnityEngine;

public class Building : MonoBehaviour {
    /// First dimension : Y (horizontal), second dimension : X (vertical).
    private BuildingTile[,] tiles;

    [SerializeField]
    private int width = 10;

    [SerializeField]
    private int height = 1;

    public Sprite emptyTileSprite;

    private void Start() {
        var tilesParentObject = transform.Find("Tiles").gameObject;

        tiles = new BuildingTile[height, width];
        for (uint y = 0; y < height; y++) {
            for (uint x = 0; x < width; x++) {
                var tileName = string.Format("BuildingTile_{0}_{1}", x, y);
                var tileObject = new GameObject(tileName);
                var tile = tileObject.AddComponent<BuildingTile>();
                tileObject.transform.position = new Vector3(x, y, 0);

                var tileRenderer = tileObject.AddComponent<SpriteRenderer>();
                tileRenderer.sprite = emptyTileSprite;

                tiles[y, x] = tile;
                tileObject.transform.parent = tilesParentObject.transform;
            }
        }
    }
}