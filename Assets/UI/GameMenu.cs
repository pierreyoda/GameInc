using UnityEngine;

public class GameMenu : MonoBehaviour {
    public void ShowMenu() {
        gameObject.SetActive(true);
    }

    public void HideMenu() {
        gameObject.SetActive(false);
    }
}
