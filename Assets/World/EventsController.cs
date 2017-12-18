using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

    public delegate float VariableFloat(EventsController ec, DateTime d, GameDevCompany c);
    public delegate bool TriggerCondition(EventsController ec, DateTime d, GameDevCompany c);
    public delegate void TriggerAction(EventsController ec, DateTime d, GameDevCompany c);

    [SerializeField] private List<WorldEvent> worldEvents = new List<WorldEvent>();
    [SerializeField] private List<EventVariable> eventsVariables = new List<EventVariable>();
    [SerializeField] private List<WorldEvent> eventsObservingGameDate = new List<WorldEvent>();
    [SerializeField] private List<WorldEvent> eventsObservingPlayerCompany = new List<WorldEvent>();

    public void InitEvents(List<Event> events, List<Text> textsCollection) {
        foreach (Event e in events) {
            // Parse the variable declarations
            foreach (string declaration in e.VariablesDeclarations) {
                EventVariable variable = ParseVariableDeclaration(declaration);
                if (variable == null) {
                    Debug.LogError($"EventsController - Event variable declaration parsing error for Event (ID = {e.Id}).");
                    break;
                }
                EventVariable existing = eventsVariables.Find(v => v.Name == variable.Name);
                if (existing != null) {
                    Debug.LogWarning($"EventsController - Event variable \"@{variable.Name}\" assignment erases a previous one in Event of ID = {e.Id}.");
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
            WorldEvent worldEvent = new WorldEvent(e, descriptionText, conditions, actions);
            worldEvents.Add(worldEvent);

            // Sort by observed game object
            foreach (string gameObject in e.ObservedObjects) {
                if (gameObject.StartsWith("World.CurrentDate"))
                    eventsObservingGameDate.Add(worldEvent);
                if (gameObject.StartsWith("Company"))
                    eventsObservingPlayerCompany.Add(worldEvent);
            }
        }
    }

    /// <summary>
    /// Initialize the event variables.
    /// </summary>
    public void InitVariables(DateTime d, GameDevCompany c) {
        foreach (EventVariable variable in eventsVariables) {
            SetVariable(variable.Name, variable.Assignment(this, d, c));
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
        List<string> unactiveEventsIDs = new List<string>();
        foreach (WorldEvent we in eventsObservingGameDate) {
            if (we.CheckEvent(this, gameDate, playerCompany))
                unactiveEventsIDs.Add(we.Info.Id);
        }

        foreach (string unactiveID in unactiveEventsIDs) {
            eventsObservingGameDate.RemoveAll(we => we.Info.Id == unactiveID);
            eventsObservingPlayerCompany.RemoveAll(we => we.Info.Id == unactiveID);
        }
    }

    public void OnPlayerCompanyChanged(DateTime gameDate, GameDevCompany playerCompany) {
        foreach (WorldEvent we in eventsObservingPlayerCompany) {
            we.CheckEvent(this, gameDate, playerCompany);
        }
    }

    private static EventVariable ParseVariableDeclaration(string declaration) {
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
        VariableFloat variable = (ec, d, c) => {
            float value = variableValue(ec, d, c);
            ec.SetVariable(variableName, value);
            return value;
        };
        return new EventVariable(variableName, variable);
    }

    private static TriggerCondition ParseTriggerCondition(string condition) {
        string[] tokens = condition.Split(' ');
        if (tokens.Length != 3) return null;

        VariableFloat leftValue = ParseOperandFloat(tokens[0].Trim());
        VariableFloat rightValue = ParseOperandFloat(tokens[2].Trim());
        switch (tokens[1].Trim()) {
            case "<": return (ec, d, c) => leftValue(ec, d, c) < rightValue(ec, d, c);
            case "<=": return (ec, d, c) => leftValue(ec, d, c) <= rightValue(ec, d, c);
            case ">": return (ec, d, c) => leftValue(ec, d, c) > rightValue(ec, d, c);
            case ">=": return (ec, d, c) => leftValue(ec, d, c) >= rightValue(ec, d, c);
            case "==": return (ec, d, c) => Math.Abs(leftValue(ec, d, c) - rightValue(ec, d, c)) < 0.00001;
        }
        return null;
    }

    private static TriggerAction ParseTriggerAction(string action) {
        string[] tokens = action.Split(' ');
        if (tokens.Length == 0) return null;
        string leftName = tokens[0].Trim();

        if (leftName.StartsWith("Company.EnableFeature") ||
            leftName.StartsWith("Company.DisableFeature")) {
            int posLeftParenthesis = leftName.IndexOf('(');
            int posRightParenthesis = leftName.IndexOf(')');
            string featureName = leftName.Substring(posLeftParenthesis + 1,
                posRightParenthesis - posLeftParenthesis - 1);
            if (!GameDevCompany.SUPPORTED_FEATURES.Contains(featureName)) {
                Debug.LogError($"EventsController.ParseTriggerAction(\"{action}\") : unsupported Company method.");
            }

            bool enable = leftName.StartsWith("Company.EnableFeature");
            return (ec, d, c) => c.SetFeature(featureName, enable);
        }

        if (tokens.Length != 3) return null;
        string variableName = leftName.Substring(1);
        string operation = tokens[1].Trim();
        VariableFloat rightValue = ParseOperandFloat(tokens[2].Trim());

        // Game variable
        if (leftName.StartsWith("$")) {
            switch (variableName) {
                case "Company.Money":
                    return (ec, d, c) => {
                        switch (operation) {
                            case "+=":
                                c.Pay(rightValue(ec, d, c));
                                break;
                            case "-=":
                                c.Charge(rightValue(ec, d, c));
                                break;
                        }
                    };
                case "Company.NeverBailedOut":
                    if (operation != "=") {
                        Debug.LogError($"EventsController.ParseTriggerAction(\"{action}\") : unsupported operation.");
                    }
                    return (ec, d, c) => c.NeverBailedOut = Math.Abs(rightValue(ec, d, c) - 1f) < 0.000001;
                default:
                    Debug.LogError($"EventsController.ParseTriggerAction(\"{action}\") : unsupported variable.");
                    return null;
            }
        }

        // Event variable
        // TODO - get rid of GetVariable(.), using a new EventVariableFloat delegate ?
        if (leftName.StartsWith("@")) {
            switch (operation) {
                case "=": return (ec, d, c) => ec.SetVariable(variableName, rightValue(ec, d, c));
                case "+=": return (ec, d, c) => ec.SetVariable(variableName, ec.GetVariable(variableName) + rightValue(ec, d, c));
                case "-=": return (ec, d, c) => ec.SetVariable(variableName, ec.GetVariable(variableName) - rightValue(ec, d, c));
            }
        }
        return null;
    }

    public static VariableFloat ParseOperandFloat(string operand) {
        // Game variables
        if (operand.StartsWith("$")) {
            switch (operand.Substring(1)) {
                case "World.CurrentDate.Year": return (ec, d, c) => (float) d.Year;
                case "World.CurrentDate.Month": return (ec, d, c) => (float) d.Month;
                case "World.CurrentDate.Day": return (ec, d, c) => (float) d.Day;
                case "Company.Money": return (ec, d, c) => c.Money;
                case "Company.NeverBailedOut" : return (ec, d, c) => c.NeverBailedOut ? 1f : 0f;
                case "Company.Projects.CompletedGames.Count": return (ec, d, c) => c.CompletedProjects.Games.Count;
                default:
                    Debug.LogError($"EventsController.ParseOperandFloat(\"{operand}\") : unkown variable.");
                    return null;
            }
        }
        // Event variables
        if (operand.StartsWith("@")) {
            return (ec, d, c) => ec.GetVariable(operand.Substring(1));
        }
        // Boolean - TODO : improve handling
        if (operand == "true") return (ec, d, c) => 1f;
        if (operand == "false") return (ec, d, c) => 0f;
        // Constant
        float value = float.Parse(operand, CultureInfo.InvariantCulture.NumberFormat);
        return (ec, d, c) => value;
    }
}
