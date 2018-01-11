using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Event = Database.Event;

public class EventTriggeredDialog : MonoBehaviour {
    [SerializeField] private WorldEvent triggeredEvent;
    [SerializeField] private Text dialogTitle;
    [SerializeField] private Text dialogDescription;
    [SerializeField] private Button buttonConfirm;

    private void Start() {
        Assert.IsNotNull(buttonConfirm);
        buttonConfirm.onClick.AddListener(DismissEventDialog);
    }

    public void ShowEventDialog(WorldEvent eventToDisplay) {
        triggeredEvent = eventToDisplay;
        dialogTitle.text = triggeredEvent.Info.TitleEnglish;
        dialogDescription.text = triggeredEvent.ComputedDescription;
        gameObject.SetActive(true);
    }

    public void DismissEventDialog() {
        gameObject.SetActive(false);
    }
}
