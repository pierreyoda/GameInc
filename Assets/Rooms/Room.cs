using NUnit.Framework;
using UnityEngine;

public class Room : Buildable {
    private static int INSTANCES_COUNT = 0;
    private static int PIXELS_PER_UNIT = 50;

    [HideInInspector] private int id;
    public int Id => id;

    [SerializeField] private Database.Room info;
    public Database.Room Info => info;

    [SerializeField] private bool underConstruction = true;
    public bool UnderConstruction => underConstruction;

    [SerializeField] private int daysSinceConstructionStart = 0;
    public int DaysSinceConstructionStart => daysSinceConstructionStart;

    [SerializeField] private Sprite sprite;
    public Sprite Sprite => sprite;

    private void Start() {
        id = INSTANCES_COUNT++;

        UpdatePosition(PositionX, PositionY);

        if (SpriteRenderer == null)
            SpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        SpriteRenderer.sprite = sprite;
        SpriteRenderer.sortingOrder = LayerMask.NameToLayer("Rooms");
    }

    public void SetInfo(Database.Room roomInfo) {
        info = roomInfo;
        infoId = info.Id;
        width = info.Width;
        var spriteTexture = Resources.Load<Texture2D>($"Core/{info.TextureName}");
        Assert.IsNotNull(spriteTexture);
        sprite = Sprite.Create(spriteTexture,
            new Rect(0, 0, spriteTexture.width, spriteTexture.height),
            new Vector2(0.5f, 0.5f),
            PIXELS_PER_UNIT);
        if (SpriteRenderer == null)
            SpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        SpriteRenderer.sprite = sprite;
    }

    public void OnNewDay() {
        ++daysSinceConstructionStart;
        if (underConstruction && daysSinceConstructionStart > info.ConstructionTime)
            underConstruction = false;
    }
}
