using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Database {

/// <summary>
/// An event occuring in the game World.
/// Supports a simple syntax for the trigger and action properties.
/// </summary>
[Serializable]
public class Event : DatabaseElement {
    public static readonly string[] SUPPORTED_VARIABLES = {
        "World.CurrentDate.Year",
        "World.CurrentDate.Month",
        "World.CurrentDate.Day",
        "Company.Money",
        "Company.NeverBailedOut",
        "Company.Projects.CompletedGames.Count",
    };

    [SerializeField] private string[] triggerConditions;
    public string[] TriggerConditions => triggerConditions;

    [SerializeField] private string[] triggerActions;
    public string[] TriggerActions => triggerActions;

    [SerializeField] private string triggerLimit;
    public string TriggerLimit => triggerLimit;

    public string Title => Name;

    [SerializeField] private string description;
    public string Description => description;

    [SerializeField] private string[] variables;
    public string[] VariablesDeclarations => variables;

    private List<string> observedObjects;
    public IReadOnlyList<string> ObservedObjects => observedObjects.AsReadOnly();

    public Event(string id, string name, string[] triggerConditions,
        string[] triggerActions, string triggerLimit, string[] variables,
        string description) : base(id, name) {
        this.triggerConditions = triggerConditions;
        this.triggerActions = triggerActions;
        this.triggerLimit = triggerLimit;
        this.description = description;
        this.variables = variables;
    }

    public override bool IsValid() {
        observedObjects = new List<string>();

        // Trigger limit check
        if (!IsTriggerLimitValid()) return false;

        // Description check
        if (description.Length == 0) {
            Debug.LogError($"Event with ID = {Id} : empty description or Text description ID.");
            return false;
        }
        if (description.StartsWith("$") && description.Length == 1) {
            Debug.LogError($"Event with ID = {Id} : empty Text description ID.");
            return false;
        }

        bool triggersValid = triggerConditions.All(trigger => IsTriggerValid(trigger, true))
                             && triggerActions.All(trigger => IsTriggerValid(trigger, false))
                             && variables.All(declaration => IsTriggerValid(declaration, false));
        return triggersValid && base.IsValid();
    }

    public bool IsTriggerLimitValid() {
        // Game Variable or Event Variable
        if (triggerLimit.StartsWith("$") || triggerLimit.StartsWith("@")) {
            if (triggerLimit.Length == 1) {
                Debug.LogError($"Event with ID = {Id} : empty variable name in trigger limit.");
                return false;
            }
            string variableName = triggerLimit.Substring(1);
            if (triggerLimit.StartsWith("$") && !SUPPORTED_VARIABLES.Contains(variableName)) {
                Debug.LogError($"Event with ID = {Id} : unknown game variable \"{variableName}\".");
                return false;
            }
            return true;
        }

        // Constant
        int limit;
        if (!int.TryParse(triggerLimit, out limit)) {
            Debug.LogError($"Event with ID = {Id} : invalid trigger limit \"{triggerLimit}\".");
            return false;
        }
        return true;
    }

    private bool IsTriggerValid(string trigger, bool isCondition) {
        if (isCondition) {
            for (int i = 0; i < trigger.Length - 1; i++) {
                if (i == 0 || trigger[i] != '=') continue;
                if (trigger[i - 1] == '<' || trigger[i - 1] == '>') continue;
                if (trigger[i + 1] != '=' && (i > 0 && trigger[i - 1] != '=')) {
                    Debug.LogError($"Event with ID = {Id} : illegal assignment in trigger condition \"{trigger}\".");
                    return false;
                }
            }
        }
        if (!isCondition && trigger.Contains("==")) {
            Debug.LogError($"Event with ID = {Id} : illegal test in trigger action \"{trigger}\".");
            return false;
        }

        foreach (string token in trigger.Split(' ')) {
            if (token.StartsWith("$")) { // game variable
                if (token.Length == 1) {
                    Debug.LogError($"Event with ID = {Id} : empty game variable name.");
                    return false;
                }

                string variableName = token.Substring(1);
                if (!SUPPORTED_VARIABLES.Contains(variableName)) {
                    Debug.LogError($"Event with ID = {Id} : unknown game variable \"{variableName}\".");
                    return false;
                }

                if (isCondition) {
                    List<string> variableGroups = variableName.Split('.').ToList();
                    variableGroups.RemoveAt(variableGroups.Count - 1);
                    string observedObject = string.Join(".", variableGroups);
                    if (!observedObjects.Contains(observedObject))
                        observedObjects.Add(observedObject);
                }
            } else if (token.StartsWith("@") && token.Length == 1) { // event variable
                Debug.LogError($"Event with ID = {Id} : empty event variable name.");
                return false;
            } else if (token.StartsWith("Company.EnableFeature") ||
                       token.StartsWith("Company.DisableFeature")) {
                int posLeftParenthesis = token.IndexOf('(');
                int posRightParenthesis = token.IndexOf(')');
                if (posLeftParenthesis == -1 || posRightParenthesis == -1
                    || posLeftParenthesis > posRightParenthesis) {
                    Debug.LogError($"Event with ID = {Id} : incorrect function call syntax.");
                    return false;
                }

                string featureName = token.Substring(posLeftParenthesis + 1,
                    posRightParenthesis - posLeftParenthesis - 1).Trim();
                if (featureName.Length == 0 ||
                    !GameDevCompany.SUPPORTED_FEATURES.Contains(featureName)) {
                    Debug.LogError($"Event with ID = {Id} : unkown CompanyFeature name \"{featureName}\".");
                    return false;
                }
            }
        }
        return true;
    }
}

}