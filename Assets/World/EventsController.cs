using System;
using System.Collections.Generic;
using Script;
using UnityEngine;
using Event = Database.Event;

public class EventsController : MonoBehaviour {
    [SerializeField] private List<WorldEvent> worldEvents = new List<WorldEvent>();

    public void InitEvents(List<Event> events,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        foreach (Event e in events) {
            foreach (string declaration in e.VariablesDeclarations) {
                if (!Parser.ParseVariableDeclaration(declaration,
                    localVariables, globalVariables, functions)) {
                    Debug.LogError($"EventsController - Event variable declaration parsing error for Event (ID = {e.Id}).");
                }
            }

            // Parse the trigger conditions and actions
            List<Expression<bool>> conditions = new List<Expression<bool>>();
            List<IExpression> actions = new List<IExpression>();
            foreach (string condition in e.TriggerConditions) {
                Expression<bool> trigger = Parser.ParseComparison(condition,
                    localVariables, globalVariables, functions);
                if (trigger == null) {
                    Debug.LogError(
                        $"EventsController - Condition parsing error for Event (ID = {e.Id}). Ignoring \"{condition}\".");
                    continue;
                }
                if (trigger.Type() != SymbolType.Boolean) {
                    Debug.LogError(
                        $"EventsController - Condition expression {trigger.Type()} and not boolean for Event (ID = {e.Id}). Ignoring \"{condition}\".");
                    continue;
                }
                conditions.Add(trigger);
            }
            foreach (string action in e.TriggerActions) {
                IExpression trigger = Parser.ParseExpression(action,
                    localVariables, globalVariables, functions);
                if (trigger == null) {
                    Debug.LogError(
                        $"EventsController - Action parsing error for Event (ID = {e.Id}). Ignoring \"{action}\".");
                    continue;
                }
                actions.Add(trigger);
            }

            // Store the WorldEvent and its computed metadata
            WorldEvent worldEvent = new WorldEvent(e, conditions, actions,
                localVariables, globalVariables, functions);
            worldEvents.Add(worldEvent);
        }
    }
    public List<WorldEvent> OnGameDateChanged(IScriptContext context) {
        List<WorldEvent> triggeredEvents = new List<WorldEvent>();
        foreach (WorldEvent we in worldEvents) {
            if (!we.Active) continue;
            bool triggered;
            we.CheckEvent(context, out triggered);
            if (triggered) triggeredEvents.Add(we);
        }
        return triggeredEvents;
    }

    public void OnPlayerCompanyChanged(IScriptContext context) {
        foreach (WorldEvent we in worldEvents) {
            if (!we.Active) continue;
            bool triggered;
            we.CheckEvent(context, out triggered);
        }
    }
}
