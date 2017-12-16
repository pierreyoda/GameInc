using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildRoomSelectionMenu : MonoBehaviour {
	[SerializeField] private bool opened = false;

	[SerializeField] private WorldController worldController;

	[SerializeField] private GameObject contentGameObject;

	[SerializeField] private Button modelButton;
	private List<Button> buttons = new List<Button>();

	private void Start () {
		modelButton.gameObject.SetActive(false);
		UpdateSelectionMenu();
	}

	public void OpenSelectionMenu(List<Database.Room> rooms) {
		opened = true;

		PopulateSelectionMenu(rooms);

		UpdateSelectionMenu();
	}

	public void CloseSelectionMenu() {
		opened = false;

		UpdateSelectionMenu();
	}

	private void PopulateSelectionMenu(List<Database.Room> rooms) {
		foreach (var button in buttons) {
			Destroy(button.gameObject);
		}
		buttons.Clear();

		for (int i = 0; i < rooms.Count; i++) {
			var room = rooms[i];
			string buttonText = $"{room.Name} - {room.Cost}k";

			var roomButton = Instantiate(modelButton);
			roomButton.gameObject.SetActive(true);
			roomButton.name = $"BuildRoomButton_{room.Id}";
			roomButton.transform.SetParent(contentGameObject.transform, false);

			roomButton.onClick.AddListener(delegate { OnSelectionMade(room.Id); });
			roomButton.GetComponentInChildren<Text>().text = buttonText;

			buttons.Add(roomButton.GetComponent<Button>());
		}

	}

	private void UpdateSelectionMenu() {
		gameObject.SetActive(opened);
	}

	private void OnSelectionMade(string roomId) {
		worldController.BuildRoom(roomId);
	}
}
