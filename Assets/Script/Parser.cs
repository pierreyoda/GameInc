using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace Script {

[Serializable]
public class LocalVariable {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private SymbolType type;
    public SymbolType Type => type;

    [SerializeField] private ISymbol value;
    public ISymbol Value {
        get { return value; }
        set { this.value = value; }
    }

    [SerializeField] private IExpression declaration;
    public IExpression Declaration => declaration;

    public LocalVariable(string name, SymbolType type, IExpression declaration) {
        this.name = name;
        this.type = type;
        this.declaration = declaration;
    }
}

public delegate ISymbol GlobalVariableFromContext(IScriptContext context);

[Serializable]
public class GlobalVariable {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private SymbolType type;
    public SymbolType Type => type;

    [SerializeField] private GlobalVariableFromContext fromContext;
    public GlobalVariableFromContext FromContext => fromContext;

    [SerializeField] private ISymbol value;
    public ISymbol Value {
        get { return value; }
        set { this.value = value; }
    }

    public GlobalVariable(string name, SymbolType type,
        GlobalVariableFromContext fromContext) {
        this.name = name;
        this.type = type;
        this.fromContext = fromContext;
    }
}

[Serializable]
public class Parser : MonoBehaviour {
    private static readonly CultureInfo DateCultureInfo = CultureInfo.InvariantCulture;
    public const NumberStyles NumberStyleInteger = NumberStyles.Integer;
    public const NumberStyles NumberStyleFloat =
        NumberStyles.Float | NumberStyles.AllowThousands;

    private static IExpression ParseAssignment(string assignment,
        ParserContext context) {
        // Tokenization
        string a = assignment.Trim();
        List<string> tokens = Lexer.Tokenize(a);
        if (tokens == null) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : tokenization error.");
            return null;
        }
        if (tokens.Count < 3) {
            Debug.LogError($"Parse.ParseAssignment(\"{a}\") : illegal declaration \"{a}\"");
            return null;
        }

        // Sanity checks
        string variable = tokens[0];
        bool global = variable.StartsWith("$");
        string variableName = variable.Substring(1);
        if (variableName.Length < 1) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : illegal empty variable name.");
            return null;
        }
        bool declaration = !global && tokens[1] == ":";
        int decl = declaration ? 2 : 0;
        string operation = tokens[1 + decl];
        AssignmentType assignmentType;
        if (!Operations.AssignmentTypeFromString(operation, out assignmentType)) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unsupported assignment \"{operation}\".");
            return null;
        }
        if (assignmentType == AssignmentType.Assign && !global && !declaration) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : variable declaration for \"${variableName}\" must define a type.");
            return null;
        }

        // Type
        SymbolType type = SymbolType.Invalid;
        LocalVariable localVariable = null;
        GlobalVariable globalVariable = null;
        if (declaration) {
            if (!variable.StartsWith("@") || tokens[1] != ":") {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : illegal declaration \"{a}\"");
                return null;
            }
            string typeString = tokens[2];
            if (!Symbol<bool>.SymbolTypeFromString(typeString, out type)) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unsupported Type \"{typeString}\"");
                return null;
            }
        } else if (variable.StartsWith("@")) { // local variable
            localVariable = context.LocalVariables.Find(lv => lv.Name == variableName);
            if (localVariable == null) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unkown local Variable \"{variableName}\"");
                return null;
            }
            type = localVariable.Type;
        }
        if (variable.StartsWith("$")) { // global variable
            Assert.IsTrue(localVariable == null && !declaration);
            globalVariable = context.GlobalVariables.Find(gv => gv.Name == variableName);
            if (globalVariable == null) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unkown Game Variable \"{variableName}\"");
                return null;
            }
            type = globalVariable.Type;
        }
        Assert.IsFalse(type == SymbolType.Invalid);

        // Right expression
        //Debug.LogWarning($"==> parse assignment \"{a}\" => right expr tokens = {string.Join(" --- ", tokens.Skip(2+decl))}");
        IExpression rightExpression = ParseExpression(tokens.Skip(2 + decl).ToArray(),
            context);
        if (rightExpression == null) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : parsing error in declaration.");
            return null;
        }
        if (rightExpression.Type() != type) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : right expression {rightExpression.Type()} instead of {type} (\"{rightExpression.Script()}\").");
            return null;
        }
        if (declaration) {
            Assert.IsTrue(localVariable == null && globalVariable == null);
            localVariable = new LocalVariable(variableName, type, rightExpression) {
                Value = GetDefaultValue(type)
            };
            context.LocalVariables.Add(localVariable);
        }
        if (global) Assert.IsNotNull(globalVariable);
        else Assert.IsNotNull(localVariable);

        // Return type : if ends with ';' void, else returns the computed value
        SymbolType returnType;
        if (tokens.Last() == ";") returnType = SymbolType.Void;
        else returnType = type;
        bool returnsType = returnType == type;

        //Debug.LogWarning($"==> parsed {type} assignment \"{a}\" : returns type = {returnsType} : " +
        //                 $"{variable} {assignmentType} {rightExpression.Script()}");
        IVariableExpression variableExpression;
        IExpression assignmentExpression;
        switch (type) {
            case SymbolType.Boolean:
                if (global)
                    variableExpression = new GlobalVariableExpression<bool>(globalVariable);
                else
                    variableExpression = new LocalVariableExpression<bool>(localVariable);
                if (returnsType)
                assignmentExpression = new AssignmentExpression<bool, bool>(assignmentType,
                    variableExpression, rightExpression as Expression<bool>, returnsType);
                else
                assignmentExpression = new AssignmentExpression<Void, bool>(assignmentType,
                    variableExpression, rightExpression as Expression<bool>, returnsType);
                break;
            case SymbolType.Integer:
                if (global)
                    variableExpression = new GlobalVariableExpression<int>(globalVariable);
                else
                    variableExpression = new LocalVariableExpression<int>(localVariable);
                if (returnsType)
                    assignmentExpression = new AssignmentExpression<int, int>(assignmentType,
                        variableExpression, rightExpression as Expression<int>, returnsType);
                else
                    assignmentExpression = new AssignmentExpression<Void, int>(assignmentType,
                        variableExpression, rightExpression as Expression<int>, returnsType);
                break;
            case SymbolType.Float:
                if (global)
                    variableExpression = new GlobalVariableExpression<float>(globalVariable);
                else
                    variableExpression = new LocalVariableExpression<float>(localVariable);
                if (returnsType)
                    assignmentExpression = new AssignmentExpression<float, float>(assignmentType,
                        variableExpression, rightExpression as Expression<float>, returnsType);
                else
                    assignmentExpression = new AssignmentExpression<Void, float>(assignmentType,
                        variableExpression, rightExpression as Expression<float>, returnsType);
                break;
            case SymbolType.Id:
            case SymbolType.String:
                if (global)
                    variableExpression = new GlobalVariableExpression<string>(globalVariable);
                else
                    variableExpression = new LocalVariableExpression<string>(localVariable);
                if (returnsType)
                    assignmentExpression = new AssignmentExpression<string, string>(assignmentType,
                        variableExpression, rightExpression as Expression<string>, returnsType);
                else
                    assignmentExpression = new AssignmentExpression<Void, string>(assignmentType,
                        variableExpression, rightExpression as Expression<string>, returnsType);
                break;
            case SymbolType.Date:
                if (global)
                    variableExpression = new GlobalVariableExpression<DateTime>(globalVariable);
                else
                    variableExpression = new LocalVariableExpression<DateTime>(localVariable);
                if (returnsType)
                    assignmentExpression = new AssignmentExpression<DateTime, DateTime>(assignmentType,
                        variableExpression, rightExpression as Expression<DateTime>, returnsType);
                else
                    assignmentExpression = new AssignmentExpression<Void, DateTime>(assignmentType,
                        variableExpression, rightExpression as Expression<DateTime>, returnsType);
                break;
            default:
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : invalid assignment.");
                return null;
        }
        return assignmentExpression;
    }

    public static IExpression ParseExpression(string expression,
        ParserContext context) {
        bool isAssignment, isComparison;
        List<string> tokens = Lexer.Tokenize(expression, out isAssignment);
        if (tokens == null) {
            Debug.LogError($"Parser.ParseExpression(\"{expression}\") : tokenization error.");
            return null;
        }
        // assignment => special handling to simplify the algorithm
        if (isAssignment)
            return ParseAssignment(expression, context);
        return ParseExpression(tokens.ToArray(), context);
    }

    private static IExpression ParseExpression(string[] tokens,
        ParserContext context) {
        string expressionString = string.Join(" ", tokens);
        // use the Shunting-Yard Algorithm to transform the script into
        // a Polish Notation (postfix) list of expressions and operators
        Stack<IExpression> output = new Stack<IExpression>();
        Stack<OperatorType> operators = new Stack<OperatorType>();
        for (int i = 0; i < tokens.Length; i++) {
            string token = tokens[i];
            if (token == "") continue;
            // token is ";" : expression ends
            if (token == ";") {
                if (i + 1 < tokens.Length)
                    Debug.LogWarning(
                        $"Parser.ParseExpression(\"{expressionString}\") :';' before the expression ends, ignoring the " +
                        $"remaining tokens \"{string.Join(" ", tokens.Skip(i + 1))}\".");
                break;
            }
            // is token is an operator ?
            OperatorType operatorType;
            if (Operations.OperatorTypeFromString(token, out operatorType)) {
                if (token == "(") {
                    operators.Push(operatorType);
                    continue;
                }
                if (token == ")") {
                    OperatorType operation = operators.Peek();
                    while (operation != OperatorType.OpeningParenthesis) {
                        if (ProcessOperation(output, operators) == OperatorType.IllegalOperation) {
                            Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                           "invalid operation.");
                            return null;
                        }
                        if (operators.Count == 0) break;
                        operation = operators.Peek();
                    }
                    if (operators.Count == 0 ||
                        operators.Peek() != OperatorType.OpeningParenthesis) {
                        Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                       "parentheses error (missing '(').");
                        return null;
                    }
                    operators.Pop();
                    continue;
                }
                int priority = Operations.OperatorPriority(operatorType);
                bool rightAssociative = operatorType == OperatorType.Power;
                while (operators.Count > 0) {
                    OperatorType otherOperation = operators.Peek();
                    if (otherOperation == OperatorType.OpeningParenthesis) break;
                    int otherPriority = Operations.OperatorPriority(otherOperation);
                    if (otherPriority == priority && rightAssociative ||
                        otherPriority < priority) break;
                    if (ProcessOperation(output, operators) == OperatorType.IllegalOperation) {
                        Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : invalid operation.");
                        return null;
                    }
                }
                operators.Push(operatorType);
                continue;
            }
            // is token the start of a function call ?
            IFunction function = context.Functions.Find(f => f.Name() == token);
            if (function != null) {
                string name = token;
                if (i + 2 >= tokens.Length || tokens[i + 1] != "(") {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : invalid function call for \"{token}\".");
                    return null;
                }
                bool ends = false;
                string functionCall = $"{name}(";
                int openingParentheses = 1, closingParentheses = 0;
                for (int j = i + 2; j < tokens.Length; j++) {
                    string t = tokens[j];
                    functionCall += $"{t}";
                    if (t == "(") ++openingParentheses;
                    if (t == ")" && ++closingParentheses == openingParentheses) {
                        i = j;
                        ends = true;
                        break;
                    }
                }
                if (!ends) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : missing closing parenthesis for function call \"{name}\".");
                    return null;
                }
                IExpression callExpression = ParseFunctionCall(functionCall,
                    function, context);
                if (callExpression == null) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : error while parsing function call \"{functionCall}\".");
                    return null;
                }
                //Debug.LogWarning($"Parser.ParseExpression(\"{expressionString}\") => function call \"{functionCall}\" => \"{callExpression.Script()}\"");
                output.Push(callExpression);
                continue;
            }
            // token is a symbol (variable or literal)
            IExpression tokenExpression = ParseToken(token, context);
            if (tokenExpression == null) {
                Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : parsing error in token \"{token}\".");
                return null;
            }
            //Debug.LogWarning($"Parser.ParseExpression(\"{expressionString}\") => token \"{token}\" => {tokenExpression.Type()} : {tokenExpression.Script()}");
            output.Push(tokenExpression);
        }
        while (operators.Count > 0) {
            OperatorType type = operators.Peek();
            if (type == OperatorType.OpeningParenthesis ||
                type == OperatorType.ClosingParenthesis) {
                Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : parentheses mitchmatch.");
                return null;
            }
            ProcessOperation(output, operators);
        }
        //Debug.LogWarning("ot="+string.Join(" ----- ", output.Select(o => o.Script())));
        //Debug.LogWarning("op="+string.Join(" ----- ", operators));
        if (output.Count != 1 || operators.Count != 0) {
            Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : parsing algorithm error.");
            return null;
        }
        return output.Pop();
    }

    private static OperatorType ProcessOperation(Stack<IExpression> output,
        Stack<OperatorType> operators) {
        Assert.IsTrue(output.Count >= 2 && operators.Count > 0);
        OperatorType operation = operators.Pop();
        IExpression right = output.Pop();
        IExpression left = output.Pop();
        SymbolType leftType = left.Type(), rightType = right.Type();
        if (left.Type() != right.Type()) {
            Debug.LogWarning("ot="+string.Join(" ----- ", output.Select(o => o.Script())));
            Debug.LogWarning("op="+string.Join(" ----- ", operators));
            Debug.LogWarning("o1=" + left.Type()+":"+left.Script());
            Debug.LogError($"Parser.CreateOperation : invalid {operation} between {leftType} ({left.Script()}) and {rightType} ({right.Script()}).");
            return OperatorType.IllegalOperation;
        }
        IExpression expression;
        if (Operations.IsComparisonOperator(operation)) {
            switch (leftType) {
                case SymbolType.Boolean:
                    expression = new ComparisonExpression<bool>(operation,
                        left as Expression<bool>, right as Expression<bool>);
                    break;
                case SymbolType.Integer:
                    expression = new ComparisonExpression<int>(operation,
                        left as Expression<int>, right as Expression<int>);
                    break;
                case SymbolType.Float:
                    expression = new ComparisonExpression<float>(operation,
                        left as Expression<float>, right as Expression<float>);
                    break;
                case SymbolType.Id:
                case SymbolType.String:
                    expression = new ComparisonExpression<string>(operation,
                        left as Expression<string>, right as Expression<string>);
                    break;
                case SymbolType.Date:
                    expression = new ComparisonExpression<DateTime>(operation,
                        left as Expression<DateTime>, right as Expression<DateTime>);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else {
            switch (leftType) {
                case SymbolType.Boolean:
                    expression = new OperationExpression<bool>(operation,
                        left as Expression<bool>, right as Expression<bool>);
                    break;
                case SymbolType.Integer:
                    expression = new OperationExpression<int>(operation,
                        left as Expression<int>, right as Expression<int>);
                    break;
                case SymbolType.Float:
                    expression = new OperationExpression<float>(operation,
                        left as Expression<float>, right as Expression<float>);
                    break;
                case SymbolType.Id:
                case SymbolType.String:
                    expression = new OperationExpression<string>(operation,
                        left as Expression<string>, right as Expression<string>);
                    break;
                case SymbolType.Date:
                    expression = new OperationExpression<DateTime>(operation,
                        left as Expression<DateTime>, right as Expression<DateTime>);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        Assert.IsNotNull(expression);
        output.Push(expression);
        return operation;
    }

    private static IExpression ParseFunctionCall(string call, IFunction metadata,
        ParserContext context) {
        int startIndex = call.IndexOf('(');
        int endIndex = call.LastIndexOf(')');
        if (startIndex == -1 || endIndex == -1 || startIndex > endIndex) {
            Debug.LogError($"ScriptParser.ParseFunctionCall(\"{call}\") : syntax error.");
            return null;
        }

        // Parameters identification
        string current = "";
        List<string> parametersString = new List<string>();
        int openedParentheses = 0, closedParentheses = 0;
        for (int i = startIndex + 1; i < endIndex; i++) {
            char c = call[i];
            if (c == ',' && closedParentheses == openedParentheses) {
                parametersString.Add(current.Trim());
                current = "";
                continue;
            }
            current += c;
            if (c == '(') ++openedParentheses;
            else if (c == ')') ++closedParentheses;
        }
        current = current.Trim();
        if (current != "") parametersString.Add(current);
        int arity = metadata.Parameters().Length;
        if (parametersString.Count != arity) {
            Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : wrong arity ({parametersString.Count} instead of {arity}).");
            return null;
        }

        List<IExpression> parameters = new List<IExpression>();
        SymbolType[] parametersTypes = metadata.Parameters();
        for (int i = 0; i < arity; i++) {
            string parameterString = parametersString[i].Trim();
            IExpression expression = ParseExpression(parameterString, context);
            if (expression == null) {
                Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : cannot parse function argument \"{parameterString}\".");
                return null;
            }
            SymbolType type = parametersTypes[i];
            if (expression.Type() != type && type != SymbolType.Void) { // void : any type
                Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : {expression.Type()} argument \"{parameterString}\" must be of type {type}.");
                return null;
            }
            parameters.Add(expression);
        }

        switch (metadata.ReturnType()) {
            case SymbolType.Void:
                return new FunctionExpression<Void>(metadata as Function<Void>, parameters.ToArray());
            case SymbolType.Boolean:
                return new FunctionExpression<bool>(metadata as Function<bool>, parameters.ToArray());
            case SymbolType.Integer:
                return new FunctionExpression<int>(metadata as Function<int>, parameters.ToArray());
            case SymbolType.Float:
                return new FunctionExpression<float>(metadata as Function<float>, parameters.ToArray());
            case SymbolType.Id:
            case SymbolType.String:
                return new FunctionExpression<string>(metadata as Function<string>, parameters.ToArray());
            case SymbolType.Date:
                return new FunctionExpression<DateTime>(metadata as Function<DateTime>, parameters.ToArray());
            default:
                return null;
        }
    }

    private static IExpression ParseToken(string expression,
        ParserContext context, string declaringLocalVariable = "") {
        expression = expression.Trim();
        if (expression == "") return null;
        // local variable
        if (expression.StartsWith("@")) {
            string variableName = expression.Substring(1);
            LocalVariable localVariable = context.LocalVariables.Find(
                lv => lv.Name == variableName);
            if (variableName != declaringLocalVariable && localVariable == null) {
                Debug.LogError($"Parser.ParseToken(\"{expression}\") : " +
                               $"unkown Local Variable \"@{variableName}\".");
                return null;
            }
            switch (localVariable.Type) {
                case SymbolType.Boolean: return new LocalVariableExpression<bool>(localVariable);
                case SymbolType.Integer: return new LocalVariableExpression<int>(localVariable);
                case SymbolType.Float: return new LocalVariableExpression<float>(localVariable);
                case SymbolType.Id:
                case SymbolType.String: return new LocalVariableExpression<string>(localVariable);
                case SymbolType.Date: return new LocalVariableExpression<DateTime>(localVariable);
                default: throw new ArgumentOutOfRangeException();
            }
        }
        // global variable
        if (expression.StartsWith("$")) {
            string variableName = expression.Substring(1);
            GlobalVariable globalVariable = context.GlobalVariables.Find(
                gv => gv.Name == variableName);
            if (globalVariable == null) {
                Debug.LogError($"Parser.ParseToken(\"{expression}\") : unkown Global Variable \"${variableName}\".");
                return null;
            }
            switch (globalVariable.Type) {
                case SymbolType.Boolean: return new GlobalVariableExpression<bool>(globalVariable);
                case SymbolType.Integer: return new GlobalVariableExpression<int>(globalVariable);
                case SymbolType.Float: return new GlobalVariableExpression<float>(globalVariable);
                case SymbolType.Id:
                case SymbolType.String: return new GlobalVariableExpression<string>(globalVariable);
                case SymbolType.Date: return new GlobalVariableExpression<DateTime>(globalVariable);
                default: throw new ArgumentOutOfRangeException();
            }
        }
        // universal constant
        switch (expression) {
            case "Math.PI": return new SymbolExpression<float>(new FloatSymbol(Mathf.PI));
        }
        // literal constant
        ISymbol constant = ParseLiteral(expression);
        if (constant == null) {
            Debug.LogError($"Parser.ParseToken(\"{expression}\") : cannot parse as literal.");
            return null;
        }
        switch (constant.Type()) {
            case SymbolType.Boolean: return new SymbolExpression<bool>(constant as Symbol<bool>);
            case SymbolType.Integer: return new SymbolExpression<int>(constant as Symbol<int>);
            case SymbolType.Float: return new SymbolExpression<float>(constant as Symbol<float>);
            case SymbolType.Id:
            case SymbolType.String: return new SymbolExpression<string>(constant as Symbol<string>);
            case SymbolType.Date: return new SymbolExpression<DateTime>(constant as Symbol<DateTime>);
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private static ISymbol ParseLiteral(string token) {
        token = token.Trim();
        if (token == "") return null;
        char firstChar = token[0];
        // boolean literal
        if (token == "true") return new BooleanSymbol(true);
        if (token == "false") return new BooleanSymbol(false);
        // ID literal
        if (char.IsLetter(firstChar)) {
            return new IdSymbol(token);
        }
        // string literal
        if (firstChar == '\'' || firstChar == '\"') {
            Assert.IsTrue(token.EndsWith($"{firstChar}"));
            string literal = token.Substring(1, token.Length - 2);
            return new StringSymbol(literal);
        }
        if (token.Contains("/")) {
            // date literal
            try {
                DateTime date = ParseDate(token);
                return new DateSymbol(date);
            } catch (FormatException e) {
                Debug.LogError($"Parser.ParseLiteral(\"{token}\") : invalid date literal :\n{e.Message}");
                return null;
            }
        }
        // integer literal
        int i;
        if (int.TryParse(token, NumberStyleInteger, Symbol<int>.CultureInfo, out i)) {
            return new IntegerSymbol(i);
        }
        // float literal
        float f;
        if (float.TryParse(token, NumberStyleFloat, Symbol<float>.CultureInfo, out f)) {
            return new FloatSymbol(f);
        }
        return null;
    }

    public static DateTime ParseDate(string date) {
        return DateTime.ParseExact(date, "yyyy/MM/dd", DateCultureInfo);
    }

    private static ISymbol GetDefaultValue(SymbolType type) {
        switch (type) {
            case SymbolType.Invalid: return null;
            case SymbolType.Void: return new VoidSymbol();
            case SymbolType.Boolean: return new BooleanSymbol(false);
            case SymbolType.Integer: return new IntegerSymbol(0);
            case SymbolType.Float: return new FloatSymbol(0);
            case SymbolType.Id: return new IdSymbol("");
            case SymbolType.String: return new StringSymbol("");
            case SymbolType.Date: return new DateSymbol(DateTime.Now);
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

}
