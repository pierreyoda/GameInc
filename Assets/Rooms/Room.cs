using UnityEngine;

public class Room : MonoBehaviour {
    private static int INSTANCES_COUNT = 0;

    [HideInInspector] private int id;
    public int Id => id;

    [SerializeField] private Database.Room info;
    public Database.Room Info => info;

    [SerializeField] private int floorNumber = 0;
    public int FloorNumber => floorNumber;

    [SerializeField] private int positionX = 0;
    public int PositionX => positionX;

    [SerializeField] private bool underConstruction = true;
    public bool UnderConstruction => underConstruction;

    [SerializeField] private int daysSinceConstructionStart = 0;
    public int DaysSinceConstructionStart => daysSinceConstructionStart;

    [SerializeField] private Sprite sprite;
    public Sprite Sprite => sprite;

    private SpriteRenderer spriteRenderer;

    private void Start() {
        id = INSTANCES_COUNT++;

        UpdatePosition(positionX, floorNumber);

        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
    }

    public void SetInfo(Database.Room roomInfo) {
        info = roomInfo;
        var spriteTexture = Resources.Load<Texture2D>($"Core/{info.TextureName}");
        sprite = Sprite.Create(spriteTexture,
            new Rect(0, 0, spriteTexture.width, spriteTexture.height),
            new Vector2(0.5f, 0.5f));
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
    }

    public void UpdatePosition(int posX, int posY) {
        positionX = posX;
        floorNumber = posY;
        transform.position = new Vector3(positionX, 2 * floorNumber, 0);
    }

    public void OnNewDay() {
        ++daysSinceConstructionStart;
        if (underConstruction && daysSinceConstructionStart > info.ConstructionTime)
            underConstruction = false;
    }
}
