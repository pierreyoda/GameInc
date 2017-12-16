using UnityEngine;

public class World : MonoBehaviour {
    public Building companyBuilding;

    void Start() {
        Debug.Log("Instanciating the game world...", gameObject);
    }
}