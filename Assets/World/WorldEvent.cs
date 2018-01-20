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

    [SerializeField] private int triggersCount = 0;
    private readonly TypedExecutable<int> triggersLimit;

    [SerializeField] private bool active = false;
    public bool Active => active;

    [SerializeField] private Executable onInit;
    [SerializeField] private TypedExecutable<bool> condition;
    [SerializeField] private Executable action;

    [SerializeField] private string cachedTitle;
    private readonly List<IExpression> titleExpressions = new List<IExpression>();

    [SerializeField] private string cachedDescription;
    private readonly List<IExpression> descriptionExpressions = new List<IExpression>();

    private string computedTitle;
    public string ComputedTitle => computedTitle;

    private string computedDescription;
    public string ComputedDescription => computedDescription;

    public WorldEvent(Event info, Executable onInit, TypedExecutable<bool> condition,
        Executable action, ParserContext parserContext) {
        this.info = info;
        this.onInit = onInit;
        this.condition = condition;
        this.action = action;

        // Trigger limit parsing
        triggersLimit = TypedExecutable<int>.FromScript(info.TriggerLimit,
            parserContext);
        if (triggersLimit == null) {
            Debug.LogError(
                $"WorldEvent (Info.Id = {info.Id}) : trigger limit parsing error in " +
                $"\"{info.TriggerLimit}\". Reverting to no limit default (-1).");
            triggersLimit = TypedExecutable<int>.FromScript("-1", parserContext); // no limit by default
            Assert.IsNotNull(triggersLimit);
        }

        // Text parsing
        cachedTitle = CachedText(info.TitleEnglish, titleExpressions, "Title",
            parserContext);
        cachedDescription = CachedText(info.DescriptionEnglish,
            descriptionExpressions, "Description", parserContext);
    }

    public bool InitEvent(IScriptContext context) {
        if (onInit.Execute(context) == null) {
            Debug.LogError($"WorldEvent(id = \"{info.Id}\").OnInit : evaluation " +
                      $"error in \"{info.OnInit}\". Disabling this WorldEvent.");
            active = false;
            return false;
        }
        active = true;
        return true;
    }

    /// <summary>
    /// Check the WorldEvent's trigger conditions with the current game state.
    /// If every one of them evaluates to True, trigger the actions.
    /// </summary>
    /// <returns>True if the WorldEvent cannot be triggered anymore, False otherwise.</returns>
    public bool CheckEvent(IScriptContext context, out bool triggered) {
        Assert.IsTrue(active);
        triggered = false;
        // trigger limits check
        int limit;
        if (!triggersLimit.Compute(context, out limit)) {
            Debug.Log($"WorldEvent - Event \"{info.Id}\" : evaluation error in limit \"{info.TriggerLimit}\".");
            active = false;
            return true;
        }
        if (limit >= 0 && triggersCount >= limit) {
            Debug.Log($"WorldEvent - Event \"{info.Id}\" reached its triggers limit ({limit}).");
            active = false;
            return true;
        }

        // condition check : all conditions must evaluate to True
        bool validated;
        if (!condition.Compute(context, out validated)) {
            Debug.Log($"WorldEvent - Event \"{info.Id}\" : error while " +
                      $"evaluating condition \"{info.TriggerCondition}\".");
        }
        if (!validated) return false;

        // action when triggered
        Debug.Log($"WorldEvent - Event \"{info.Id}\" triggered ! " +
                  $"Triggers count = {++triggersCount}, limit = {limit}.");
        if (action.Execute(context) == null) {
            Debug.LogError($"WorldEvent - Event \"{info.Id}\" activation : error " +
                           $"while evaluating Action script \"{info.TriggerAction}\".");
            computedTitle = computedDescription = "$ SCRIPT EXECUTION ERROR $";
            return true;
        }
        computedTitle = ComputeTitle(context);
        computedDescription = ComputeDescription(context);
        triggered = true;
        return false;
    }

    private string CachedText(string text, List<IExpression> expressions,
        string label, ParserContext parserContext) {
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

            IExpression expression = Parser.ParseExpression(expressionString, parserContext);
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
