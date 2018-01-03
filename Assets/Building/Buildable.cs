using UnityEngine;

public class Buildable : MonoBehaviour {
    [SerializeField] protected string infoId;
    public string InfoId => infoId;

    [SerializeField] private int positionY = 0;
    public int PositionY => positionY;

    [SerializeField] private int positionX = 0;
    public int PositionX => positionX;

    [SerializeField] protected int width;
    public int Width => width;

    [SerializeField] protected SpriteRenderer SpriteRenderer;

    public void UpdatePosition(int posX, int posY) {
        positionX = posX;
        positionY = posY;
        transform.position = new Vector3(positionX, 2 * positionY, 0);
    }

    public void UpdateColor(Color color) {
        SpriteRenderer.color = color;
    }
}
