using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class NewGameDialog : MonoBehaviour {
    [SerializeField] private string choosePlatformText = "Choose a Platform.";
    [SerializeField] private string chooseEngineText = "Choose a Game Engine.";

    private Transform dialogContentObject;
    private Transform dialogFooterPanel;
    private InputField textfieldName;
    private Dropdown dropdownGenre;
    private Dropdown dropdownTheme;
    private Dropdown dropdownPlatform;
    private Dropdown dropdownEngine;
    private Button buttonConfirm;
    private Button buttonCancel;

    private Database.Database database;
    private IEnumerable<GameEngine> gameEngines;

    private Action<GameProject> submitNewGameDialog;

    private void Start() {
        dialogContentObject = transform.Find("DialogContent");
        dialogFooterPanel = transform.Find("DialogFooterPanel");
        textfieldName = dialogContentObject.Find("TextFieldName").GetComponent<InputField>();
        dropdownGenre = dialogContentObject.Find("DropdownGenre").GetComponent<Dropdown>();
        dropdownTheme = dialogContentObject.Find("DropdownTheme").GetComponent<Dropdown>();
        dropdownPlatform = dialogContentObject.Find("DropdownPlatform").GetComponent<Dropdown>();
        dropdownEngine = dialogContentObject.Find("DropdownEngine").GetComponent<Dropdown>();
        buttonConfirm = dialogFooterPanel.Find("DialogConfirmButton").GetComponent<Button>();
        buttonCancel = dialogFooterPanel.Find("DialogCancelButton").GetComponent<Button>();

        gameObject.SetActive(false);
        buttonCancel.onClick.AddListener(CancelNewGame);
        textfieldName.onValueChanged.AddListener(OnNameChanged);
        dropdownGenre.onValueChanged.AddListener(OnDropdownChanged);
        dropdownTheme.onValueChanged.AddListener(OnDropdownChanged);
        dropdownPlatform.onValueChanged.AddListener(OnPlatformChanged);
        dropdownEngine.onValueChanged.AddListener(OnEngineChanged);
        buttonConfirm.onClick.AddListener(ConfirmNewGame);
        buttonConfirm.interactable = false;

        dropdownPlatform.options.Clear();
        dropdownPlatform.AddOptions(new List<string> { choosePlatformText });
        dropdownEngine.options.Clear();
        dropdownEngine.AddOptions(new List<string> { chooseEngineText });
    }

    public void ShowDialog(DateTime gameDateTime,
        Database.Database gameDatabase,
        IEnumerable<GameEngine> engines,
        Action<GameProject> onSubmit) {
        database = gameDatabase;
        gameEngines = engines;
        submitNewGameDialog = onSubmit;

        // Populate dropdowns
        List<string> genresOptions = new List<string>();
        List<string> themesOptions = new List<string>();
        List<string> platformsOptions = new List<string>();
        List<string> enginesOptions = new List<string>();
        foreach (Genre genre in database.Genres.Collection.OrderBy(g => g.Name)) {
            genresOptions.Add(genre.Name);
        }
        foreach (Theme theme in database.Themes.Collection.OrderBy(t => t.Name)) {
            themesOptions.Add(theme.Name);
        }
        foreach (Platform platform in database.Platforms.Collection
            .Where(p => p.ReleaseDate <= gameDateTime)
            .OrderBy(p => p.Name)) {
            platformsOptions.Add(platform.Name);
        }
        foreach (GameEngine gameEngine in gameEngines.OrderBy(e => e.ReleaseDate)) {
            enginesOptions.Add(gameEngine.Name);
        }
        dropdownGenre.AddOptions(genresOptions);
        dropdownTheme.AddOptions(themesOptions);
        dropdownPlatform.AddOptions(platformsOptions);
        dropdownEngine.AddOptions(enginesOptions);

        buttonConfirm.interactable = IsFormValid();
        gameObject.SetActive(true);
    }

    public void HideDialog() {
        gameObject.SetActive(false);
    }

    private void CancelNewGame() {
        HideDialog();
    }

    public void ConfirmNewGame() {
        if (!IsFormValid()) {
            Debug.LogWarning("NewGameDialog.ConfirmNewGame : invalid form.");
            return;
        }

        string gameName = textfieldName.text.Trim();
        Genre genre = database.Genres.FindFirstByName(GetDropdownSelection(dropdownGenre));
        Theme theme = database.Themes.FindFirstByName(GetDropdownSelection(dropdownTheme));
        string platformId = database.Platforms.FindFirstByName(GetDropdownSelection(dropdownPlatform)).Id;
        GameEngine engine = null;
        string selectedEngine = GetDropdownSelection(dropdownEngine);
        foreach (GameEngine e in gameEngines) {
            if (e.Name == selectedEngine) {
                engine = e;
                break;
            }
        }
        Assert.IsNotNull(engine);
        GameProject newGameProject = new GameProject(gameName, genre, theme, engine,
            new List<string> { platformId });
        submitNewGameDialog(newGameProject);
    }

    private void OnNameChanged(string text) { ValidateForm(); }
    private void OnDropdownChanged(int value) { ValidateForm(); }
    private void ValidateForm() { buttonConfirm.interactable = IsFormValid(); }

    private bool IsFormValid() {
        if (textfieldName.text.Trim().Length == 0) return false;
        if (dropdownGenre.value == 0 || dropdownTheme.value == 0) return false;
        string platform = GetDropdownSelection(dropdownPlatform);
        string engine = GetDropdownSelection(dropdownEngine);
        if (platform == choosePlatformText) return false;
        if (engine == chooseEngineText) return false;
        return true;
    }

    private void OnPlatformChanged(int value) {
        string currentEngine = GetDropdownSelection(dropdownEngine);
        dropdownEngine.ClearOptions();
        string platform = dropdownPlatform.options[value].text;
        bool allEngines = platform == choosePlatformText;
        string platformId = allEngines ? "" : database.Platforms.FindFirstByName(platform).Id;
        List<string> engineOptions = new List<string>();
        engineOptions.Add(chooseEngineText);
        foreach (GameEngine gameEngine in gameEngines) {
            foreach (string id in gameEngine.SupportedPlatformsIDs) {
                if ((allEngines || id == platformId) &&
                    !engineOptions.Contains(gameEngine.Name)) { // compatible engine
                    engineOptions.Add(gameEngine.Name);
                    break;
                }
            }
        }
        dropdownEngine.onValueChanged.RemoveAllListeners(); // avoid stack overflow
        dropdownEngine.AddOptions(engineOptions);
        //dropdownEngine.onValueChanged.AddListener(OnEngineChanged); // not needed ??

        // Select previous engine if available
        dropdownEngine.value = 0;
        for (int i = 0; i < dropdownEngine.options.Count; i++) {
            var option = dropdownEngine.options[i];
            if (option.text == currentEngine) {
                dropdownEngine.value = i;
                break;
            }
        }

        ValidateForm();
    }

    private void OnEngineChanged(int value) {
        string currentPlatform = GetDropdownSelection(dropdownPlatform);
        dropdownPlatform.ClearOptions();
        string engine = dropdownEngine.options[value].text;
        bool allPlatforms = engine == chooseEngineText;
        List<string> platformOptions = new List<string>();
        platformOptions.Add(choosePlatformText);
        foreach (GameEngine gameEngine in gameEngines) {
            if (allPlatforms || gameEngine.Name == engine) { // compatible platform
                foreach (string platformId in gameEngine.SupportedPlatformsIDs) {
                    string platformName = database.Platforms.FindById(platformId).Name;
                    if (platformOptions.Contains(platformName)) continue;
                    platformOptions.Add(platformName);
                }
            }
        }
        dropdownPlatform.onValueChanged.RemoveAllListeners();
        dropdownPlatform.AddOptions(platformOptions);
        //dropdownPlatform.onValueChanged.AddListener(OnPlatformChanged);

        // Select previous platform if available
        dropdownPlatform.value = 0;
        for (int i = 0; i < dropdownPlatform.options.Count; i++) {
            var option = dropdownPlatform.options[i];
            if (option.text == currentPlatform) {
                dropdownPlatform.value = i;
                break;
            }
        }

        ValidateForm();
    }

    private string GetDropdownSelection(Dropdown dropdown) {
        return dropdown.options.Count == 0 ? "" :
            dropdown.options[dropdown.value].text;
    }
}       
