using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class EventTriggeredDialog : MonoBehaviour {
    [SerializeField] private WorldEvent triggeredEvent;
    [SerializeField] private Text dialogTitle;
    [SerializeField] private Text dialogDescription;
    [SerializeField] private Button buttonConfirm;

    [SerializeField, HideInInspector] private Action<WorldEvent> onDialogDismissed;

    private void Start() {
        Assert.IsNotNull(buttonConfirm);
        buttonConfirm.onClick.AddListener(DismissEventDialog);
    }

    public void ShowEventDialog(WorldEvent eventToDisplay,
        Action<WorldEvent> onDismissal) {
        triggeredEvent = eventToDisplay;
        dialogTitle.text = triggeredEvent.ComputedTitle;
        dialogDescription.text = triggeredEvent.ComputedDescription;
        onDialogDismissed = onDismissal;
        gameObject.SetActive(true);
    }

    private void DismissEventDialog() {
        gameObject.SetActive(false);
        onDialogDismissed(triggeredEvent);
    }
}
