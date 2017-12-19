using System;
using UnityEngine;
using UnityEngine.UI;

public class GameHudController : MonoBehaviour {
    [SerializeField] private Text currentDateText;
    [SerializeField] private Text playerCompanyNameText;
    [SerializeField] private Text playerCompanyMoneyText;

    public void UpdateDateDisplay(DateTime currentDate) {
        currentDateText.text = currentDate.ToString("yyyy/MM/dd");
    }

    public void UpdateCompanyHud(GameDevCompany playerCompany) {
        playerCompanyNameText.text = $"{playerCompany.CompanyName}";
        playerCompanyMoneyText.text = $"{playerCompany.Money} k";
    }
}