using UnityEngine;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour {
    [SerializeField] private Button buildButton;
    [SerializeField] private Button projectButton;
    [SerializeField] private Button staffButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button systemButton;

    public void ApplyTheme(UserInterfaceTheme theme) {
        ApplyButtonColor(buildButton, theme.BuildColor);
        ApplyButtonColor(projectButton, theme.ProjectColor);
        ApplyButtonColor(staffButton, theme.StaffColor);
        ApplyButtonColor(infoButton, theme.InfoColor);
        ApplyButtonColor(systemButton, theme.SystemColor);
    }

    private static void ApplyButtonColor(Button button, Color color) {
        ColorBlock colorBlock = button.colors;
        colorBlock.highlightedColor = color;
        button.colors = colorBlock;
    }

    public void ShowMenu() {
        gameObject.SetActive(true);
    }

    public void HideMenu() {
        gameObject.SetActive(false);
    }
}
