using UnityEngine;

public class Room : MonoBehaviour {
    private static int INSTANCES_COUNT = 0;

    [HideInInspector] private int id;
    public int Id => id;

    [SerializeField] private int floorNumber = 0;
    public int FloorNumber => floorNumber;

    [SerializeField] private int positionX = 0;
    public int PositionX => positionX;

    [SerializeField] private int width;
    public int Width => width;

    [SerializeField] private bool underConstruction = true;
    public bool UnderConstruction => underConstruction;

    [SerializeField] private int daysSinceConstructionStart = 0;
    public int DaysSinceConstructionStart => daysSinceConstructionStart;

    [SerializeField] private Sprite sprite;
    public Sprite Sprite => sprite;

    private void Start() {
        id = INSTANCES_COUNT++;

        transform.position = new Vector3(positionX, floorNumber, 0);

        var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
    }

    public void OnNewDay() {
        Debug.Log("a");
        ++daysSinceConstructionStart;
    }
}
