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

    private ExpressionFloat triggersLimit;

    [SerializeField] private List<ScriptCondition> conditions;
    [SerializeField] private List<ScriptAction> actions;

    [SerializeField] private string cachedDescriptionEnglish;
    private readonly List<ExpressionFloat> descriptionExpressions = new List<ExpressionFloat>();

    public WorldEvent(Event info,
        List<ScriptCondition> conditions,
        List<ScriptAction> actions) {
        this.info = info;
        this.conditions = conditions;
        this.actions = actions;

        // Trigger limit parsing
        triggersLimit = ParseExpressionFloat(info.TriggerLimit.Split(' '));
        if (triggersLimit == null) {
            Debug.LogError($"WorldEvent (Info.Id = {info.Id}) : trigger limit parsing error in \"{info.TriggerLimit}\".");
            triggersLimit = new ExpressionFloat(info.TriggerLimit, (ec, d, c) => -1); // -1 : no limit
        }

        // Description text parsing
        cachedDescriptionEnglish = CachedDescription(info.DescriptionEnglish);
    }

    /// <summary>
    /// Check the WorldEvent's trigger conditions with the current game state.
    /// If every one of them evaluates to True, trigger the actions.
    /// </summary>
    /// <returns>True if the WorldEvent cannot be triggered anymore, False otherwise.</returns>
    public bool CheckEvent(EventsController ec, DateTime d, GameDevCompany c) {
        // trigger limits check
        int limit = (int) triggersLimit.Variable(ec, d, c); // TODO : add and use VariableInt
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
        string desc = ComputedDescription(ec, d, c);
        Debug.Log($"=== Event description:\n{desc}\n===");

        return false;
    }

    private string CachedDescription(string description) {
        string cached = "";
        description = description.Trim();
        List<string> tokens = new List<string>();
        for (int i = 0; i < description.Length; i++) {
            char character = description[i];
            if (character != '{') {
                cached += character;
                continue;
            }
            // expression print : { expression... }
            bool ends = false;
            tokens.Clear();
            string currentToken = "";
            for (int j = i + 1; j < description.Length; j++) {
                char characterEnd = description[j];
                if (characterEnd == '}') {
                    tokens.Add(currentToken);
                    ends = true;
                    i = j;
                    break;
                }
                if (characterEnd == ' ') {
                    tokens.Add(currentToken);
                    currentToken = "";
                } else {
                    currentToken += characterEnd;
                }
            }
            if (!ends) {
                Debug.LogError($"WorldEvent (Info.Id = {info.Id}) : formatting error in the English Description text.");
                continue;
            }
            string expressionString = string.Join(" ", tokens);

            bool alreadyPresent = false;
            for (int j = 0; j < descriptionExpressions.Count; j++) {
                if (descriptionExpressions[j].Tokens == expressionString) {
                    cached += $"${j}$";
                    alreadyPresent = true;
                    break;
                }
            }
            if (alreadyPresent) continue;

            ExpressionFloat expression = ParseExpressionFloat(tokens.ToArray());
            if (expression == null) {
                Debug.LogError($"WorldEvent (Info.Id = {info.Id}) : expression parsing error in the English Description text.");
                continue;
            }
            Assert.IsTrue(expressionString == expression.Tokens);
            cached += $"${descriptionExpressions.Count}$";
            descriptionExpressions.Add(expression);
        }
        return cached;
    }

    private string ComputedDescription(EventsController ec, DateTime d, GameDevCompany c) {
        string output = "";
        for (int i = 0; i < cachedDescriptionEnglish.Length; i++) {
            char character = cachedDescriptionEnglish[i];
            if (character != '$') {
                output += character;
                continue;
            }

            // expression print : ${expression index}$
            string number = "";
            for (int j = i + 1; j < cachedDescriptionEnglish.Length; j++) {
                char characterEnd = cachedDescriptionEnglish[j];
                if (characterEnd == '$') {
                    i = j;
                    break;
                }
                number += characterEnd;
            }
            Assert.IsTrue(number.Length > 0);

            int expressionIndex;
            Assert.IsTrue(int.TryParse(number, out expressionIndex));
            Assert.IsTrue(0 <= expressionIndex && expressionIndex < descriptionExpressions.Count);

            output += descriptionExpressions[expressionIndex].Variable(ec, d, c);
        }
        return output;
    }
}