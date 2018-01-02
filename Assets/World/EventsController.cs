using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Event = Database.Event;
using static ScriptParser;

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

    [SerializeField] private List<WorldEvent> worldEvents = new List<WorldEvent>();
    [SerializeField] private List<EventVariable> eventsVariables = new List<EventVariable>();
    [SerializeField] private List<WorldEvent> eventsObservingGameDate = new List<WorldEvent>();
    [SerializeField] private List<WorldEvent> eventsObservingPlayerCompany = new List<WorldEvent>();

    public void InitEvents(List<Event> events) {
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
            List<ScriptCondition> conditions = new List<ScriptCondition>();
            List<ScriptAction> actions = new List<ScriptAction>();
            foreach (string condition in e.TriggerConditions) {
                ScriptCondition trigger = ParseCondition(condition);
                if (trigger == null) {
                    Debug.LogError($"EventsController - Condition parsing error for Event (ID = {e.Id}).");
                    break;
                }
                conditions.Add(trigger);
            }
            foreach (string action in e.TriggerActions) {
                ScriptAction trigger = ParseAction(action);
                if (trigger == null) {
                    Debug.LogError($"EventsController - Action parsing error for Event (ID = {e.Id}).");
                    break;
                }
                actions.Add(trigger);
            }

            // Store the WorldEvent and its computed metadata
            WorldEvent worldEvent = new WorldEvent(e, conditions, actions);
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

    public float GetGameVariable(string variableName, DateTime d, GameDevCompany c) {
        if (variableName.StartsWith("Company.Projects.CompletedGames.WithEngineFeature(") &&
            variableName.EndsWith(").Count")) {
            string[] parameters = GetInnerParameters(variableName);
            if (parameters.Length != 1) {
                Debug.LogError($"ScriptParser.ParseScalarFloat(\"{variableName}\") : wrong function call arity.");
                return 0f;
            }
            string featureName = parameters[0];
            return c.CompletedProjects.GamesWithEngineFeature(featureName).Count;
        }
        switch (variableName) {
            case "World.CurrentDate.Year": return d.Year;
            case "World.CurrentDate.Month": return d.Month;
            case "World.CurrentDate.Day": return d.Day;
            case "World.CurrentDate.DayOfWeek": return (float) d.DayOfWeek;
            case "Company.Money": return c.Money;
            case "Company.NeverBailedOut" : return c.NeverBailedOut ? 1f : 0f;
            case "Company.Projects.CompletedGames.Count": return c.CompletedProjects.Games.Count;
            default:
                Debug.LogError($"ScriptParser.ParseScalarFloat(\"{variableName}\") : unkown game variable.");
                return 0f;
        }
    }

    public void SetVariable(string variableName, float value) {
        var variable = eventsVariables.Find(v => v.Name == variableName);
        if (variable == null) {
            Debug.LogError($"EventsController.SetVariable(\"{variableName}\", {value}) : unkown event variable.");
            return;
        }
        variable.Value = value;
        //Debug.Log($"EventsController : @{variableName} = {value}"); // TODO : fix called twice bug
    }

    public void OnGameDateChanged(DateTime gameDate, GameDevCompany playerCompany) {
        List<string> unactiveEventsIDs = new List<string>();
        foreach (WorldEvent we in eventsObservingGameDate) {
            if (we.CheckEvent(this, gameDate, playerCompany))
                unactiveEventsIDs.Add(we.Info.Id);
        }
        ClearUnactivableWorldEvents(unactiveEventsIDs);
    }

    public void OnPlayerCompanyChanged(DateTime gameDate, GameDevCompany playerCompany) {
        List<string> unactiveEventsIDs = new List<string>();
        foreach (WorldEvent we in eventsObservingPlayerCompany) {
            if (we.CheckEvent(this, gameDate, playerCompany))
                unactiveEventsIDs.Add(we.Info.Id);
        }
        ClearUnactivableWorldEvents(unactiveEventsIDs);
    }

    private void ClearUnactivableWorldEvents(List<string> unactiveEventsIDs) {
        foreach (string unactiveID in unactiveEventsIDs) {
            eventsObservingGameDate.RemoveAll(we => we.Info.Id == unactiveID);
            eventsObservingPlayerCompany.RemoveAll(we => we.Info.Id == unactiveID);
        }
    }

    private static EventVariable ParseVariableDeclaration(string declaration) {
        string[] tokens = declaration.Split(' ');
        if (tokens.Length < 3) return null;
        if (!tokens[0].StartsWith("@")) {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : must assign an event variable (\"@variable\").");
            return null;
        }
        string variableName = tokens[0].Substring(1);
        if (variableName.Length == 0) {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : empty event variable name.");
            return null;
        }

        string operation = tokens[1];
        if (operation != "=") {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : can only assign the variable value.");
            return null;
        }

        string rightValue = tokens[2];
        if (rightValue == $"@{variableName}") {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : illegal assignment.");
            return null;
        }

        ExpressionFloat variableExpression = ParseExpressionFloat(tokens.Skip(2));
        if (variableExpression == null) {
            Debug.LogError($"EventsController.ParseVariableDeclaration(\"{declaration}\") : right operand parsing error.");
            return null;
        }

        VariableFloat variable = (ec, d, c) => {
            float value = variableExpression.Variable(ec, d, c);
            ec.SetVariable(variableName, value);
            return value;
        };
        return new EventVariable(variableName, variable);
    }
}
