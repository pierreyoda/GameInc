using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Event = Database.Event;
using static ScriptParser;

[Serializable]
public class WorldEvent {
    [SerializeField] private Event info;
    public Event Info => info;

    [SerializeField] private int triggersCount = 0;
    public int TriggersCount => triggersCount;

    private VariableFloat triggersLimit;

    [SerializeField] private List<ScriptCondition> conditions;
    [SerializeField] private List<ScriptAction> actions;

    public WorldEvent(Event info,
        List<ScriptCondition> conditions,
        List<ScriptAction> actions) {
        this.info = info;
        this.conditions = conditions;
        this.actions = actions;

        triggersLimit = ParseExpressionFloat(info.TriggerLimit.Split(' '));
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
        foreach (ScriptCondition condition in conditions) {
            if (!condition(ec, d, c)) return false;
        }

        // action when triggered
        Debug.Log($"WorldEvent - Event \"{info.Id}\" triggered ! Triggers count = {triggersCount}, limit = {limit}.");
        ++triggersCount;
        foreach (ScriptAction action in actions) {
            action(ec, d, c);
        }
        string desc = ComputeDescription(ec, d, c);
        Debug.Log($"=== Event description:\n{desc}\n===");

        return false;
    }

    private string ComputeDescription(EventsController ec, DateTime d, GameDevCompany c) {
        string desc = "";
        foreach (string line in info.DescriptionEnglish.Split('\n')) {
            foreach (string token in line.Split(' ')) {
                // Variable
                if (token.StartsWith("{")) {
                    string[] parameters = GetInnerParameters(token, "{", "}");
                    if (parameters == null) {
                        Debug.LogError($"WorldEvent.ComputeDescription (ID = {info.Id}) : variable reference parsing error.");
                        desc += "{PARSING_ERROR} ";
                        continue;
                    }
                    if (parameters.Length != 1) {
                        Debug.LogError($"WorldEvent.ComputeDescription (ID = {info.Id}) : invalid variable reference.");
                        desc += "{PARSING_ERROR} ";
                        continue;
                    }

                    int referenceEnd = token.IndexOf('}');
                    Assert.IsTrue(0 < referenceEnd && referenceEnd < token.Length);
                    string variableReference = token.Substring(1, referenceEnd - 1);
                    string postReference = token.Substring(referenceEnd + 1);

                    string variableName = variableReference.Substring(1);
                    if (variableReference.StartsWith("@")) { // Event variable
                        desc += $"{ec.GetVariable(variableName)}{postReference} ";
                    } else if (variableReference.StartsWith("$")) {
                        // Game variable
                        desc += $"{ec.GetGameVariable(variableName, d, c)}{postReference} ";
                    } else {
                        Debug.LogError($"WorldEvent.ComputeDescription (ID = {info.Id}) : invalid variable reference.");
                        desc += "{PARSING_ERROR} ";
                    }
                    continue;
                }
                // Text
                desc += $"{token} ";
            }
            desc += "\n";
        }
        return desc;
    }
}