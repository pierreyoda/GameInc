using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSubMenu : MonoBehaviour {
	[SerializeField] private bool opened = false;

	[SerializeField] private Button submenuToggleButton;
	private List<Button> buttons = new List<Button>();

	private void Start () {
		for (int i = 0; i < gameObject.transform.childCount; i++) {
			buttons.Add(gameObject.transform.GetChild(i).GetComponent<Button>());
		}
		UpdateButtons();
	}

	public void ToggleSubMenu() {
		opened = !opened;
		UpdateButtons();
	}

	private void UpdateButtons() {
		foreach (var button in buttons) {
			button.gameObject.SetActive(opened);
		}
	}
}
