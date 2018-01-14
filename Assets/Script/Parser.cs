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
    private const NumberStyles NumberStyleInteger = NumberStyles.Integer;
    private const NumberStyles NumberStyleFloat =
        NumberStyles.Float | NumberStyles.AllowThousands;

    [SerializeField] private static readonly string[] TokenDelimiters = {
        " ", "(", ")", ",",
        "+", "-", "*", "/", "^",
        "=", "==", "!=", ">", "<", ">=", "<=",
    };

    public static bool ParseVariableDeclaration(string declaration,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        IExpression expression = ParseAssignment(declaration,
            localVariables, globalVariables, functions, true);
        if (expression == null) {
            Debug.LogError(
                $"Parser.ParseVariableDeclaration(\"{declaration}\") : parsing error in declaration.");
            return false;
        }
        return true;
    }

    public static bool ExecuteVariableDeclarations(IScriptContext context) {
        foreach (LocalVariable variable in context.LocalVariables()) {
            if (variable.Declaration == null) {
                if (variable.Value == null)
                    Debug.LogError(
                        $"Parser.ExecuteVariableDeclarations : variable \"@{variable.Name}\" has no declaration.");
                continue;
            }
            ISymbol value;
            switch (variable.Type) {
                case SymbolType.Boolean:
                    var declBool = variable.Declaration as Expression<bool>;
                    Assert.IsNotNull(declBool);
                    value = declBool.Evaluate(context);
                    break;
                case SymbolType.Integer:
                    var declInt = variable.Declaration as Expression<int>;
                    Assert.IsNotNull(declInt);
                    value = declInt.Evaluate(context);
                    break;
                case SymbolType.Float:
                    var declFloat = variable.Declaration as Expression<float>;
                    Assert.IsNotNull(declFloat);
                    value = declFloat.Evaluate(context);
                    break;
                case SymbolType.Id:
                case SymbolType.String:
                    var declString = variable.Declaration as Expression<string>;
                    Assert.IsNotNull(declString);
                    value = declString.Evaluate(context);
                    break;
                case SymbolType.Date:
                    var declDateTime = variable.Declaration as Expression<DateTime>;
                    Assert.IsNotNull(declDateTime);
                    value = declDateTime.Evaluate(context);
                    break;
                default:
                    Debug.LogError(
                        $"Parser.ExecuteVariableDeclarations : Type Error for Local Variable \"{variable.Name}\".");
                    return false;
            }
            variable.Value = value;
            if (value.Type() != variable.Type) {
                Debug.LogError(
                    $"Parser.ExecuteVariableDeclarations : Type Error for Local Variable \"{variable.Name}\".");
                return false;
            }
            Debug.LogWarning($"Script Declaration : @{variable.Name} : {variable.Type} = {variable.Declaration.Script()} => {value.ValueString()}.");
        }
        return true;
    }

    public static void ExecuteScript(IScriptContext context, string script,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        Debug.LogWarning("script = " + script);
        IExpression expression = ParseExpression(script,
            localVariables, globalVariables, functions);
        Debug.LogWarning("=> script expression = " + expression.Script());
        ISymbol result = expression.EvaluateAsISymbol(context);
        Debug.LogWarning("=> result = " + result.Type() + " = " + result.ValueString());
    }

    public static IExpression ParseAssignment(string assignment,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions, bool declaration = false) {
        int decl = declaration ? 2 : 0;

        string a = assignment.Trim();
        List<string> tokens = Tokenize(a);
        if (tokens == null) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : tokenization error.");
            return null;
        }
        if (tokens.Count < 3 + decl) {
            Debug.LogError($"Parse.ParseAssignment(\"{a}\") : illegal declaration \"{a}\"");
            return null;
        }

        string operation = tokens[1 + decl].Trim();
        string variable = tokens[0].Trim();
        string variableName = variable.Substring(1);
        if (variableName.Length < 1) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : illegal empty variable name.");
            return null;
        }
        bool global = variable.StartsWith("$");
        Assert.IsTrue(global && !declaration || variable.StartsWith("@"));

        AssignmentType assignmentType;
        if (!Operations.AssignmentTypeFromString(operation, out assignmentType)) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unsupported assignment \"{operation}\".");
            return null;
        }

        SymbolType type = SymbolType.Invalid;
        if (declaration) {
            if (!variable.StartsWith("@") || tokens[1].Trim() != ":") {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : illegal declaration \"{a}\"");
                return null;
            }
            string typeString = tokens[2].Trim();
            if (!Symbol<bool>.SymbolTypeFromString(typeString, out type)) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unsupported Type \"{typeString}\"");
                return null;
            }
        } else if (variable.StartsWith("@")) { // local variable
            LocalVariable localVariable = localVariables.Find(lv => lv.Name == variableName);
            if (localVariable == null) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unkown local Variable \"{variableName}\"");
                return null;
            }
            type = localVariable.Type;
        }
        if (variable.StartsWith("$")) { // global variable
            Assert.IsFalse(declaration);
            GlobalVariable globalVariable = globalVariables.Find(gv => gv.Name == variableName);
            if (globalVariable == null) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unkown Game Variable \"{variableName}\"");
                return null;
            }
            type = globalVariable.Type;
        }
        Assert.IsFalse(type == SymbolType.Invalid);

        //Debug.LogWarning($"==> parse assignment \"{a}\" => right expr tokens = {string.Join(" --- ", tokens.Skip(2+decl))}");
        IExpression rightExpression = ParseExpression(tokens.Skip(2 + decl).ToArray(),
            localVariables, globalVariables, functions);
        if (rightExpression == null) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : parsing error in declaration.");
            return null;
        }
        if (rightExpression.Type() != type) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : right expression {rightExpression.Type()} instead of {type} (\"{rightExpression.Script()}\").");
            return null;
        }
        if (declaration) {
            LocalVariable localVariable = new LocalVariable(variableName, type, rightExpression);
            localVariables.Add(localVariable);
        }

        IExpression variableExpression = ParseToken(variable, localVariables, globalVariables,
            declaration ? variableName : "");
        if (variableExpression == null) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : parsing error in variable \"{variable}\".");
            return null;
        }
        Assert.IsTrue(variableExpression.Script() == variable);

        //Debug.LogWarning($"==> parsed {type} assignment \"{a}\" : {variableExpression.Script()} {assignmentType} {rightExpression.Script()}");
        IExpression assignmentExpression;
        switch (type) {
            case SymbolType.Boolean:
                assignmentExpression = new AssignmentExpression<bool>(assignmentType,
                    variableExpression as Expression<bool>, rightExpression as Expression<bool>);
                break;
            case SymbolType.Integer:
                assignmentExpression = new AssignmentExpression<int>(assignmentType,
                    variableExpression as Expression<int>, rightExpression as Expression<int>);
                break;
            case SymbolType.Float:
                assignmentExpression = new AssignmentExpression<float>(assignmentType,
                    variableExpression as Expression<float>, rightExpression as Expression<float>);
                break;
            case SymbolType.Id:
            case SymbolType.String:
                assignmentExpression = new AssignmentExpression<string>(assignmentType,
                    variableExpression as Expression<string>, rightExpression as Expression<string>);
                break;
            case SymbolType.Date:
                assignmentExpression = new AssignmentExpression<DateTime>(assignmentType,
                    variableExpression as Expression<DateTime>, rightExpression as Expression<DateTime>);
                break;
            default:
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : invalid assignment.");
                return null;
        }
        return assignmentExpression;
    }

    public static Expression<bool> ParseComparison(string comparison,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        string c = comparison.Trim();
        List<string> tokens = Tokenize(c);
        if (tokens == null) {
            Debug.LogError($"Parser.ParseComparison(\"{c}\") : tokenization error.");
            return null;
        }
        if (tokens.Count < 3) {
            Debug.LogError($"Parse.ParseComparison(\"{c}\") : illegal comparison \"{c}\"");
            return null;
        }

        List<string> leftTokens = new List<string>(), rightTokens = new List<string>();
        leftTokens.Add(tokens[0].Trim());
        // Comparison type
        bool valid = false;
        ComparisonType comparisonType = ComparisonType.IllegalComparison;
        foreach (string token in tokens.Skip(1)) {
            string t = token.Trim();
            if (valid) { // right expression
                rightTokens.Add(t);
                continue;
            }
            string found = Array.Find(Operations.ComparisonOperators, o => o == t);
            if (!string.IsNullOrEmpty(found)) { // comparison operator
                if (!Operations.ComparisonTypeFromString(found, out comparisonType)) {
                    Debug.LogError($"Parse.ParseComparison(\"{c}\") : unsupported comparison \"{found}\"");
                    return null;
                }
                valid = true;
                continue;
            }
            // left expression
            leftTokens.Add(t);

        }
        if (comparisonType == ComparisonType.IllegalComparison) {
            Debug.LogError($"Parser.ParseComparison(\"{c}\") : unsupported comparison type.");
            return null;
        }

        // Left expression
        IExpression leftExpression = ParseExpression(leftTokens.ToArray(),
            localVariables, globalVariables, functions);
        if (leftExpression == null) {
            Debug.LogError($"Parser.ParseComparison(\"{c}\") : parsing error in left expression.");
            return null;
        }

        // Right expression
        IExpression rightExpression = ParseExpression(rightTokens.ToArray(),
            localVariables, globalVariables, functions);
        if (rightExpression == null) {
            Debug.LogError($"Parser.ParseComparison(\"{c}\") : parsing error in right expression.");
            return null;
        }

        // Type check
        if (leftExpression.Type() != rightExpression.Type()) {
            Debug.LogError($"Parser.ParseComparison(\"{c}\") : left type {leftExpression.Type()} != right type {rightExpression.Type()}.");
            return null;
        }

        IExpression comparisonExpression;
        switch (leftExpression.Type()) {
            case SymbolType.Boolean:
                comparisonExpression = new ComparisonExpression<bool>(comparisonType,
                    leftExpression as Expression<bool>, rightExpression as Expression<bool>);
                break;
            case SymbolType.Integer:
                comparisonExpression = new ComparisonExpression<int>(comparisonType,
                    leftExpression as Expression<int>, rightExpression as Expression<int>);
                break;
            case SymbolType.Float:
                comparisonExpression = new ComparisonExpression<float>(comparisonType,
                    leftExpression as Expression<float>, rightExpression as Expression<float>);
                break;
            case SymbolType.Id:
            case SymbolType.String:
                comparisonExpression = new ComparisonExpression<string>(comparisonType,
                    leftExpression as Expression<string>, rightExpression as Expression<string>);
                break;
            case SymbolType.Date:
                comparisonExpression = new ComparisonExpression<DateTime>(comparisonType,
                    leftExpression as Expression<DateTime>, rightExpression as Expression<DateTime>);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return (Expression<bool>) comparisonExpression;
    }

    public static IExpression ParseExpression(string expression,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        bool isAssignment, isComparison;
        List<string> tokens = Tokenize(expression, out isAssignment, out isComparison);
        if (tokens == null) {
            Debug.LogError($"Parser.ParseExpression(\"{expression}\") : tokenization error.");
            return null;
        }
        // comparison or assignment => special handling to simplify the algorithm
        if (isAssignment)
            return ParseAssignment(expression, localVariables, globalVariables,
                functions);
        if (isComparison)
            return ParseComparison(expression, localVariables, globalVariables,
                functions);
        return ParseExpression(tokens.ToArray(), localVariables, globalVariables, functions);
    }

    private static IExpression ParseExpression(string[] tokens,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        string expressionString = string.Join(" ", tokens);
        // use the Shunting-Yard Algorithm to transform the script into
        // a Reverse-Polish Notation list of expressions and operators
        Stack<IExpression> output = new Stack<IExpression>();
        Stack<OperatorType> operators = new Stack<OperatorType>();
        for (int i = 0; i < tokens.Length; i++) {
            string token = tokens[i].Trim();
            if (token == "") continue;
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
                            Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : invalid operation.");
                            return null;
                        }
                        if (operators.Count == 0) break;
                        operation = operators.Peek();
                    }
                    if (operators.Count == 0 ||
                        operators.Peek() != OperatorType.OpeningParenthesis) {
                        Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : parentheses error (missing '(').");
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
            IFunction function = functions.Find(f => f.Name() == token);
            if (function != null) {
                string name = token;
                if (i + 2 >= tokens.Length || tokens[i + 1].Trim() != "(") {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : invalid function call for \"{token}\".");
                    return null;
                }
                bool ends = false;
                string functionCall = $"{name}(";
                int openingParentheses = 1, closingParentheses = 0;
                for (int j = i + 2; j < tokens.Length; j++) {
                    string t = tokens[j];
                    string tTrimmed = t.Trim();
                    functionCall += $"{t}";
                    if (tTrimmed == "(") ++openingParentheses;
                    if (tTrimmed == ")" && ++closingParentheses == openingParentheses) {
                        i = j;
                        ends = true;
                        break;
                    }
                }
                if (!ends) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : missing closing parenthesis for function call \"{name}\".");
                    return null;
                }
                IExpression callExpression = ParseFunctionCall(functionCall, function,
                    localVariables, globalVariables, functions);
                if (callExpression == null) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : error while parsing function call \"{functionCall}\".");
                    return null;
                }
                //Debug.LogWarning($"Parser.ParseExpression(\"{expressionString}\") => function call \"{functionCall}\" => \"{callExpression.Script()}\"");
                output.Push(callExpression);
                continue;
            }
            // token is a symbol (variable or literal)
            IExpression tokenExpression = ParseToken(token, localVariables, globalVariables);
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
        Assert.IsNotNull(expression);
        output.Push(expression);
        return operation;
    }

    private static IExpression ParseFunctionCall(string call, IFunction metadata,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
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
            IExpression expression = ParseExpression(parameterString,
                localVariables, globalVariables, functions);
            if (expression == null) {
                Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : cannot parse function argument \"{parameterString}\".");
                return null;
            }
            SymbolType type = parametersTypes[i];
            if (expression.Type() != type) {
                Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : {expression.Type()} argument \"{parameterString}\" must be of type {type}.");
                return null;
            }
            parameters.Add(expression);
        }

        switch (metadata.ReturnType()) {
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
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        string declaringLocalVariable = "") {
        expression = expression.Trim();
        if (expression == "") return null;
        // local variable
        if (expression.StartsWith("@")) {
            string variableName = expression.Substring(1);
            LocalVariable localVariable = localVariables.Find(l => l.Name == variableName);
            if (variableName != declaringLocalVariable && localVariable == null) {
                Debug.LogError($"Parser.ParseToken(\"{expression}\") : unkown Local Variable \"@{variableName}\".");
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
            GlobalVariable globalVariable = globalVariables.Find(gv => gv.Name == variableName);
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
            string literal = token.Substring(1, token.Length - 1);
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

    public static List<string> Tokenize(string script) {
        bool a, b;
        return Tokenize(script, out a, out b);
    }

    private static List<string> Tokenize(string script,
        out bool isAssignment, out bool isComparison) {
        string token = "";
        bool assignmentDoubleLetter = false;
        isAssignment = isComparison = false;
        List<string> tokens = new List<string>();
        for (int i = 0; i < script.Length; i++) {
            char c = script[i];
            if (c == '\'') {
                token = token.Trim();
                if (token != "") tokens.Add(token);
                token = $"{c}";
                bool ends = false;
                char openingChar = c;
                for (int j = i + 1; j < script.Length; j++) {
                    char character = script[j];
                    token += character;
                    if (character == openingChar && script[j - 1] != '\\') {
                        tokens.Add(token);
                        token = "";
                        ends = true;
                        i = j;
                        break;
                    }
                }
                if (!ends) {
                    Debug.LogError($"Parser.Tokenize(\"{script}\") : string quote must ends.");
                    return null;
                }
                continue;
            }
            string cString = $"{c}";
            bool isOperator = TokenDelimiters.Contains(cString);
            if (isOperator) {
                if (i + 1 < script.Length) {
                    if ((c == '=' || c == '+' || c == '-' || c == '*' || c == '/' ||
                         c == '^' || c == '%' || c == '!' || c == '<' || c == '>') &&
                        script[i + 1] == '=') { // "==", "+=", "-=", "!=", ">=", ...
                        assignmentDoubleLetter = true;
                        token += c;
                        continue;
                    }
                    if ((c == '+' || c == '-') && char.IsDigit(script[i + 1])) { // number sign
                        // TODO : support math expressions such as "3+5" => 3 + 5
                        token += c;
                        continue;
                    }
                    // date support : treat every "yyyy/MM/dd" as a single token
                    if (i > 0 && c == '/' && char.IsDigit(script[i - 1]) &&
                        char.IsDigit(script[i + 1])) {
                        token += c;
                        continue;
                    }
                }
                if (assignmentDoubleLetter) token += c;
                token = token.Trim();

                // expression type basic identification
                if (!assignmentDoubleLetter && c == '=') isAssignment = true;
                if (!isAssignment && !isComparison) {
                    string tested = assignmentDoubleLetter ? token : $"{c}";
                    AssignmentType assignmentType;
                    if (Operations.AssignmentTypeFromString(tested, out assignmentType)) // assignment ?
                        isAssignment = true;
                    ComparisonType comparisonType;
                    if (!isAssignment &&
                        Operations.ComparisonTypeFromString(tested, out comparisonType)) // comparison ?
                        isComparison = true;
                }

                if (token != "") tokens.Add(token);
                if (!assignmentDoubleLetter && c != ' ') tokens.Add(cString);
                assignmentDoubleLetter = false;
                token = "";
                continue;
            }
            token += c;
        }
        tokens.Add(token.Trim());
        return tokens;
    }
}

}
