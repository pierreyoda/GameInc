using System;
using System.Collections.Generic;
using Database;
using UnityEngine;
using Event = Database.Event;

[Serializable]
public class WorldEvent {
    [SerializeField] private Event info;
    public Event Info => info;

    [SerializeField] private int triggersCount = 0;
    public int TriggersCount => triggersCount;

    private EventsController.VariableFloat triggersLimit;

    [SerializeField] private List<EventsController.TriggerCondition> conditions;
    [SerializeField] private List<EventsController.TriggerAction> actions;

    public WorldEvent(Event info,
        List<EventsController.TriggerCondition> conditions,
        List<EventsController.TriggerAction> actions) {
        this.info = info;
        this.conditions = conditions;
        this.actions = actions;

        triggersLimit = EventsController.ParseExpressionFloat(info.TriggerLimit.Split(' '));
        if (triggersLimit == null) {
            Debug.LogError($"WorldEvent (Info.Id = {info.Id}) : trigger limit parsing error in \"{info.TriggerLimit}\".");
            triggersLimit = (ec, d, c) => -1; // -1 : no limit
        }
    }

    /// <summary>
    /// Check the WorldEvent's trigger conditions with the current game state.
    /// If every one of them evaluates to True, trigger the actions.
    /// <returns>True if the WorldEvent cannot be triggered anymore, False otherwise.</returns>
    /// </summary>
    public bool CheckEvent(EventsController ec, DateTime d, GameDevCompany c) {
        // trigger limits check
        int limit = (int) triggersLimit(ec, d, c); // TODO : add and use VariableInt
        if (limit >= 0 && triggersCount >= limit) {
            Debug.Log($"WorldEvent - Event \"{info.Id}\" reached its triggers limit ({limit}).");
            return true;
        }

        // condition check : all conditions must evaluate to True
        foreach (EventsController.TriggerCondition condition in conditions) {
            if (!condition(ec, d, c)) return false;
        }

        // action when triggered
        Debug.Log($"WorldEvent - Event \"{info.Id}\" triggered ! Triggers count = {triggersCount}, limit = {limit}.");
        ++triggersCount;
        foreach (EventsController.TriggerAction action in actions) {
            action(ec, d, c);
        }
        string desc = ComputeDescription(ec);
        Debug.Log($"=== Event description:\n{desc}\n===");

        return false;
    }

    // TODO : support game variables (for instance "$Domain.MyVariable")
    private string ComputeDescription(EventsController ec) {
        string desc = "";
        foreach (string line in info.DescriptionEnglish.Split('\n')) {
            foreach (string token in line.Split(' ')) {
                desc += token.StartsWith("@") ? $" {ec.GetVariable(token.Substring(1))}" : $" {token}";
            }
            desc += "\n";
        }
        return desc;
    }
}