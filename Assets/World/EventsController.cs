using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Event = Database.Event;

public class EventsController : MonoBehaviour {
    private delegate float VariableFloat(DateTime date, GameDevCompany company);
    private delegate bool TriggerCondition(DateTime date, GameDevCompany company);
    private delegate void TriggerAction(DateTime date, GameDevCompany company);

    private readonly List<Tuple<Event, List<TriggerCondition>, List<TriggerAction>>> eventsTriggers =
        new List<Tuple<Event, List<TriggerCondition>, List<TriggerAction>>>();
    [SerializeField] private List<Event> eventsObservingGameDate = new List<Event>();
    [SerializeField] private List<Event> eventsObservingPlayerCompany = new List<Event>();

    public void InitEvents(List<Event> events) {
        foreach (Event e in events) {
            // Parse the trigger conditions and actions
            List<TriggerCondition> conditions = new List<TriggerCondition>();
            List<TriggerAction> actions = new List<TriggerAction>();
            foreach (string condition in e.TriggerConditions) {
                TriggerCondition trigger = ParseTriggerCondition(condition);
                if (trigger == null) {
                    Debug.LogError($"EventsController - Parsing error for Event (ID = {e.Id}).");
                    break;
                }
                conditions.Add(trigger);
            }
            foreach (string action in e.TriggerActions) {
                TriggerAction trigger = ParseTriggerAction(action);
                if (trigger == null) {
                    Debug.LogError($"EventsController - Parsing error for Event (ID = {e.Id}).");
                    break;
                }
                actions.Add(trigger);
            }
            eventsTriggers.Add(new Tuple<Event, List<TriggerCondition>, List<TriggerAction>>(e, conditions, actions));

            // Sort by observed game object
            foreach (string gameObject in e.ObservedObjects) {
                if (gameObject == "World.CurrentDate")
                    eventsObservingGameDate.Add(e);
                if (gameObject == "Company")
                    eventsObservingPlayerCompany.Add(e);
            }
        }
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

    private void CheckEvent(string eventId, DateTime g, GameDevCompany c) {
        var eventTriggers = eventsTriggers.Find(ea => ea.Item1.Id == eventId);
        // condition check
        bool triggered = true;
        foreach (TriggerCondition condition in eventTriggers.Item2) {
            if (!condition(g, c)) {
                triggered = false;
                break;
            }
        }
        // action when triggered
        if (!triggered) return;
        Debug.Log($"EventsController - Event \"{eventId}\" triggered !");
        foreach (TriggerAction action in eventTriggers.Item3) {
            action(g, c);
        }
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
        if (!tokens[0].StartsWith("$")) return null;

        string operation = tokens[1].Trim();
        VariableFloat rightValue = ParseOperandFloat(tokens[2].Trim());
        switch (tokens[0].Trim().Substring(1)) {
            case "Company.Money":
                return (d, c) => {
                    switch (operation) {
                        case "+=": c.Pay(rightValue(d, c)); break;
                        case "-=": c.Charge(rightValue(d, c)); break;
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

    private VariableFloat ParseOperandFloat(string operand) {
        // Variable
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
        // Boolean - TODO : improve handling
        if (operand == "true") return (d, c) => 1f;
        if (operand == "false") return (d, c) => 0f;
        // Constant
        float value = float.Parse(operand, CultureInfo.InvariantCulture.NumberFormat);
        return (d, c) => value;
    }
}