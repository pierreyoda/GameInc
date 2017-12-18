using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GameDevCompany : MonoBehaviour {
    [SerializeField] private float money = 0;
    public float Money => money;

    [SerializeField] private bool neverBailedOut = true;
    public bool NeverBailedOut {
        get { return neverBailedOut; }
        set { neverBailedOut = value; }
    }

    [SerializeField] private string name;
    public string Name => name;

    public GameDevCompany(string name) {
        this.name = name;
    }

    public void Charge(float cost) {
        Assert.IsTrue(cost > 0);
        money -= cost;
    }

    public void Pay(float income) {
        Assert.IsTrue(income > 0);
        money += income;
    }
}
