using UnityEngine;

public class GameDevCompany : MonoBehaviour {
    [SerializeField] private float money = 0;
    public float Money => money;

    [SerializeField] private string name;
    public string Name => name;

    public GameDevCompany(string name) {
        this.name = name;
    }

    public void Charge(float cost) {
        money -= cost;
    }

    public void Pay(float income) {
        money += income;
    }
}