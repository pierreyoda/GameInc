using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;

namespace Script {

[Serializable]
public class TypedExecutable<T> {
    [SerializeField] private readonly Executable executable;

    private TypedExecutable(Executable executable) {
        this.executable = executable;
    }

    public static TypedExecutable<T> FromScript(string script,
        ParserContext parserContext) {
        if (typeof(T) == typeof(Void)) {
            Debug.LogError("TypedExecutable<T = Void> : Void type not allowed.");
            return null;
        }
        Executable executable = Executable.FromScript(script, parserContext);
        if (executable == null) {
            Debug.LogError($"TypedExecutable<T = {typeof(T)}>.FromScript : " +
                           $"parsing error in  :\n{script}");
            return null;
        }
        if (!Executable.TypeCompatibility<T>(executable.Type)) {
            Debug.LogError($"TypedExecutable<T = {typeof(T)}>.FromScript : T " +
                           $"is not compatible with {executable.Type}.");
            return null;
        }
        return new TypedExecutable<T>(executable);
    }

    public bool Compute(IScriptContext context, out T result) {
        ISymbol lastResult = executable.Execute(context);
        if (lastResult == null) {
            Debug.LogError($"TypedExecutable<T = {typeof(T)}>.Compute : execution error.");
            result = default(T);
            return false;
        }
        Assert.IsTrue(lastResult.Type() == executable.Type);
        Symbol<T> lastResultTyped = lastResult as Symbol<T>;
        Assert.IsNotNull(lastResultTyped);
        result = lastResultTyped.Value;
        return true;
    }
}

[Serializable]
public class Executable {
    [SerializeField] private SymbolType type;
    public SymbolType Type => type;

    [SerializeField] private List<IExpression> expressions;

    private Executable(SymbolType type, List<IExpression> expressions) {
        this.type = type;
        this.expressions = expressions;
    }

    /// <summary>
    /// Try to parse the given text script as an Executable sequence of Expressions.
    ///
    /// The return Type is determined by the last Expression : if it ends with ';',
    /// then it is considered as evaluating to Void, otherwise the Type will be
    /// inferred from this Expression's script.
    /// </summary>
    public static Executable FromScript(string script,
        ParserContext parserContext) {
        // Expressions split
        bool inStringLiteral = false;
        string currentExpression = "";
        List<string> expressionsString = new List<string>();
        for (int i = 0; i < script.Length; i++) {
            char c = script[i];
            currentExpression += c;
            if (c == '\'') {
                if (!inStringLiteral) inStringLiteral = true;
                else if (i > 0 && script[i - 1] != '\\') inStringLiteral = false;
            } else if (c == ';' && !inStringLiteral) {
                expressionsString.Add(currentExpression.TrimStart());
                currentExpression = "";
            }
        }
        currentExpression = currentExpression.Trim();
        if (currentExpression != "") expressionsString.Add(currentExpression);

        // Expressions parsing
        SymbolType returnType;
        List<IExpression> expressions = ParseExpressionSequence(
            expressionsString.ToArray(), parserContext, out returnType);
        if (returnType == SymbolType.Invalid) {
            Debug.LogError( "Executable.FromScript(...) : could not determing Type " +
                           $"from last expression \"{expressions.Last()}\".");
            return null;
        }

        return new Executable(returnType, expressions);
    }

    private static List<IExpression> ParseExpressionSequence(
        string[] expressionsString, ParserContext context,
        out SymbolType returnType) {
        returnType = SymbolType.Invalid;
        List<IExpression> expressions = new List<IExpression>();
        if (expressionsString.Length == 0) {
            returnType = SymbolType.Void;
            expressions.Add(new SymbolExpression<Void>(new VoidSymbol()));
            return expressions;
        }
        for (int i = 0; i < expressionsString.Length; i++) {
            string expressionString = expressionsString[i];
            IExpression expression = Parser.ParseExpression(expressionString,
                context);
            if (expression == null) {
                Debug.LogError( "Executable.ParseExpressionSequence(...) : parsing error at " +
                                $"expression n°{i+1} \"{expressionString}\".");
                return null;
            }
            if (i == expressionsString.Length - 1) { // last expression determines return Type
                if (expressionString.EndsWith(";")) returnType = SymbolType.Void;
                else returnType = expression.Type();
            }
            expressions.Add(expression);
        }
        return expressions;
    }

    public ISymbol Execute(IScriptContext context) {
        ISymbol result = new VoidSymbol();
        for (int i = 0; i < expressions.Count; i++) {
            IExpression expression = expressions[i];
            result = expression.EvaluateAsISymbol(context);
            if (result == null) {
                Debug.LogError( "Executable : error while evaluating expression " +
                               $"n°{i+1} \"{expression.Script()}\".");
                return null;
            }
        }
        return result;
    }

    public bool ExecuteExpecting<T>(IScriptContext context, out T result) {
        result = default(T);
        if (type == SymbolType.Void) {
            Debug.LogError("Executable.ExecuteExpecting : cannot expect void. " +
                           "Use Execute() instead.");
            return false;
        }
        ISymbol lastResult = Execute(context);
        if (lastResult == null || lastResult.Type() != type) {
            Debug.LogError("Executable.ExecuteExpecting : execution error.");
            return false;
        }
        if (!TypeCompatibility<T>(lastResult.Type())) {
            Debug.LogError($"Executable.ExecuteExpecting : result type {typeof(T)} " +
                           $"imcompatible with executable type {type}.");
            return false;
        }
        Symbol<T> lastResultTyped = lastResult as Symbol<T>;
        Assert.IsNotNull(lastResultTyped);
        result = lastResultTyped.Value;
        return true;
    }

    public static bool TypeCompatibility<T>(SymbolType type) {
        // TODO : differenciate ID and String
        return typeof(T) == typeof(bool) && type == SymbolType.Boolean ||
            typeof(T) == typeof(int) && type == SymbolType.Integer ||
            typeof(T) == typeof(float) && type == SymbolType.Float ||
            typeof(T) == typeof(string) && (type == SymbolType.Id || type == SymbolType.String) ||
            typeof(T) == typeof(DateTime) && type == SymbolType.Date;
    }
}

}
