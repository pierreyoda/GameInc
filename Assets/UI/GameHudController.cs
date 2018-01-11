using System;
using Database;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class GameHudController : MonoBehaviour {
    [SerializeField] private Text currentDateText;
    [SerializeField] private Text playerCompanyNameText;
    [SerializeField] private Text playerCompanyMoneyText;
    [SerializeField] private NewsBarPanel newsBarPanel;

    private void Start() {
        Assert.IsNotNull(currentDateText);
        Assert.IsNotNull(playerCompanyMoneyText);
        Assert.IsNotNull(playerCompanyMoneyText);
        Assert.IsNotNull(newsBarPanel);
        newsBarPanel.gameObject.SetActive(false);
    }

    public void UpdateDateDisplay(DateTime currentDate) {
        newsBarPanel.gameObject.SetActive(newsBarPanel.UpdateDate(currentDate));
        currentDateText.text = currentDate.ToString("yyyy/MM/dd");
    }

    public void UpdateNewsBar(News latestNews) {
        newsBarPanel.UpdateNewsText(latestNews);
    }

    public void UpdateCompanyHud(GameDevCompany playerCompany) {
        playerCompanyNameText.text = $"{playerCompany.CompanyName}";
        playerCompanyMoneyText.text = $"{playerCompany.Money} k";
    }
}