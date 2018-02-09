using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class DialogsController: MonoBehaviour {
    [SerializeField] private GameHudController hudController;

    [SerializeField] private bool pauseOnNewProjectDialog = false;
    [SerializeField] private bool pauseOnTriggeredEventDialog = true;

    [SerializeField] private NewGameDialog newGameDialog;

    [SerializeField] private Queue<WorldEvent> triggeredEvents
        = new Queue<WorldEvent>();
    [SerializeField] private EventTriggeredDialog triggeredEventDialog;

    [SerializeField, HideInInspector] private bool eventDialogOpened;

    private void Start() {
        Assert.IsNotNull(hudController);
        Assert.IsNotNull(newGameDialog);
        Assert.IsNotNull(triggeredEventDialog);
        newGameDialog.gameObject.SetActive(false);
        triggeredEventDialog.gameObject.SetActive(false);
    }

    public void PushTriggeredEvent(WorldEvent worldEvent) {
        triggeredEvents.Enqueue(worldEvent);
        if (!eventDialogOpened)
            ShowNextTriggeredEventDialog();
    }

    private void ShowNextTriggeredEventDialog() {
        WorldEvent triggeredEvent = triggeredEvents.Dequeue();
        Assert.IsNotNull(triggeredEvent);
        if (pauseOnTriggeredEventDialog)
            hudController.SetDialogPause(true);
        eventDialogOpened = true;
        triggeredEventDialog.ShowEventDialog(triggeredEvent,
            OnEventDialogDismissed);
    }

    private void OnEventDialogDismissed(WorldEvent worldEvent) {
        if (triggeredEvents.Count > 0) {
            ShowNextTriggeredEventDialog();
            return;
        }
        eventDialogOpened = false;
        if (pauseOnTriggeredEventDialog)
            hudController.SetDialogPause(false);
    }

    public void ShowNewProjectDialog(DateTime gameDate, Project.ProjectType type,
        Database.Database database, IEnumerable<GameEngine> gameEngines) {
        if (pauseOnNewProjectDialog)
            hudController.SetDialogPause(true);
        switch (type) {
            case Project.ProjectType.GameProject:
                newGameDialog.ShowDialog(gameDate, database, gameEngines,
                    gameProject => {
                        newGameDialog.HideDialog();
                        if (pauseOnNewProjectDialog)
                            hudController.SetDialogPause(false);
                        hudController.SubmitNewProjectDialog(gameProject);
                    });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}
