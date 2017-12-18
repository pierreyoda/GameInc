using System;
using System.Collections.Generic;
using System.Globalization;
using Database;
using UnityEngine;
using Event = Database.Event;

public class EventsController : MonoBehaviour {
    [Serializable]
    private class EventVariable {
        [SerializeField] private string name;
        public string Name => name;

        [SerializeField] private VariableFloat assignment;
        public VariableFloat Assignment {
            get { return assignment; }
            set { assignment = value; }
        }

        [SerializeField] private float value = 0f;
        public float Value {
            get { return value; }
            set { this.value = value; }
        }

        public EventVariable(string name, VariableFloat assignment) {
            this.name = name;
            this.assignment = assignment;
        }
    }

    private delegate float VariableFloat(DateTime date, GameDevCompany company);
    private delegate bool TriggerCondition(DateTime date, GameDevCompany company);
    private delegate void TriggerAction(DateTime date, GameDevCompany company);

    private readonly List<Tuple<Event, Text, List<TriggerCondition>, List<TriggerAction>>> eventsTriggers =
        new List<Tuple<Event, Text, List<TriggerCondition>, List<TriggerAction>>>();
    [SerializeField] private List<EventVariable> eventsVariables = new List<EventVariable>();
    [SerializeField] private List<Event> eventsObservingGameDate = new List<Event>();
    [SerializeField] private List<Event> eventsObservingPlayerCompany = new List<Event>();

    public void InitEvents(List<Event> events, List<Text> textsCollection) {
        foreach (Event e in events) {
            // Parse the variable declarations
            foreach (string declaration in e.VariablesDeclarations) {
                EventVariable variable = ParseVariableDeclaration(declaration);
                if (variable == null) {
                    Debug.LogError($"EventsController - Event variable declaration parsing error for Event (ID = {e.Id}.");
                    break;
                }
                EventVariable existing = eventsVariables.Find(v => v.Name == variable.Name);
                if (existing != null) {
                    Debug.LogWarning($"EventsController - Event variable \"@{variable.Name}\" assignment erases a previous one.");
                    existing.Assignment = variable.Assignment;
                }
                else {
                    eventsVariables.Add(variable);
                }
            }

            // Parse the trigger conditions and actions
            List<TriggerCondition> conditions = new List<TriggerCondition>();
            List<TriggerAction> actions = new List<TriggerAction>();
            foreach (string condition in e.TriggerConditions) {
                TriggerCondition trigger = ParseTriggerCondition(condition);
                if (trigger == null) {
                    Debug.LogError($"EventsController - Condition parsing error for Event (ID = {e.Id}).");
                    break;
                }
                conditions.Add(trigger);
            }
            foreach (string action in e.TriggerActions) {
                TriggerAction trigger = ParseTriggerAction(action);
                if (trigger == null) {
                    Debug.LogError($"EventsController - Action parsing error for Event (ID = {e.Id}).");
                    break;
                }
                actions.Add(trigger);
            }

            // Get the description (if starts with '$' : try to load from associated Text in database)
            Text descriptionText = null;
            if (!e.Description.StartsWith("$")) {
                descriptionText = new Text($"_GENERATED_DESCRIPTION_FOR_EVENT_{e.Id}",
                    new[] {e.Description});
            } else {
                string descriptionId = e.Description.Substring(1);
                descriptionText = textsCollection.Find(t => t.Id == descriptionId);
            }

            // Store the event and its computed informations
            if (descriptionText == null) {
                Debug.LogError($"EventsController - Invalid description ID for Event (ID = {e.Id}).");
                continue;
            }
            eventsTriggers.Add(new Tuple<Event, Text, List<TriggerCondition>, List<TriggerAction>>(
                e, descriptionText, conditions, actions));

            // Sort by observed game object
            foreach (string gameObject in e.ObservedObjects) {
                if (gameObject == "World.CurrentDate")
                    eventsObservingGameDate.Add(e);
                if (gameObject == "Company")
                    eventsObservingPlayerCompany.Add(e);
            }
        }
    }

    /// <summary>
    /// Initialize the event variables.
    /// </summary>
    public void InitVariables(DateTime d, GameDevCompany c) {
        foreach (EventVariable variable in eventsVariables) {
            SetVariable(variable.Name, variable.Assignment(d, c));
        }
    }

    public float GetVariable(string variableName) {
        var variable = eventsVariables.Find(v => v.Name == variableName);
        if (variable == null) {
            Debug.LogError($"EventsController.GetVariable(\"{variableName}\") : unkown event variable.");
            return 0f;
        }
        return variable.Value;
    }

    public void SetVariable(string variableName, float value) {
        var variable = eventsVariables.Find(v => v.Name == variableName);
        if (variable == null) {
            Debug.LogError($"EventsController.SetVariable(\"{variableName}\", {value}) : unkown event variable.");
            return;
        }
        variable.Value = value;
        Debug.Log($"EventsController : @{variableName} = {value}");
    }

    public void OnGameDateChanged(DateTime gameDate, GameDevCompany playerCompany) {
        foreach (Event e in eventsObservingGameDate) {
            CheckEvent(e.Id, gameDate, playerCompany);
        }
    }

    public void OnPlayerCompanyChanged(DateTime gameDate, GameDevCompany playerCompany) {
        foreach (Event e in eventsObservingPlayerCompany) {
            CheckEvent(e.Id, gameDate, playerCompany);
        }
    }

    private void CheckEvent(string eventId, DateTime d, GameDevCompany c) {
        var eventTriggers = eventsTriggers.Find(ea => ea.Item1.Id == eventId);
        // condition check
        bool triggered = true;
        foreach (TriggerCondition condition in eventTriggers.Item3) {
            if (!condition(d, c)) {
                triggered = false;
                break;
            }
        }
        // action when triggered
        if (!triggered) return;
        Debug.Log($"EventsController - Event \"{eventId}\" triggered !");
        foreach (TriggerAction action in eventTriggers.Item4) {
            action(d, c);
        }
        string description = ComputeDescription(eventTriggers.Item2, d, c);
        Debug.Log($"=== Event description:\n{description}\n===");
    }

    // TODO : support game variables (for instance "$Domain.MyVariable")
    public string ComputeDescription(Text descriptionText, DateTime d,
        GameDevCompany c) {
        string description = "";
        foreach (string line in descriptionText.TextEnglish) {
            foreach (string token in line.Split(' ')) {
                description += token.StartsWith("@") ? $" {GetVariable(token.Substring(1))}" : $" {token}";
            }
            description += "\n";
        }
        return description;
    }

    private EventVariable ParseVariableDeclaration(string declaration) {
        string[] tokens = declaration.Split(' ');
        if (tokens.Length != 3) return null;
        if (!tokens[0].StartsWith("@")) {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : must assign an event variable (\"@variable\").");
            return null;
        }
        string variableName = tokens[0].Trim().Substring(1);
        if (variableName.Length == 0) {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : empty event variable name.");
            return null;
        }

        string operation = tokens[1].Trim();
        if (operation != "=") {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : can only assign the variable value.");
            return null;
        }

        string rightValue = tokens[2].Trim();
        if (rightValue == $"@{variableName}") {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : illegal assignment.");
            return null;
        }

        VariableFloat variableValue = ParseOperandFloat(tokens[2].Trim());
        VariableFloat variable = (d, c) => {
            float value = variableValue(d, c);
            SetVariable(variableName, value);
            return value;
        };
        return new EventVariable(variableName, variable);
    }

    private TriggerCondition ParseTriggerCondition(string condition) {
        string[] tokens = condition.Split(' ');
        if (tokens.Length != 3) return null;

        VariableFloat leftValue = ParseOperandFloat(tokens[0].Trim());
        VariableFloat rightValue = ParseOperandFloat(tokens[2].Trim());
        switch (tokens[1].Trim()) {
            case "<": return (d, c) => leftValue(d, c) < rightValue(d, c);
            case "<=": return (d, c) => leftValue(d, c) <= rightValue(d, c);
            case ">": return (d, c) => leftValue(d, c) > rightValue(d, c);
            case ">=": return (d, c) => leftValue(d, c) >= rightValue(d, c);
            case "==": return (d, c) => Math.Abs(leftValue(d, c) - rightValue(d, c)) < 0.00001;
        }
        return null;
    }

    private TriggerAction ParseTriggerAction(string action) {
        string[] tokens = action.Split(' ');
        if (tokens.Length != 3) return null;
        string leftName = tokens[0].Trim();
        string variableName = leftName.Substring(1);
        string operation = tokens[1].Trim();
        VariableFloat rightValue = ParseOperandFloat(tokens[2].Trim());

        // Game variable
        if (leftName.StartsWith("$")) {
            switch (variableName) {
                case "Company.Money":
                    return (d, c) => {
                        switch (operation) {
                            case "+=":
                                c.Pay(rightValue(d, c));
                                break;
                            case "-=":
                                c.Charge(rightValue(d, c));
                                break;
                        }
                    };
                case "Company.NeverBailedOut":
                    if (operation != "=") {
                        Debug.LogError($"EventsController.ParseTriggerAction(\"{action}\") : unsupported operation.");
                    }
                    return (d, c) => c.NeverBailedOut = Math.Abs(rightValue(d, c) - 1f) < 0.000001;
                default:
                    Debug.LogError($"EventsController.ParseTriggerAction(\"{action}\") : unsupported variable.");
                    return null;
            }
        }
        // Event variable
        // TODO - get rid of GetVariable(.), using a new EventVariableFloat delegate ?
        if (leftName.StartsWith("@")) {
            switch (operation) {
                case "=": return (d, c) => SetVariable(variableName, rightValue(d, c));
                case "+=": return (d, c) => SetVariable(variableName, GetVariable(variableName) + rightValue(d, c));
                case "-=": return (d, c) => SetVariable(variableName, GetVariable(variableName) - rightValue(d, c));
            }
        }
        return null;
    }

    private VariableFloat ParseOperandFloat(string operand) {
        // Game variables
        if (operand.StartsWith("$")) {
            switch (operand.Substring(1)) {
                case "World.CurrentDate.Year": return (d, c) => (float) d.Year;
                case "World.CurrentDate.Month": return (d, c) => (float) d.Month;
                case "World.CurrentDate.Day": return (d, c) => (float) d.Day;
                case "Company.Money": return (d, c) => c.Money;
                case "Company.NeverBailedOut" : return (d, c) => c.NeverBailedOut ? 1f : 0f;
                default:
                    Debug.LogError($"EventsController.ParseOperandFloat(\"{operand}\") : unkown variable.");
                    return null;
            }
        }
        // Event variables
        if (operand.StartsWith("@")) {
            return (d, c) => GetVariable(operand.Substring(1));
        }
        // Boolean - TODO : improve handling
        if (operand == "true") return (d, c) => 1f;
        if (operand == "false") return (d, c) => 0f;
        // Constant
        float value = float.Parse(operand, CultureInfo.InvariantCulture.NumberFormat);
        return (d, c) => value;
    }
}
