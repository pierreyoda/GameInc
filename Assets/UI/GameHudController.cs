using System;
using System.Collections.Generic;
using Database;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class GameHudController : MonoBehaviour {
    [Header("Control")]
    [SerializeField] private WorldController worldController;
    [SerializeField] private Button newProjectButton;

    [Header("Theme")]
    [SerializeField] private UserInterfaceTheme theme;

    [Header("Menu")]
    [SerializeField] private GameMenu menu;

    [Header("Company Summary")]
    [SerializeField] private Text currentDateText;
    [SerializeField] private Text playerCompanyNameText;
    [SerializeField] private Text playerCompanyMoneyText;

    [Header("Dialogs")]
    [SerializeField] private bool pauseGameInDialog = true;
    public bool PauseGameInDialog => pauseGameInDialog;

    [SerializeField] private NewGameDialog newGameDialog;
    [SerializeField] private EventTriggeredDialog eventTriggeredDialog;

    [Header("News Prompter")]
    [SerializeField] [UnityEngine.Range(1, 10)] private int maximumNewsCount = 3;
    [SerializeField] [UnityEngine.Range(1f, 50f)] private float marginBetweenPanels = 2f;
    [SerializeField] private Transform newsBarPanelsParent;
    [SerializeField] private NewsBarPanel newsBarPanelModel;
    [SerializeField] private List<NewsBarPanel> newsBarPanels = new List<NewsBarPanel>();
    private int currentNewsBarPanelIndex = 0;

    private void Start() {
        Assert.IsNotNull(worldController);
        Assert.IsNotNull(newProjectButton);
        Assert.IsNotNull(theme);
        Assert.IsNotNull(menu);
        Assert.IsNotNull(currentDateText);
        Assert.IsNotNull(playerCompanyMoneyText);
        Assert.IsNotNull(playerCompanyMoneyText);
        Assert.IsNotNull(newsBarPanelModel);
        Assert.IsNotNull(newsBarPanelsParent);

        eventTriggeredDialog.gameObject.SetActive(false);
        for (int i = 0; i < maximumNewsCount; i++) {
            NewsBarPanel newsBarPanel = Instantiate(newsBarPanelModel);
            newsBarPanel.transform.SetParent(newsBarPanelsParent, false);
            newsBarPanel.name = $"NewsBarPanel_{i}";
            newsBarPanel.gameObject.SetActive(false);
            newsBarPanels.Add(newsBarPanel);
        }
        newsBarPanelModel.gameObject.SetActive(false);

        menu.ApplyTheme(theme);
    }

    public void CanStartNewProject(bool allowed) {
        newProjectButton.interactable = allowed;
    }

    public void ShowNewProjectDialog(DateTime gameDate, Project.ProjectType type,
        Database.Database database, IEnumerable<GameEngine> gameEngines) {
        switch (type) {
            case Project.ProjectType.GameProject:
                newGameDialog.ShowDialog(gameDate, database, gameEngines, SubmitNewProjectDialog);
                break;
        }
    }

    public void SubmitNewProjectDialog(GameProject newGameProject) {
        newGameDialog.HideDialog();
        worldController.OnProjectStarted(newGameProject);
    }

    public void OnCompanyChanged(GameDevCompany playerCompany) {
        playerCompanyNameText.text = $"{playerCompany.CompanyName}";
        playerCompanyMoneyText.text = $"{playerCompany.Money:0.#} k";
    }

    public void OnDateChanged(DateTime currentDate) {
        currentDateText.text = currentDate.ToString("yyyy/MM/dd");
        for (int i = 0; i < newsBarPanels.Count; i++) {
            NewsBarPanel newsBarPanel = newsBarPanels[i];
            bool visible = newsBarPanel.UpdateDate(currentDate);
            newsBarPanel.gameObject.SetActive(visible);
            if (!visible && currentNewsBarPanelIndex > 0)
                currentNewsBarPanelIndex -= 1;
        }
        UpdateNewsBarPanelPositions();
    }

    public void OnEventTriggered(WorldEvent triggeredEvent) {
        eventTriggeredDialog.gameObject.SetActive(true);
        eventTriggeredDialog.ShowEventDialog(triggeredEvent);
    }

    public void PushLatestNews(News latestNews) {
        NewsBarPanel availableNewsBarPanel = null;
        for (int i = 0; i < newsBarPanels.Count; i++) {
            if (!newsBarPanels[i].gameObject.activeSelf) {
                availableNewsBarPanel = newsBarPanels[i];
                break;
            }
        }
        if (availableNewsBarPanel == null) {
            Debug.LogWarning($"GameHudController : maximum displayed news count reached ({maximumNewsCount}).");
            return;
        }
        availableNewsBarPanel.UpdateDisplayedNews(latestNews);
        availableNewsBarPanel.gameObject.SetActive(true);
        UpdateNewsBarPanelPositions();
    }

    private void UpdateNewsBarPanelPositions() {
        int visiblesCount = 0;
        float panelHeight = Mathf.Abs(newsBarPanelModel.gameObject.GetComponent<RectTransform>().sizeDelta.y);
        for (int i = 0; i < newsBarPanels.Count; i++) {
            NewsBarPanel newsBarPanel = newsBarPanels[i];
            if (!newsBarPanel.gameObject.activeSelf) continue;
            float positionY = ++visiblesCount * (panelHeight + marginBetweenPanels);
            newsBarPanel.GetComponent<RectTransform>().anchoredPosition  =
                new Vector3(Screen.width / 2f + 10f, positionY, 0); // TODO : resolution independant
        }
    }
}
