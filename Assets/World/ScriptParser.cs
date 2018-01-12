using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.WindowsStandalone;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class ScriptParser {
    private static readonly char[] SpecialOperators = new char[] {
        '+', '-', '*', '/', ',',
    };

    public static readonly string[] FUNCTIONS_ARITY_1 = {
        "Math.Cos",
        "Math.Sin",
        "Math.Tan",
        "Math.Abs",
        "Random.Next",
        "Company.EnableFeature",
        "Company.DisableFeature",
    };

    public static readonly string[] FUNCTIONS_ARITY_2 = {
        "Random.Range",
    };

    private static readonly CultureInfo CultureInfoFloat =
        CultureInfo.GetCultureInfo("en-US");

    private static readonly NumberStyles NUMBER_STYLE_FLOAT =
        NumberStyles.Float | NumberStyles.AllowThousands;

    public class ExpressionFloat {
        public string Tokens;
        public VariableFloat Variable;

        public ExpressionFloat(string tokens, VariableFloat variable) {
            Tokens = tokens;
            Variable = variable;
        }
    }

    public delegate float VariableFloat(EventsController ec, DateTime d, GameDevCompany c);
    public delegate bool ScriptCondition(EventsController ec, DateTime d, GameDevCompany c);
    public delegate void ScriptAction(EventsController ec, DateTime d, GameDevCompany c);

    public static ScriptCondition ParseCondition(string condition) {
        string[] tokens = condition.Split(' ');
        if (tokens.Length < 3) return null;

        VariableFloat leftValue = ParseScalarFloat(tokens[0]);
        ExpressionFloat rightValue = ParseExpressionFloat(tokens.Skip(2));
        if (rightValue == null) {
            Debug.LogError($"ScriptParser.ParseCondition(\"{condition}\") : right operand parsing error.");
            return null;
        }

        switch (tokens[1]) {
            case "<": return (ec, d, c) => leftValue(ec, d, c) < rightValue.Variable(ec, d, c);
            case "<=": return (ec, d, c) => leftValue(ec, d, c) <= rightValue.Variable(ec, d, c);
            case ">": return (ec, d, c) => leftValue(ec, d, c) > rightValue.Variable(ec, d, c);
            case ">=": return (ec, d, c) => leftValue(ec, d, c) >= rightValue.Variable(ec, d, c);
            case "==": return (ec, d, c) => Math.Abs(leftValue(ec, d, c) - rightValue.Variable(ec, d, c)) < 0.00001;
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
        ExpressionFloat rightValue = ParseExpressionFloat(tokens.Skip(2));
        if (rightValue == null) {
            Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : right operand parsing error.");
            return null;
        }

        // Game variable
        if (leftName.StartsWith("$Game.Scores")) { // Current Game Project Scores
            string scoreId = leftName.Split('.')[2];
            switch (operation) {
                case "+=": return (ec, d, c) => c.CurrentGame().ModifyScore(scoreId, rightValue.Variable(ec, d, c));
                case "-=": return (ec, d, c) => c.CurrentGame().ModifyScore(scoreId, -1 * rightValue.Variable(ec, d, c));
                default:
                    Debug.LogError(
                        $"ScriptParser.ParseAction(\"{action}\") : unsupported operation for \"$Game.Scores.{scoreId}\".");
                    return null;
            }
        }
        if (leftName.StartsWith("$")) {
            switch (variableName) {
                case "Company.Money":
                    switch (operation) {
                        case "+=": return (ec, d, c) => c.Pay(rightValue.Variable(ec, d, c));
                        case "-=": return (ec, d, c) => c.Charge(rightValue.Variable(ec, d, c));
                        case "=": return (ec, d, c) => c.SetMoney(rightValue.Variable(ec, d, c));
                        default:
                            Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : unsupported operator for \"$Company.Money\".");
                            return null;
                    }
                case "Company.NeverBailedOut":
                    if (operation != "=") {
                        Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : unsupported operation.");
                        return null;
                    }
                    return (ec, d, c) => c.NeverBailedOut = Math.Abs(rightValue.Variable(ec, d, c) - 1f) < 0.000001;
                default:
                    Debug.LogError($"ScriptParser.ParseAction(\"{action}\") : unsupported variable.");
                    return null;
            }
        }

        // Event variable
        // TODO - get rid of GetVariable(.), using a new EventVariableFloat delegate ?
        if (leftName.StartsWith("@")) {
            switch (operation) {
                case "=": return (ec, d, c) => ec.SetVariable(variableName, rightValue.Variable(ec, d, c));
                case "+=": return (ec, d, c) => ec.SetVariable(variableName, ec.GetVariable(variableName) + rightValue.Variable(ec, d, c));
                case "-=": return (ec, d, c) => ec.SetVariable(variableName, ec.GetVariable(variableName) - rightValue.Variable(ec, d, c));
            }
        }
        return null;
    }

    public static ExpressionFloat ParseExpressionFloat(IEnumerable<string> expressionTokens) {
        Assert.IsTrue(expressionTokens.Count() > 0);

        DateTime temp = DateTime.Now;
        bool inFunctionCall = false;
        int openingParenthesesCountCall = 0, closingParenthesesCountCall = 0;
        int openingParenthesesCount = 0, closingParenthesesCount = 0;
        // each "$i" where i is an uint will be replaced by the associated variable value
        // we can do this since $ has special meaning otherwise
        string expression = "";
        string currentToken = "", functionCall = "";
        List<VariableFloat> variables = new List<VariableFloat>();
        string expressionString = string.Join(" ", expressionTokens);
        for (int i = 0; i < expressionString.Length; i++) {
            char c = expressionString[i];
            if (c == '(') {
                if (!inFunctionCall && i > 0) {
                    string previous = "";
                    for (int j = i - 1; j >= 0; j--) {
                        char character = expressionString[j];
                        if (character == ' ') break;
                        previous = previous.Insert(0, $"{character}");
                    }
                    inFunctionCall = previous.Length > 0;
                    if (inFunctionCall) {
                        openingParenthesesCountCall = closingParenthesesCountCall = 0;
                        functionCall = previous;
                    }
                }
                if (inFunctionCall) {
                    functionCall += c;
                    ++openingParenthesesCountCall;
                }
                ++openingParenthesesCount;
                if (!inFunctionCall) expression += c;
                currentToken = "";
                continue;
            }
            if (c == ')') {
                ++closingParenthesesCount;
                bool functionCallEnds = false;
                if (inFunctionCall &&
                    ++closingParenthesesCountCall == openingParenthesesCountCall) {
                    functionCall += c;
                    functionCallEnds = true;
                }
                if (functionCallEnds) {
                    inFunctionCall = false;
                    bool isFunctionCall;
                    VariableFloat call = ParseFunctionCall(functionCall, out isFunctionCall);
                    if (call == null || !isFunctionCall) {
                        Debug.LogError(
                            $"ScriptParser.ParseExpressionFloat(\"{expressionString}\") : parsing error in function call \"{functionCall}\".");
                        return null;
                    }
                    expression += $"${variables.Count} ";
                    variables.Add(call);
                    functionCall = currentToken = "";
                }
                continue;
            }
            bool ends = i == expressionString.Length - 1;
            if (SpecialOperators.Contains(c)) {
                if (!inFunctionCall) expression += $"{c} ";
                currentToken = "";
                if (inFunctionCall) functionCall += c;
                continue;
            }
            if (!inFunctionCall && (c == ' ' || ends)) { // current token = literal or variable scalar
                currentToken += c;
                currentToken = currentToken.Trim();
                if (currentToken != "") {
                    bool isVariable;
                    VariableFloat scalar = ParseScalarFloat(currentToken, out isVariable);
                    if (scalar == null) {
                        Debug.LogError(
                            $"ScriptParser.ParseExpressionFloat(\"{expressionString}\") : parsing error in token \"{currentToken}\".");
                        return null;
                    }
                    if (isVariable) {
                        expression += $"${variables.Count} ";
                        variables.Add(scalar);
                    } else {
                        expression += scalar(null, temp, null) + (c == ' ' ? " " : ""); // literal or constant scalar
                    }
                }
                currentToken = "";
            }
            currentToken += c;
            if (inFunctionCall) functionCall += c;
        }
        if (openingParenthesesCount != closingParenthesesCount) {
            Debug.LogError($"ScriptParser.ParseExpressionFloat(\"{expressionString}\") : invalid parentheses.");
            return null;
        }

        VariableFloat variable = (ec, d, c) => {
            string computedExpression = "";
            for (int i = 0; i < expression.Length; i++) {
                char character = expression[i];
                if (character != '$') {
                    computedExpression += character;
                    continue;
                }

                int variableIndexEnd = i;
                for (int j = i + 1; j < expression.Length; j++) {
                    char characterEnd = expression[j];
                    if (characterEnd == ' ' || !char.IsNumber(characterEnd)) {
                        variableIndexEnd = j;
                        break;
                    }
                }
                Assert.IsTrue(i < variableIndexEnd && variableIndexEnd < expression.Length);

                int variableIndex;
                Assert.IsTrue(int.TryParse(expression.Substring(i + 1, variableIndexEnd - i),
                    out variableIndex));
                Assert.IsTrue(0 <= variableIndex && variableIndex < variables.Count);

                computedExpression += variables[variableIndex](ec, d, c) + " ";
                i = variableIndexEnd;
            }
            float result = ExpressionEvaluator.Evaluate<float>(computedExpression);
            //Debug.LogWarning($"expression : {expressionString} => {expression} => {computedExpression} = {result}");
            return result;
        };
        return new ExpressionFloat(expressionString, variable); // NB : ERRORS WILL SILENTLY RETURN 0
    }


    public static VariableFloat ParseScalarFloat(string scalar) {
        bool temp;
        return ParseScalarFloat(scalar, out temp);
    }

    public static VariableFloat ParseScalarFloat(string scalar, out bool isVariable) {
        // Function call
        bool isFunctionCall;
        VariableFloat functionCall = ParseFunctionCall(scalar, out isFunctionCall);
        if (isFunctionCall) {
            isVariable = true;
            if (functionCall != null) return functionCall;
            Debug.LogError($"ScriptParser.ParseScalarFloat(\"{scalar}\") : function call parsing error.");
            return null;
        }

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
        // Universal constant
        if (scalar == "Math.PI") return (ec, d, c) => Mathf.PI;
        // Constant
        float value;
        if (!float.TryParse(scalar, NUMBER_STYLE_FLOAT, CultureInfoFloat, out value)) {
            Debug.LogError($"ScriptParser.ParseScalarFloat(\"{scalar}\") : cannot parse as float.");
            return null;
        }
        return (ec, d, c) => value;
    }

    public static VariableFloat ParseFunctionCall(string call, out bool isFunctionCall) {
        int arity = 0;
        isFunctionCall = false;
        string functionName = "";
        foreach (string function in FUNCTIONS_ARITY_1) {
            if (call.StartsWith(function)) {
                arity = 1;
                isFunctionCall = true;
                functionName = function;
                break;
            }
        }
        foreach (string function in FUNCTIONS_ARITY_2) {
            if (call.StartsWith(function)) {
                arity = 2;
                isFunctionCall = true;
                functionName = function;
                break;
            }
        }
        if (functionName == "") return null;

        List<ExpressionFloat> parameterExpressions = new List<ExpressionFloat>();
        string[] parameters = GetInnerParameters(call);
        if (parameters == null) {
            Debug.LogError($"ScriptParser.ParseScalarFloat(\"{call}\") : function call syntax error.");
            return null;
        }
        if (parameters.Length != arity) {
            Debug.LogError($"ScriptParser.ParseScalarFloat(\"{call}\") : wrong arity for {call} ({parameters.Length} instead of {arity}).");
            return null;
        }
        foreach (string parameter in parameters) {
            ExpressionFloat expression = ParseExpressionFloat(parameter.Trim().Split(' '));
            if (expression == null) {
                Debug.LogError($"ScriptParser.ParseFunctionCall(\"{call}\") : parameter parsing error for \"{call}\".");
                return null;
            }
            parameterExpressions.Add(expression);
        }

        switch (functionName) {
            // Arity 1
            case "Math.Cos": return (ec, d, c) => Mathf.Cos(parameterExpressions[0].Variable(ec, d, c));
            case "Math.Sin": return (ec, d, c) => Mathf.Sin(parameterExpressions[0].Variable(ec, d, c));
            case "Math.Tan": return (ec, d, c) => Mathf.Tan(parameterExpressions[0].Variable(ec, d, c));
            case "Math.Abs": return (ec, d, c) => Mathf.Abs(parameterExpressions[0].Variable(ec, d, c));
            case "Random.Next": return (ec, d, c) => Random.value;
            // Arity 2
            case "Random.Range": return (ec, d, c) => Random.Range(
                    parameterExpressions[0].Variable(ec, d, c),
                    parameterExpressions[1].Variable(ec, d, c));
            // Unkown Function
            default:
                Debug.LogError($"ScriptParser.ParseFunctionCall(\"{call}\") : unkown function name {functionName}.");
                return null;
        }
    }

    public static string[] GetInnerParameters(string functionCall,
        string start = "(", string end = ")") {
        List<string> parameters = new List<string>();

        int startIndex = functionCall.IndexOf(start, StringComparison.Ordinal);
        int endIndex = functionCall.IndexOf(end, StringComparison.Ordinal);
        if (startIndex == -1 || endIndex == -1 || startIndex > endIndex) {
                Debug.LogError($"ScriptParser.GetInnerParameters(\"{functionCall}\") : syntax error.");
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
