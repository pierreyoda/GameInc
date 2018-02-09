using System;
using System.Collections.Generic;
using Script;
using UnityEngine;
using Event = Database.Event;

[Serializable]
public class EventsController : MonoBehaviour {
    [SerializeField] private DialogsController dialogsController;
    [SerializeField] private List<WorldEvent> worldEvents = new List<WorldEvent>();

    public void CreateEvents(List<Event> events,
        ParserContext parserContext) {
        foreach (Event e in events) {
            // On-Init script
            Executable onInit = Executable.FromScript(e.OnInit, parserContext);
            if (onInit == null) {
                Debug.LogError(
                     "EventsController - Event on-init script parsing " +
                    $"error for Event (ID = {e.Id}). Ignoring Event."
                );
                continue;
            }
            // Condition script
            TypedExecutable<bool> condition = TypedExecutable<bool>.FromScript(
                e.TriggerCondition, parserContext);
            if (condition == null) {
                Debug.LogError(
                     "EventsController - Condition parsing error for Event " +
                    $"(ID = {e.Id}) in \"{e.TriggerCondition}\".");
                continue;
            }
            // Action script
            Executable action = Executable.FromScript(e.TriggerAction,
                parserContext);
            if (action == null) {
                Debug.LogError(
                     "EventsController - Action parsing error for Event " +
                    $"(ID = {e.Id}) in \"{e.TriggerCondition}\".");
                continue;
            }
            // Store the WorldEvent and its computed metadata
            WorldEvent worldEvent = new WorldEvent(e, onInit, condition,
                action, parserContext);
            worldEvents.Add(worldEvent);
        }
    }

    public bool InitEvents(IScriptContext context) {
        bool noErrors = true;
        foreach (WorldEvent we in worldEvents) {
            if (!we.InitEvent(context)) noErrors = false;
        }
        return noErrors;
    }

    public void OnGameDateChanged(IScriptContext context) {
        foreach (WorldEvent we in worldEvents) {
            if (!we.Active) continue;
            bool triggered;
            we.CheckEvent(context, out triggered);
            if (triggered) dialogsController.PushTriggeredEvent(we);
        }
    }

    public void OnPlayerCompanyChanged(IScriptContext context) {
        foreach (WorldEvent we in worldEvents) {
            if (!we.Active) continue;
            bool triggered;
            we.CheckEvent(context, out triggered);
            if (triggered) dialogsController.PushTriggeredEvent(we);
        }
    }
}
