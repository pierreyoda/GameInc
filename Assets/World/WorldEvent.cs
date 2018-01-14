using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using NUnit.Framework;
using Script;
using UnityEngine;
using Event = Database.Event;

[Serializable]
public class WorldEvent {
    [SerializeField] private Event info;
    public Event Info => info;

    [SerializeField] private int triggersCount = 0;
    private Expression<int> triggersLimit;

    [SerializeField] private bool active = true;
    public bool Active => active;

    [SerializeField] private List<Expression<bool>> conditions;
    [SerializeField] private List<IExpression> actions;

    [SerializeField] private string cachedTitle;
    private readonly List<IExpression> titleExpressions = new List<IExpression>();

    [SerializeField] private string cachedDescription;
    private readonly List<IExpression> descriptionExpressions = new List<IExpression>();

    private string computedTitle;
    public string ComputedTitle => computedTitle;

    private string computedDescription;
    public string ComputedDescription => computedDescription;

    public WorldEvent(Event info,
        List<Expression<bool>> conditions,
        List<IExpression> actions,
        List<LocalVariable> localVariables,
        List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        this.info = info;
        this.conditions = conditions;
        this.actions = actions;

        // Trigger limit parsing
        IExpression limit = Parser.ParseExpression(info.TriggerLimit,
            localVariables, globalVariables, functions);
        if (limit == null) {
            Debug.LogError(
                $"WorldEvent (Info.Id = {info.Id}) : trigger limit parsing error in \"{info.TriggerLimit}\". Reverting to no limit default (-1).");
            triggersLimit = new SymbolExpression<int>(new IntegerSymbol(-1)); // no limit by default
        } else if (limit.Type() != SymbolType.Integer) {
            Debug.LogError(
                $"WorldEvent (Info.Id = {info.Id}) : trigger limit of type {limit.Type()} instead of {SymbolType.Integer}. Reverting to no limit default (-1).");
            triggersLimit = new SymbolExpression<int>(new IntegerSymbol(-1)); // no limit by default
        } else {
            triggersLimit = limit as Expression<int>;
        }

        // Text parsing
        cachedTitle = CachedText(info.TitleEnglish, titleExpressions,
            localVariables, globalVariables, functions, "Title");
        cachedDescription = CachedText(info.DescriptionEnglish, descriptionExpressions,
            localVariables, globalVariables, functions, "Description");
    }

    /// <summary>
    /// Check the WorldEvent's trigger conditions with the current game state.
    /// If every one of them evaluates to True, trigger the actions.
    /// </summary>
    /// <returns>True if the WorldEvent cannot be triggered anymore, False otherwise.</returns>
    public bool CheckEvent(IScriptContext context, out bool triggered) {
        triggered = false;

        // trigger limits check
        ISymbol limitSymbol = triggersLimit.Evaluate(context);
        if (limitSymbol == null || limitSymbol.Type() != SymbolType.Integer) {
            Debug.Log($"WorldEvent - Event \"{info.Id}\" : invalid limit \"{triggersLimit.Script()}\".");
            active = false;
            return true;
        }
        int limit = ((Symbol<int>) limitSymbol).Value;
        if (limit >= 0 && triggersCount >= limit) {
            Debug.Log($"WorldEvent - Event \"{info.Id}\" reached its triggers limit ({limit}).");
            active = false;
            return true;
        }

        // condition check : all conditions must evaluate to True
        foreach (Expression<bool> condition in conditions) {
            ISymbol result = condition.Evaluate(context);
            if (result == null) {
                Debug.Log($"WorldEvent - Event \"{info.Id}\" : error while evaluating condition \"{condition.Script()}\".");
                return true;
            }
            if (result.Type() != SymbolType.Boolean) {
                Debug.Log($"WorldEvent - Event \"{info.Id}\" : non-boolean condition of type {result.Type()} \"{condition.Script()}\".");
                return true;
            }
            bool validated = ((Symbol<bool>) result).Value;
            if (!validated) return false;
        }

        // action when triggered
        Debug.Log($"WorldEvent - Event \"{info.Id}\" triggered ! Triggers count = {triggersCount}, limit = {limit}.");
        ++triggersCount;
        foreach (IExpression action in actions) {
        }

        computedTitle = ComputeTitle(context);
        computedDescription = ComputeDescription(context);

        triggered = true;
        return false;
    }

    private string CachedText(string text, List<IExpression> expressions,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions,
        string label) {
        string cached = "";
        text = text.Trim();
        for (int i = 0; i < text.Length; i++) {
            char character = text[i];
            if (character != '{') {
                cached += character;
                continue;
            }
            // expression print : { expression... }
            bool ends = false;
            string expressionString = "";
            for (int j = i + 1; j < text.Length; j++) {
                char c = text[j];
                if (c == '}') {
                    ends = true;
                    i = j;
                    break;
                }
                expressionString += c;
            }
            if (!ends) {
                Debug.LogError($"WorldEvent (Info.Id = {info.Id}) : formatting error in the English {label} text \"{text}\".");
                continue;
            }

            bool alreadyPresent = false;
            for (int j = 0; j < expressions.Count; j++) {
                if (expressions[j].Script() == expressionString) {
                    cached += $"${j}$";
                    alreadyPresent = true;
                    break;
                }
            }
            if (alreadyPresent) continue;

            IExpression expression = Parser.ParseExpression(expressionString,
                localVariables, globalVariables, functions);
            if (expression == null) {
                Debug.LogError($"WorldEvent (Info.Id = {info.Id}) : expression parsing error in the English {label} text.");
                continue;
            }
            cached += $"${expressions.Count}$";
            expressions.Add(expression);
        }
        return cached;
    }

    private string ComputeTitle(IScriptContext context) {
        return ComputeText(context, titleExpressions, cachedTitle);
    }

    private string ComputeDescription(IScriptContext context) {
        return ComputeText(context, descriptionExpressions, cachedDescription);
    }

    private static string ComputeText(IScriptContext context,
        List<IExpression> expressions, string cachedText) {
        List<string> expressionValues = new List<string>();
        for (int i = 0; i < expressions.Count; i++) {
            IExpression expression = expressions[i];
            ISymbol symbol = expression.EvaluateAsISymbol(context);
            if (symbol == null) {
                expressionValues.Add($"{{EVALUATION ERROR FOR : {expression.Script()}}}");
                continue;
            }
            expressionValues.Add(symbol.ValueString());
        }

        string output = "";
        for (int i = 0; i < cachedText.Length; i++) {
            char character = cachedText[i];
            if (character != '$') {
                output += character;
                continue;
            }

            // expression print : ${expression index}$
            string number = "";
            for (int j = i + 1; j < cachedText.Length; j++) {
                char characterEnd = cachedText[j];
                if (characterEnd == '$') {
                    i = j;
                    break;
                }
                number += characterEnd;
            }
            Assert.IsTrue(number.Length > 0);

            int expressionIndex;
            Assert.IsTrue(int.TryParse(number, out expressionIndex));
            Assert.IsTrue(0 <= expressionIndex && expressionIndex < expressions.Count);

            output += expressionValues[expressionIndex];
        }
        return output;
    }
}
