using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Script {

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

        // Identification
        bool declaration = tokens[0] == context.Grammar.VariableDeclarator ||
                           tokens[0] == context.Grammar.ConstantDeclarator;
        string variable = tokens[declaration ? 1 : 0];
        bool global = variable.StartsWith("$");
        string variableName = global ? variable.Substring(1) : variable;
        if (variableName.Length < (global ? 2 : 1) ||
            !global && !context.Grammar.ValidateVariableName(variableName)) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : illegal variable name \"{variableName}\".");
            return null;
        }
        int decl = declaration ? 3 : 0;

        // Type
        LocalVariable localVariable = null;
        GlobalVariable globalVariable = null;
        SymbolType type = SymbolType.Invalid, arrayType = SymbolType.Invalid;
        if (declaration) {
            if (tokens[2] != context.Grammar.TypeDeclarator) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : illegal declaration \"{a}\".");
                return null;
            }
            string typeString = tokens[3];
            if (tokens[4] == "[") { // array
                if (tokens[5] != "]") {
                    Debug.LogError($"Parser.ParseAssignment(\"{a}\") : invalid Array declaration.");
                    return null;
                }
                decl += 2;
                type = SymbolType.Array;
                if (!Symbol<bool>.SymbolTypeFromString(typeString, out arrayType)) {
                    Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unsupported Array Type \"{typeString}\".");
                    return null;
                }
            } else if (!Symbol<bool>.SymbolTypeFromString(typeString, out type)) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unsupported Type \"{typeString}\".");
                return null;
            }
        } else if (!global) { // assignment on existing local variable
            localVariable = context.LocalVariables.Find(lv => lv.Name == variableName);
            if (localVariable == null) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unknown local Variable \"{variableName}\"");
                return null;
            }
            if (!localVariable.Mutable) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : local Variable " +
                               $"\"{variableName}\" is immutable and cannot be re-assigned.");
                return null;
            }
            type = localVariable.Type;
        }
        if (global) { // global variable
            Assert.IsTrue(localVariable == null);
            if (declaration) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : cannot declare a Global Variable.");
                return null;
            }
            globalVariable = context.GlobalVariables.Find(gv => gv.Name == variableName);
            if (globalVariable == null) {
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unknown Game Variable \"{variableName}\"");
                return null;
            }
            type = globalVariable.Type;
        }
        Assert.IsFalse(type == SymbolType.Invalid);

        // Variable creation for declarations
        if (declaration) {
            bool mutable = tokens[0] == context.Grammar.VariableDeclarator;
            Assert.IsTrue(localVariable == null && globalVariable == null);
            if (type == SymbolType.Array) {
                ISymbol symbol;
                switch (arrayType) {
                    case SymbolType.Void:
                        symbol = new ArraySymbol<Void>(new List<Expression<Void>>(), arrayType);
                        break;
                    case SymbolType.Boolean:
                        symbol = new ArraySymbol<bool>(new List<Expression<bool>>(), arrayType);
                        break;
                    case SymbolType.Integer:
                        symbol = new ArraySymbol<int>(new List<Expression<int>>(), arrayType);
                        break;
                    case SymbolType.Float:
                        symbol = new ArraySymbol<float>(new List<Expression<float>>(), arrayType);
                        break;
                    case SymbolType.Id:
                        symbol = new ArraySymbol<Id>(new List<Expression<Id>>(), arrayType);
                        break;
                    case SymbolType.String:
                        symbol = new ArraySymbol<string>(new List<Expression<string>>(), arrayType);
                        break;
                    case SymbolType.Date:
                        symbol = new ArraySymbol<DateTime>(new List<Expression<DateTime>>(), arrayType);
                        break;
                    default:
                        Debug.LogError($"Parser.ParseAssignment(\"{a}\") : unsupported Array type \"{arrayType}[]\".");
                        return null;
                }
                localVariable = new LocalVariable(variableName, symbol, mutable);
            } else
                localVariable = new LocalVariable(variableName, GetDefaultValue(type), mutable);
            context.LocalVariables.Add(localVariable);
        }
        if (global) Assert.IsNotNull(globalVariable);
        else Assert.IsNotNull(localVariable);

        // Assignment type
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

        // Right expression
        IExpression rightExpression = ParseExpression(tokens.Skip(2 + decl).ToArray(),
            context);
        if (rightExpression == null) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : parsing error in declaration.");
            return null;
        }
        if (rightExpression.Type() != type) {
            Debug.LogError($"Parser.ParseAssignment(\"{a}\") : right expression {rightExpression.Type()} " +
                           $"instead of {type} (\"{rightExpression.Script()}\").");
            return null;
        }

        // Return type : if ends with ';' void, else returns the computed value
        bool returnsType = tokens.Last() != ";";

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
                if (global)
                    variableExpression = new GlobalVariableExpression<Id>(globalVariable);
                else
                    variableExpression = new LocalVariableExpression<Id>(localVariable);
                if (returnsType)
                    assignmentExpression = new AssignmentExpression<Id, Id>(assignmentType,
                        variableExpression, rightExpression as Expression<Id>, returnsType);
                else
                    assignmentExpression = new AssignmentExpression<Void, Id>(assignmentType,
                        variableExpression, rightExpression as Expression<Id>, returnsType);
                break;
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
            case SymbolType.Array:
                switch (arrayType) {
                    case SymbolType.Void:
                        if (global)
                            variableExpression = new GlobalVariableExpression<Array<Void>>(globalVariable);
                        else
                            variableExpression = new LocalVariableExpression<Array<Void>>(localVariable);
                        ArrayAssignmentExpression<Void> voidArrayAssignment = new ArrayAssignmentExpression<Void>(
                            assignmentType, variableExpression, (ArrayExpression<Void, Array<Void>>) rightExpression);
                        if (returnsType)
                            assignmentExpression = voidArrayAssignment;
                        else
                            assignmentExpression = new VoidArrayAssignmentExpression<Void>(voidArrayAssignment);
                        break;
                    case SymbolType.Boolean:
                        if (global)
                            variableExpression = new GlobalVariableExpression<Array<bool>>(globalVariable);
                        else
                            variableExpression = new LocalVariableExpression<Array<bool>>(localVariable);
                        ArrayAssignmentExpression<bool> boolArrayAssignment = new ArrayAssignmentExpression<bool>(
                            assignmentType, variableExpression, (ArrayExpression<bool, Array<bool>>) rightExpression);
                        if (returnsType)
                            assignmentExpression = boolArrayAssignment;
                        else
                            assignmentExpression = new VoidArrayAssignmentExpression<bool>(boolArrayAssignment);
                        break;
                    case SymbolType.Integer:
                        if (global)
                            variableExpression = new GlobalVariableExpression<Array<int>>(globalVariable);
                        else
                            variableExpression = new LocalVariableExpression<Array<int>>(localVariable);
                        ArrayAssignmentExpression<int> intArrayAssignment = new ArrayAssignmentExpression<int>(
                            assignmentType, variableExpression, (ArrayExpression<int, Array<int>>) rightExpression);
                        if (returnsType)
                            assignmentExpression = intArrayAssignment;
                        else
                            assignmentExpression = new VoidArrayAssignmentExpression<int>(intArrayAssignment);
                        break;
                    case SymbolType.Float:
                        if (global)
                            variableExpression = new GlobalVariableExpression<Array<float>>(globalVariable);
                        else
                            variableExpression = new LocalVariableExpression<Array<float>>(localVariable);
                        ArrayAssignmentExpression<float> floatArrayAssignment = new ArrayAssignmentExpression<float>(
                            assignmentType, variableExpression, (ArrayExpression<float, Array<float>>) rightExpression);
                        if (returnsType)
                            assignmentExpression = floatArrayAssignment;
                        else
                            assignmentExpression = new VoidArrayAssignmentExpression<float>(floatArrayAssignment);
                        break;
                    case SymbolType.Id:
                        if (global)
                            variableExpression = new GlobalVariableExpression<Array<Id>>(globalVariable);
                        else
                            variableExpression = new LocalVariableExpression<Array<Id>>(localVariable);
                        ArrayAssignmentExpression<Id> idArrayAssignment = new ArrayAssignmentExpression<Id>(
                            assignmentType, variableExpression, (ArrayExpression<Id, Array<Id>>) rightExpression);
                        if (returnsType)
                            assignmentExpression = idArrayAssignment;
                        else
                            assignmentExpression = new VoidArrayAssignmentExpression<Id>(idArrayAssignment);
                        break;
                    case SymbolType.String:
                        if (global)
                            variableExpression = new GlobalVariableExpression<Array<string>>(globalVariable);
                        else
                            variableExpression = new LocalVariableExpression<Array<string>>(localVariable);
                        ArrayAssignmentExpression<string> stringArrayAssignment = new ArrayAssignmentExpression<string>(
                            assignmentType, variableExpression, (ArrayExpression<string, Array<string>>) rightExpression);
                        if (returnsType)
                            assignmentExpression = stringArrayAssignment;
                        else
                            assignmentExpression = new VoidArrayAssignmentExpression<string>(stringArrayAssignment);
                        break;
                    case SymbolType.Date:
                        if (global)
                            variableExpression = new GlobalVariableExpression<Array<DateTime>>(globalVariable);
                        else
                            variableExpression = new LocalVariableExpression<Array<DateTime>>(localVariable);
                        ArrayAssignmentExpression<DateTime> dateArrayAssignment = new ArrayAssignmentExpression<DateTime>(
                            assignmentType, variableExpression, (ArrayExpression<DateTime, Array<DateTime>>) rightExpression);
                        if (returnsType)
                            assignmentExpression = dateArrayAssignment;
                        else
                            assignmentExpression = new VoidArrayAssignmentExpression<DateTime>(dateArrayAssignment);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            default:
                Debug.LogError($"Parser.ParseAssignment(\"{a}\") : invalid assignment.");
                return null;
        }
        return assignmentExpression;
    }

    public static IExpression ParseExpression(string expression,
        ParserContext context) {
        bool isAssignment;
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
                        $"Parser.ParseExpression(\"{expressionString}\") : ';' before the expression ends, " +
                        $"ignoring the remaining tokens \"{string.Join(" ", tokens.Skip(i + 1))}\".");
                break;
            }
            // is token the start of an array '[' ?
            if (token == "[") {
                string arrayDecl = "[";
                bool ends = false;
                int openingArray = 1, closingArray = 0;
                for (int j = i + 1; j < tokens.Length; j++) {
                    string t = tokens[j];
                    arrayDecl += $"{t}";
                    if (t == "[") ++openingArray;
                    if (t == "]" && ++closingArray == openingArray) {
                        i = j;
                        ends = true;
                        break;
                    }
                }
                if (!ends) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                   $"missing closing ']' for Array declaration.");
                    return null;
                }
                IExpression arrayExpression = ParseArray(arrayDecl, context);
                if (arrayExpression == null) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                   $"error while parsing Array declaration \"{arrayDecl}\".");
                    return null;
                }
                //Debug.LogWarning($"Parser.ParseExpression(\"{expressionString}\") => Array declaration \"{arrayDecl}\" => \"{arrayExpression.Script()}\"");
                output.Push(arrayExpression);
                continue;
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
                // sanity checks
                string name = token;
                if (i + 2 >= tokens.Length || tokens[i + 1] != "(") {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") " +
                                   $": invalid function call for \"{token}\".");
                    return null;
                }
                // call parsing
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
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                   $"missing closing parenthesis for function call \"{name}\".");
                    return null;
                }
                IExpression callExpression = ParseFunctionCall(functionCall,
                    function, context);
                if (callExpression == null) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                   $"error while parsing function call \"{functionCall}\".");
                    return null;
                }
                //Debug.LogWarning($"Parser.ParseExpression(\"{expressionString}\") => function call \"{functionCall}\" => \"{callExpression.Script()}\"");
                output.Push(callExpression);
                continue;
            }
            // is token the start of a function invocation on a symbol ?
            // eg : a.ToInt() => float.ToInt(a) if a : float
            int invocationIndex = token.IndexOf('.');
            if (invocationIndex > 0 && !token.StartsWith("@") && // exlude ID literals
                !char.IsDigit(token[invocationIndex - 1]) && // exclude float literals
                !token.StartsWith("$")) { // exlude global variables
                // sanity checks
                string[] subtokens = token.Split('.');
                if (subtokens.Length != 2 || i == tokens.Length -1 || tokens[i + 1] != "(") {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") " +
                                   $": invalid function invocation for \"{token}\".");
                    return null;
                }
                // function identification
                string variableName = token.Substring(0, invocationIndex);
                string functionName = token.Substring(invocationIndex + 1);
                LocalVariable localVariable = context.LocalVariables.Find(
                    lv => lv.Name == variableName);
                if (localVariable == null) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : invalid invocation " +
                                   $"of \"{functionName}\" : unknown Local Variable \"{variableName}\".");
                    return null;
                }
                string fullFunctionName = Symbol<Void>.FunctionNameFromInvocation(
                    functionName, localVariable.Type);
                if (string.IsNullOrEmpty(fullFunctionName)) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : invalid invocation of " +
                                   $"\"{functionName}\" on {localVariable.Type}: unknown function \"{functionName}\".");
                    return null;
                }
                IFunction invocation = context.Functions.Find(f => f.Name() == fullFunctionName);
                if (invocation == null) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : invalid invocation of " +
                                   $"\"{functionName}\" on {localVariable.Type}: unknown function \"{fullFunctionName}\".");
                    return null;
                }
                // call parsing
                string functionCall = $"{functionName}({localVariable.Name}";
                bool ends = false;
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
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                   $"missing closing parenthesis for function invocation \"{functionName}\".");
                    return null;
                }
                IExpression callExpression = ParseFunctionInvocation(functionCall,
                    invocation, context);
                if (callExpression == null) {
                    Debug.LogError($"Parser.ParseExpression(\"{expressionString}\") : " +
                                   $"error while parsing function invocation \"{functionCall}\".");
                    return null;
                }
                //Debug.LogWarning($"Parser.ParseExpression(\"{expressionString}\") => function invocation \"{functionCall}\" => \"{callExpression.Script()}\"");
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
        if (leftType != rightType) {
            Debug.LogWarning("ot="+string.Join(" ----- ", output.Select(o => o.Script())));
            Debug.LogWarning("op="+string.Join(" ----- ", operators));
            Debug.LogWarning("o1=" + left.Type()+":"+left.Script());
            Debug.LogError($"Parser.ProcessOperation : invalid {operation} between {leftType} " +
                           $"({left.Script()}) and {rightType} ({right.Script()}).");
            return OperatorType.IllegalOperation;
        }
        if (leftType == SymbolType.Array && rightType == SymbolType.Array &&
            left.ArrayType() != right.ArrayType()) {
            Debug.LogError( "Parser.ProcessOperation : cannot compare Array " +
                            $"Types {left.ArrayType()} and {right.ArrayType()}.");
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
                    expression = new ComparisonExpression<Id>(operation,
                        left as Expression<Id>, right as Expression<Id>);
                    break;
                case SymbolType.String:
                    expression = new ComparisonExpression<string>(operation,
                        left as Expression<string>, right as Expression<string>);
                    break;
                case SymbolType.Date:
                    expression = new ComparisonExpression<DateTime>(operation,
                        left as Expression<DateTime>, right as Expression<DateTime>);
                    break;
                case SymbolType.Array:
                    switch (left.ArrayType()) {
                        case SymbolType.Void:
                            expression = new ComparisonExpression<Void>(operation,
                                left as Expression<Void>, right as Expression<Void>);
                            break;
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
                            expression = new ComparisonExpression<Id>(operation,
                                left as Expression<Id>, right as Expression<Id>);
                            break;
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
                    expression = new OperationExpression<Id>(operation,
                        left as Expression<Id>, right as Expression<Id>);
                    break;
                case SymbolType.String:
                    expression = new OperationExpression<string>(operation,
                        left as Expression<string>, right as Expression<string>);
                    break;
                case SymbolType.Date:
                    expression = new OperationExpression<DateTime>(operation,
                        left as Expression<DateTime>, right as Expression<DateTime>);
                    break;
                case SymbolType.Array:
                    switch (left.ArrayType()) {
                        case SymbolType.Void:
                            expression = new OperationExpression<Void>(operation,
                                left as Expression<Void>, right as Expression<Void>);
                            break;
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
                            expression = new OperationExpression<Id>(operation,
                                left as Expression<Id>, right as Expression<Id>);
                            break;
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
        // Parameters identification
        List<string> parametersString = ProcessList(call, '(', ')', ',');
        int arity = metadata.Parameters().Length;
        if (parametersString.Count != arity) {
            Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : wrong arity " +
                           $"({parametersString.Count} instead of {arity}).");
            return null;
        }

        // Parameters parsing
        List<IExpression> parameters = new List<IExpression>();
        SymbolType[] parametersTypes = metadata.Parameters();
        for (int i = 0; i < arity; i++) {
            string parameterString = parametersString[i].Trim();
            IExpression expression = ParseExpression(parameterString, context);
            if (expression == null) {
                Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : cannot " +
                               $"parse function argument \"{parameterString}\".");
                return null;
            }
            SymbolType type = parametersTypes[i];
            if (expression.Type() != type && type != SymbolType.Void) { // void : any type
                Debug.LogError($"Parser.ParseFunctionCall(\"{call}\") : {expression.Type()} " +
                               $"argument \"{parameterString}\" must be of type {type}.");
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
                return new FunctionExpression<Id>(metadata as Function<Id>, parameters.ToArray());
            case SymbolType.String:
                return new FunctionExpression<string>(metadata as Function<string>, parameters.ToArray());
            case SymbolType.Date:
                return new FunctionExpression<DateTime>(metadata as Function<DateTime>, parameters.ToArray());
            default:
                return null;
        }
    }

    /// <summary>
    /// Parse a function invocation on a symbol.
    /// For instance, if 'a' is a FloatSymbol :
    /// 'a.ToInt()' will be treated as 'float.ToInt(a)'.
    /// </summary>
    private static IExpression ParseFunctionInvocation(string call,
        IFunction metadata, ParserContext context) {
        // Parameters identification
        List<string> parametersString = ProcessList(call, '(', ')', ',');
        int arity = metadata.Parameters().Length;
        if (parametersString.Count != arity) {
            Debug.LogError($"Parser.ParseFunctionInvocation(\"{call}\") : wrong arity " +
                           $"({parametersString.Count} instead of {arity}).");
            return null;
        }

        // Parameters parsing
        List<IExpression> parameters = new List<IExpression>();
        for (int i = 0; i < parametersString.Count; i++) {
            string parameterString = parametersString[i].Trim();
            IExpression expression = ParseExpression(parameterString, context);
            if (expression == null) {
                Debug.LogError($"Parser.ParseFunctionInvocation(\"{call}\") : cannot " +
                               $"parse function argument n°{i} \"{parameterString}\".");
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
                return new FunctionExpression<Id>(metadata as Function<Id>, parameters.ToArray());
            case SymbolType.String:
                return new FunctionExpression<string>(metadata as Function<string>, parameters.ToArray());
            case SymbolType.Date:
                return new FunctionExpression<DateTime>(metadata as Function<DateTime>, parameters.ToArray());
            default:
                return null;
        }
    }

    private static IExpression ParseArray(string array, ParserContext context) {
        SymbolType elementsType = SymbolType.Invalid;
        List<IExpression> elements = new List<IExpression>();
        List<string> elementsString = ProcessList(array, '[', ']', ',');
        for (int i = 0; i < elementsString.Count; i++) {
            string elementString = elementsString[i].Trim();
            IExpression expression = ParseExpression(elementString, context);
            if (expression == null) {
                Debug.LogError($"Parser.ParseArray(\"{array}\") : cannot " +
                               $"parse element n°{i} \"{elementString}\".");
                return null;
            }
            if (i == 0) {
                elementsType = expression.Type();
            } else if (expression.Type() != elementsType) {
                Debug.LogError($"Parser.ParseArray(\"{array}\") : type mismatch for element " +
                               $"n°{i} ({expression.Type()} instead of {elementsType}).");
                return null;
            }
            elements.Add(expression);
        }

        // empty array
        if (elements.Count == 0)
            return new ArrayExpression<Void, Array<Void>>(new ArraySymbol<Void>(
                new List<Expression<Void>>(), SymbolType.Void), SymbolType.Void);

        switch (elementsType) {
            case SymbolType.Void:
                List<Expression<Void>> voids = elements.Cast<Expression<Void>>().ToList();
                Assert.IsNotNull(voids);
                return new ArrayExpression<Void, Array<Void>>(new ArraySymbol<Void>(voids, elementsType), SymbolType.Void);
            case SymbolType.Boolean:
                List<Expression<bool>> bools = elements.Cast<Expression<bool>>().ToList();
                Assert.IsNotNull(bools);
                return new ArrayExpression<bool, Array<bool>>(new ArraySymbol<bool>(bools, elementsType), SymbolType.Boolean);
            case SymbolType.Integer:
                List<Expression<int>> ints = elements.Cast<Expression<int>>().ToList();
                Assert.IsNotNull(ints);
                return new ArrayExpression<int, Array<int>>(new ArraySymbol<int>(ints, elementsType), SymbolType.Integer);
            case SymbolType.Float:
                List<Expression<float>> floats = elements.Cast<Expression<float>>().ToList();
                Assert.IsNotNull(floats);
                return new ArrayExpression<float, Array<float>>(new ArraySymbol<float>(floats, elementsType), SymbolType.Float);
            case SymbolType.Id:
                List<Expression<Id>> ids = elements.Cast<Expression<Id>>().ToList();
                Assert.IsNotNull(ids);
                return new ArrayExpression<Id, Array<Id>>(new ArraySymbol<Id>(ids, elementsType), SymbolType.Id);
            case SymbolType.String:
                List<Expression<string>> strings = elements.Cast<Expression<string>>().ToList();
                Assert.IsNotNull(strings);
                return new ArrayExpression<string, Array<string>>(new ArraySymbol<string>(strings, elementsType), SymbolType.String);
            case SymbolType.Date:
                List<Expression<DateTime>> dates = elements.Cast<Expression<DateTime>>().ToList();
                Assert.IsNotNull(dates);
                return new ArrayExpression<DateTime, Array<DateTime>>(new ArraySymbol<DateTime>(dates, elementsType), SymbolType.Date);
            default:
                return null;
        }
    }

    private static List<string> ProcessList(string listString, char openingChar,
        char closingChar, char separator) {
        int startIndex = listString.IndexOf(openingChar);
        int endIndex = listString.LastIndexOf(closingChar);
        if (startIndex == -1 || endIndex == -1 || startIndex > endIndex) {
            Debug.LogError($"ScriptParser.ParseList(\"{listString}\") : syntax error.");
            return null;
        }
        string current = "";
        List<string> list = new List<string>();
        int openedParentheses = 0, closedParentheses = 0;
        for (int i = startIndex + 1; i < endIndex; i++) {
            char c = listString[i];
            if (c == separator && closedParentheses == openedParentheses) {
                list.Add(current.Trim());
                current = "";
                continue;
            }
            current += c;
            if (c == '(') ++openedParentheses;
            else if (c == ')') ++closedParentheses;
        }
        current = current.Trim();
        if (current != "") list.Add(current);
        return list;
    }

    private static IExpression ParseToken(string expression,
        ParserContext context) {
        expression = expression.Trim();
        if (expression == "") return null;
        // local variable ?
        LocalVariable localVariable = context.LocalVariables.Find(
            lv => lv.Name == expression);
        if (localVariable != null) {
            switch (localVariable.Type) {
                case SymbolType.Void: return new LocalVariableExpression<Void>(localVariable);
                case SymbolType.Boolean: return new LocalVariableExpression<bool>(localVariable);
                case SymbolType.Integer: return new LocalVariableExpression<int>(localVariable);
                case SymbolType.Float: return new LocalVariableExpression<float>(localVariable);
                case SymbolType.Id: return new LocalVariableExpression<Id>(localVariable);
                case SymbolType.String: return new LocalVariableExpression<string>(localVariable);
                case SymbolType.Date: return new LocalVariableExpression<DateTime>(localVariable);
                case SymbolType.Array:
                    switch (localVariable.ArrayType) {
                        case SymbolType.Void: return new LocalVariableExpression<Array<Void>>(localVariable);
                        case SymbolType.Boolean: return new LocalVariableExpression<Array<bool>>(localVariable);
                        case SymbolType.Integer: return new LocalVariableExpression<Array<int>>(localVariable);
                        case SymbolType.Float: return new LocalVariableExpression<Array<float>>(localVariable);
                        case SymbolType.Id: return new LocalVariableExpression<Array<Id>>(localVariable);
                        case SymbolType.String: return new LocalVariableExpression<Array<string>>(localVariable);
                        case SymbolType.Date: return new LocalVariableExpression<Array<DateTime>>(localVariable);
                        default: throw new ArgumentOutOfRangeException();
                    }
                default: throw new ArgumentOutOfRangeException();
            }
        }
        // global variable
        if (expression.StartsWith("$")) {
            string variableName = expression.Substring(1);
            GlobalVariable globalVariable = context.GlobalVariables.Find(
                gv => gv.Name == variableName);
            if (globalVariable == null) {
                Debug.LogError($"Parser.ParseToken(\"{expression}\") : unknown Global Variable \"${variableName}\".");
                return null;
            }
            switch (globalVariable.Type) {
                case SymbolType.Void: return new GlobalVariableExpression<Void>(globalVariable);
                case SymbolType.Boolean: return new GlobalVariableExpression<bool>(globalVariable);
                case SymbolType.Integer: return new GlobalVariableExpression<int>(globalVariable);
                case SymbolType.Float: return new GlobalVariableExpression<float>(globalVariable);
                case SymbolType.Id: return new GlobalVariableExpression<Id>(globalVariable);
                case SymbolType.String: return new GlobalVariableExpression<string>(globalVariable);
                case SymbolType.Date: return new GlobalVariableExpression<DateTime>(globalVariable);
                case SymbolType.Array:
                    switch (globalVariable.ArrayType) {
                        case SymbolType.Void: return new GlobalVariableExpression<Array<Void>>(globalVariable);
                        case SymbolType.Boolean: return new GlobalVariableExpression<Array<bool>>(globalVariable);
                        case SymbolType.Integer: return new GlobalVariableExpression<Array<int>>(globalVariable);
                        case SymbolType.Float: return new GlobalVariableExpression<Array<float>>(globalVariable);
                        case SymbolType.Id: return new GlobalVariableExpression<Array<Id>>(globalVariable);
                        case SymbolType.String: return new GlobalVariableExpression<Array<string>>(globalVariable);
                        case SymbolType.Date: return new GlobalVariableExpression<Array<DateTime>>(globalVariable);
                        default: throw new ArgumentOutOfRangeException();
                    }
                default: throw new ArgumentOutOfRangeException();
            }
        }
        // universal constant
        switch (expression) {
            case "Math.PI": return new SymbolExpression<float>(new FloatSymbol(Mathf.PI));
        }
        // literal constant
        ISymbol constant = ParseLiteral(expression, context);
        if (constant == null) {
            Debug.LogError($"Parser.ParseToken(\"{expression}\") : cannot parse as literal.");
            return null;
        }
        switch (constant.Type()) {
            case SymbolType.Boolean: return new SymbolExpression<bool>(constant as Symbol<bool>);
            case SymbolType.Integer: return new SymbolExpression<int>(constant as Symbol<int>);
            case SymbolType.Float: return new SymbolExpression<float>(constant as Symbol<float>);
            case SymbolType.Id: return new SymbolExpression<Id>(constant as Symbol<Id>);
            case SymbolType.String: return new SymbolExpression<string>(constant as Symbol<string>);
            case SymbolType.Date: return new SymbolExpression<DateTime>(constant as Symbol<DateTime>);
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private static ISymbol ParseLiteral(string token, ParserContext context) {
        token = token.Trim();
        if (token == "") return null;
        char firstChar = token[0];
        // boolean literal
        if (token == "true") return new BooleanSymbol(true);
        if (token == "false") return new BooleanSymbol(false);
        // ID literal
        if (firstChar == '@') {
            if (!context.Grammar.ValidateIdLiteral(token)) {
                Debug.LogError($"Parser.ParseLiteral(\"{token}\") : invalid ID literal.");
                return null;
            }
            return new IdSymbol(token.Substring(1));
        }
        // string literal
        if (firstChar == '\'' || firstChar == '\"') {
            Assert.IsTrue(token.EndsWith($"{firstChar}"));
            string literal = token.Substring(1, token.Length - 2);
            return new StringSymbol(literal);
        }
        // date literal
        if (token.Contains("/")) {
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
