using UnityEngine;
using UnityEngine.UI;

public class GameHudController : MonoBehaviour {
    [SerializeField] private Text playerCompanyNameText;
    [SerializeField] private Text playerCompanyMoneyText;

    public void UpdateHud(GameDevCompany playerCompany) {
        playerCompanyNameText.text = $"{playerCompany.Name}";
        playerCompanyMoneyText.text = $"{playerCompany.Money} k";
    }
}