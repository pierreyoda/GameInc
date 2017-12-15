using System;
using UnityEngine;

public class WorldController : MonoBehaviour {
    public int defaultStartingMoney = 100000;

    private World world;
    private GameDevCompany company;

    private void Start() {
        world = gameObject.AddComponent<World>();

        company = gameObject.AddComponent<GameDevCompany>();
        company.money = Convert.ToUInt32(defaultStartingMoney);
        company.name = "My First Game Company";
    }
}