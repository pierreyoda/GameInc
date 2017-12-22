using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class ScriptParser {
    private static readonly char[] SPECIAL_OPERATORS = new char[] {
        '+', '-', '*', '/', '(', ')'
    };

    private static readonly CultureInfo CULTURE_INFO_FLOAT =
        CultureInfo.CreateSpecificCulture("en-US");

    private static readonly NumberStyles NUMBER_STYLE_FLOAT =
        NumberStyles.Float | NumberStyles.AllowThousands;

    public delegate float VariableFloat(EventsController ec, DateTime d, GameDevCompany c);
    public delegate bool ScriptCondition(EventsController ec, DateTime d, GameDevCompany c);
    public delegate void ScriptAction(EventsController ec, DateTime d, GameDevCompany c);

    public static ScriptCondition ParseCondition(string condition) {
        string[] tokens = condition.Split(' ');
        if (tokens.Length < 3) return null;

        VariableFloat leftValue = ParseScalarFloat(tokens[0]);
        VariableFloat rightValue = ParseExpressionFloat(tokens.Skip(2));
        if (rightValue == null) {
            Debug.LogError($"ScriptParser.ParseCondition(\"{condition}\") : right operand parsing error.");
            return null;
        }

        switch (tokens[1]) {
            case "<": return (ec, d, c) => leftValue(ec, d, c) < rightValue(ec, d, c);
            case "<=": return (ec, d, c) => leftValue(ec, d, c) <= rightValue(ec, d, c);
            case ">": return (ec, d, c) => leftValue(ec, d, c) > rightValue(ec, d, c);
            case ">=": return (ec, d, c) => leftValue(ec, d, c) >= rightValue(ec, d, c);
            case "==": return (ec, d, c) => Math.Abs(leftValue(ec, d, c) - rightValue(ec, d, c)) < 0.00001;
        }
        return null;
    }

    public static ScriptAction ParseAction(string action) {
        string[] tokens = action.Split(' ');
        if (tokens.Length == 0) return null;
        string leftName = tokens[0].Trim();

        if (leftName.StartsWith("Company.EnableFeature") ||
            leftName.StartsWith("Company.DisableFeature")) {
            string[] parameters = GetInnerParameters(action);
            if (parameters.Length != 1) {
                Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : wrong Company Feature method arity.");
                return null;
            }

            string featureName = parameters[0];
            bool enable = leftName.StartsWith("Company.EnableFeature");
            return (ec, d, c) => c.SetFeature(featureName, enable);
        }

        if (tokens.Length < 3) return null;
        string variableName = leftName.Substring(1);
        string operation = tokens[1].Trim();
        VariableFloat rightValue = ParseExpressionFloat(tokens.Skip(2));
        if (rightValue == null) {
            Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : right operand parsing error.");
            return null;
        }

        // Game variable
        if (leftName.StartsWith("$Game.")) {
            string scoreName = leftName.Split('.')[1];
            if (!GameProject.GAME_SCORES.Contains(scoreName)) {
                Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : unkown Game Score name.");
                return null;
            }
            switch (operation) {
                case "+=": return (ec, d, c) => c.CurrentGame().ModifyScore(scoreName, rightValue(ec, d, c));
                case "-=": return (ec, d, c) => c.CurrentGame().ModifyScore(scoreName, -1 * rightValue(ec, d, c));
                default:
                    Debug.LogError(
                        $"ScriptParser.ParseAction(\"{action}\") : unsupported operation for \"$Game.{scoreName}\".");
                    return null;
            }
        }
        if (leftName.StartsWith("$")) {
            switch (variableName) {
                case "Company.Money":
                    switch (operation) {
                        case "+=": return (ec, d, c) => c.Pay(rightValue(ec, d, c));
                        case "-=": return (ec, d, c) => c.Charge(rightValue(ec, d, c));
                        case "=": return (ec, d, c) => c.SetMoney(rightValue(ec, d, c));
                        default:
                            Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : unsupported operator for \"$Company.Money\".");
                            return null;
                    }
                case "Company.NeverBailedOut":
                    if (operation != "=") {
                        Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : unsupported operation.");
                        return null;
                    }
                    return (ec, d, c) => c.NeverBailedOut = Math.Abs(rightValue(ec, d, c) - 1f) < 0.000001;
                default:
                    Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : unsupported variable.");
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

    public static VariableFloat ParseExpressionFloat(IEnumerable<string> expressionTokens) {
        Assert.IsTrue(expressionTokens.Count() > 0);

        int count = 0;
        DateTime temp = DateTime.Now;
        // each "$i" where i is an uint will be replaced by the associated variable value
        // we can do this since $ has special meaning otherwise
        string expression = "";
        List<VariableFloat> variables = new List<VariableFloat>();
        foreach (string token in expressionTokens) {
            if (token.Length == 0) continue;

            if (token.Length == 1 && SPECIAL_OPERATORS.Contains(token[0])) {
                expression += $"{token[0]} ";
                continue;
            }

            string trueToken = token;
            if (token[0] == '(' || token[0] == ')') {
                expression += $"{token[0]} ";
                trueToken = trueToken.Substring(1);
            }

            bool lastCharacterIsParenthesis = false;
            if (trueToken.Length > 1 && (trueToken.Last() == '(' || trueToken.Last() == ')')) {
                trueToken = trueToken.Substring(0, trueToken.Length - 1);
                lastCharacterIsParenthesis = true;
            }

            bool isVariable;
            VariableFloat scalar = ParseScalarFloat(trueToken, out isVariable);
            if (scalar == null) {
                Debug.LogError($"ScriptParser.ParseExpressionFloat : parsing error on token \"{trueToken}\".");
                return null;
            }

            if (isVariable) {
                variables.Add(scalar);
                expression += $"${count++} ";
            } else {
                expression += scalar(null, temp, null) + " ";
            }

            if (lastCharacterIsParenthesis) {
                expression += $" {token.Last()} ";
            }
        }

        return (ec, d, c) => {
            string computedExpression = "";
            for (int i = 0; i < expression.Length; i++) {
                char character = expression[i];
                if (character != '$') {
                    computedExpression += character;
                    continue;
                }

                int variableIndexEnd = i;
                for (int j = i + 1; j < expression.Length; j++) {
                    if (expression[j] == ' ') {
                        variableIndexEnd = j;
                        break;
                    }
                }
                Assert.IsTrue(variableIndexEnd > i);

                int variableIndex;
                Assert.IsTrue(int.TryParse(expression.Substring(i + 1, variableIndexEnd - i),
                    out variableIndex));
                Assert.IsTrue(0 <= variableIndex && variableIndex < variables.Count());

                computedExpression += variables[variableIndex](ec, d, c) + " ";
                i = variableIndexEnd;
            }
            return ExpressionEvaluator.Evaluate<float>(computedExpression);
        };
    }


    public static VariableFloat ParseScalarFloat(string scalar) {
        bool temp;
        return ParseScalarFloat(scalar, out temp);
    }

    public static VariableFloat ParseScalarFloat(string scalar, out bool isVariable) {
        // Game variables
        if (scalar.StartsWith("$")) {
            isVariable = true;
            string variableName = scalar.Substring(1);
            return (ec, d, c) => ec.GetGameVariable(variableName, d, c);
        }
        // Event variables
        if (scalar.StartsWith("@")) {
            isVariable = true;
            return (ec, d, c) => ec.GetVariable(scalar.Substring(1));
        }

        isVariable = false;
        // Boolean - TODO : improve handling (VariableBoolean ?)
        if (scalar == "true") return (ec, d, c) => 1f;
        if (scalar == "false") return (ec, d, c) => 0f;
        // Constant
        float value;
        if (!float.TryParse(scalar, NUMBER_STYLE_FLOAT, CULTURE_INFO_FLOAT, out value)) {
            Debug.LogError($"ScriptParser.ParseScalarFloat(\"{scalar}\") : cannot parse as float.");
            return null;
        }
        return (ec, d, c) => value;
    }

    public static string[] GetInnerParameters(string functionCall,
        string start = "(", string end = ")") {
        List<string> parameters = new List<string>();

        int startIndex = functionCall.IndexOf(start, StringComparison.Ordinal);
        int endIndex = functionCall.IndexOf(end, StringComparison.Ordinal);
        if (startIndex == -1 || endIndex == -1 ||
            startIndex > endIndex) {
            Debug.LogError($"ScriptParser.GetFunctionCallParameters(\"{functionCall}\") : syntax error.");
            return null;
        }

        string call = functionCall.Substring(startIndex + 1,
            endIndex - startIndex - 1);
        foreach (string parameter in call.Split(',')) {
            parameters.Add(parameter.Trim());
        }

        return parameters.ToArray();
    }


}