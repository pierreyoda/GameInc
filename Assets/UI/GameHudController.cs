using System;
using System.Collections.Generic;
using Database;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[Serializable]
public class GameHudController : MonoBehaviour {
    [Header("Control")]
    [SerializeField] private WorldController worldController;
    [SerializeField] private Button newProjectButton;

    [Header("Theme")]
    [SerializeField] private UserInterfaceTheme theme;

    [Header("Menu")]
    [SerializeField] private GameMenu menu;

    [Header("Simulation Speed")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button speedButton;
    [SerializeField, HideInInspector] private bool speedIsNormal = true;
    [SerializeField, Range(1, 50)] private int speedMultiplier = 5;

    [Header("Company Summary")]
    [SerializeField] private Text currentDateText;
    [SerializeField] private Text playerCompanyNameText;
    [SerializeField] private Text playerCompanyMoneyText;

    [Header("Dialogs")]
    [SerializeField] private bool pauseGameInDialog = true;
    public bool PauseGameInDialog => pauseGameInDialog;

    [SerializeField] private DialogsController dialogsController;

    [Header("News Prompter")]
    [SerializeField] [Range(1, 10)] private int maximumNewsCount = 3;
    [SerializeField] [Range(1f, 50f)] private float marginBetweenPanels = 2f;
    [SerializeField] private Transform newsBarPanelsParent;
    [SerializeField] private NewsBarPanel newsBarPanelModel;
    [SerializeField] private List<NewsBarPanel> newsBarPanels = new List<NewsBarPanel>();
    private int currentNewsBarPanelIndex = 0;

    private void Start() {
        Assert.IsNotNull(worldController);
        Assert.IsNotNull(newProjectButton);
        Assert.IsNotNull(theme);
        Assert.IsNotNull(menu);
        Assert.IsNotNull(pauseButton);
        Assert.IsNotNull(speedButton);
        Assert.IsNotNull(currentDateText);
        Assert.IsNotNull(playerCompanyMoneyText);
        Assert.IsNotNull(playerCompanyMoneyText);
        Assert.IsNotNull(newsBarPanelModel);
        Assert.IsNotNull(newsBarPanelsParent);

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

    public void OnSimulationStarted() {
        UpdateSimulationButtons();
    }

    public void CanStartNewProject(bool allowed) {
        newProjectButton.interactable = allowed;
    }

    public void ShowNewProjectDialog(DateTime gameDate, Project.ProjectType type,
        Database.Database database, IEnumerable<GameEngine> gameEngines) {
        dialogsController.ShowNewProjectDialog(gameDate, type, database, gameEngines);
    }

    public void SubmitNewProjectDialog(Project newGameProject) {
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
                new Vector3(Screen.width / 2f + 10f, positionY, 0); // TODO : resolution independent
        }
    }

    private void UpdateSimulationButtons() {
        pauseButton.GetComponentInChildren<Text>().text = worldController.IsSimulationRunning ?
            "Pause" : "Resume";
        speedButton.GetComponentInChildren<Text>().text = speedIsNormal ?
            $"x{speedMultiplier}" : "x1";
    }

    public void ToggleSimulationPause() {
        bool running = worldController.IsSimulationRunning;
        worldController.SetSimulationStatus(!running);
        UpdateSimulationButtons();
    }

    public void ToggleSimulationSpeed() {
        worldController.SetSimulationSpeed(speedIsNormal ? speedMultiplier : 1);
        speedIsNormal = !speedIsNormal;
        UpdateSimulationButtons();
    }

    /// <summary>
    /// Pause or resume the game upon opening or closing a Dialog.
    /// </summary>
    public void SetDialogPause(bool pause) {
        bool running = worldController.IsSimulationRunning;
        if (running && pause) {
            worldController.SetSimulationStatus(false);
            UpdateSimulationButtons();
        } else if (!running && !pause) {
            worldController.SetSimulationStatus(true);
            UpdateSimulationButtons();
        }
    }
}
