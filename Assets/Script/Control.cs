using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Assertions;

namespace Script {

public interface IIterator {
    string Representation();
    bool Init(IScriptContext context);
    ISymbol Next(IScriptContext c, out bool ended);
}

/// <summary>
/// Describes a range-based loop expression of the type :
/// for i in Range(0, 10) {
///     [... BODY]
/// }
/// </summary>
/// <typeparam name="TR">Type of the Range expression (and thus type of the iterated variable).</typeparam>
[Serializable]
public class RangedLoopExpression<TR> : Expression<Void> {
    [SerializeField] private LocalVariable iterated;
    [SerializeField] private int iterations = 0;
    [SerializeField] private IIterator iterator;
    [SerializeField] private Executable body;

    public RangedLoopExpression(LocalVariable iterated,
        IIterator iterator, Executable body)
        : base($"for {iterated.Name} in {iterator.Representation()} {{ ... }}", SymbolType.Array) {
        Assert.IsTrue(Executable.TypeCompatibility<TR>(iterated.Type, iterated.ArrayType));
        this.iterated = iterated;
        this.iterator = iterator;
        this.body = body;
    }

    public override Symbol<Void> Evaluate(IScriptContext c) {
        if (!iterator.Init(c)) {
            Debug.LogError( "Script : RangedLoopExpression iterator " +
                            "initialization error.");
            return null;
        }

        bool ended;
        int maximumIterations = c.LoopsMaximumIterations();
        do {
            ISymbol newIterated = iterator.Next(c, out ended);
            if (newIterated == null) {
                Debug.LogError( "Script : RangedLoopExpression range evaluation error " +
                               $"at iteration {iterations}.");
                return null;
            }
            if (++iterations >= maximumIterations) {
                Debug.LogError( "Script : RangedLoopExpression exceeded the " +
                               $"maximum iterations limit ({maximumIterations}).");
                return null;
            }
            iterated.Value = newIterated;
            if (body.Execute(c) == null) {
                Debug.LogError( "Script : RangedLoopExpression body evalution error " +
                                $"at iteration {iterations}.");
                return null;
            }
        } while (!ended);
        return new VoidSymbol();
    }
}

[Serializable]
public class IntegerRangeExpression<T> : Expression<T>, IIterator {
    [SerializeField] private int index = -1;
    [SerializeField] private int step = 1;
    [SerializeField] private Expression<int> start;
    [SerializeField] private Expression<int> end;
    [SerializeField] private int currentValue;
    [SerializeField] private int endValue;

    public IntegerRangeExpression(Expression<int> start, Expression<int> end)
        : base($"Range({start.Script()}, {end.Script()})", SymbolType.Integer) {
        this.start = start;
        this.end = end;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        Debug.LogError("Script : IntegerRangeExpression should not be called directly ; " +
                       "use NextValue() instead!");
        return null;
    }

    public string Representation() => Script();

    public bool Init(IScriptContext c) {
        Symbol<int> startSymbol = start.Evaluate(c);
        if (startSymbol == null) {
            Debug.LogError( "Script : IntegerRangeExpression.Init : evaluation error " +
                            $"for starting value \"{start.Script()}\".");
            return false;
        }
        Symbol<int> endSymbol = end.Evaluate(c);
        if (endSymbol == null) {
            Debug.LogError( "Script : IntegerRangeExpression.Init : evaluation error " +
                            $"for ending value \"{end.Script()}\".");
            return false;
        }

        currentValue = startSymbol.Value;
        endValue = endSymbol.Value;
        int delta = endValue - currentValue;
        if (step > 0 && delta < 0 || step < 0 && delta > 0) {
            Debug.LogError( "Script : IntegerRangeExpression.Init : logic error " +
                           $"with the range values (start: {currentValue}, " +
                           $"end: {endValue}, step: {step}).");
            return false;
        }
        return true;
    }

    public ISymbol Next(IScriptContext c, out bool ended) {
        if (++index > 0) currentValue += step;
        ended = currentValue == endValue;
        return new IntegerSymbol(currentValue);
    }
}

public static class Control {
    public static IExpression ParseRangedLoopExpression(string[] tokens,
        ParserContext context) {
        if (tokens.Length < 10) {
            Debug.LogError( "Control.ParseRangedLoopExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : syntax error.");
            return null;
        }
        if (tokens[0] != context.Grammar.RangedLoopDeclarator ||
            tokens[2] != "in") {
            Debug.LogError( "Control.ParseRangedLoopExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : loop syntax error.");
            return null;
        }

        // Iteration variable & Range
        string iteratedName = tokens[1];
        if (context.LocalVariables.Find(lv => lv.Name == iteratedName) != null) {
            Debug.LogError( "Control.ParseRangedLoopExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : variable " +
                            $"\"{iteratedName}\" already exists.");
            return null;
        }
        int declarationEndIndex = Array.IndexOf(tokens, "{", 7);
        if (declarationEndIndex < 0 || declarationEndIndex >= tokens.Length) {
            Debug.LogError( "Control.ParseRangedLoopExpression(\"" +
                           $"{string.Join(" ", tokens)}\") : no loop body declared.");
            return null;
        }
        ISymbol iterated;
        IIterator iterator = ParseRangeExpression(
            tokens.Skip(3).Take(declarationEndIndex - 3).ToArray(),
            context, out iterated);
        if (iterator == null) {
            Debug.LogError( "Control.ParseRangedLoopExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : range parsing error.");
            return null;
        }
        LocalVariable iteratedVariable = new LocalVariable(iteratedName,
            iterated, true);
        iteratedVariable.Reference();
        context.LocalVariables.Add(iteratedVariable);

        // Body
        Executable body = Executable.FromScript(
            string.Join(" ", tokens.Skip(declarationEndIndex).ToArray()), // TODO avoid this
            context);
        if (body == null) {
            Debug.LogError( "Control.ParseRangedLoopExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : body parsing error.");
            return null;
        }

        // Expression
        IExpression loopExpression;
        switch (iterated.Type()) {
            case SymbolType.Boolean:
                loopExpression = new RangedLoopExpression<bool>(iteratedVariable,
                    iterator, body);
                break;
            case SymbolType.Integer:
                loopExpression = new RangedLoopExpression<int>(iteratedVariable,
                    iterator, body);
                break;
            case SymbolType.Float:
                loopExpression = new RangedLoopExpression<float>(iteratedVariable,
                    iterator, body);
                break;
            case SymbolType.Id:
                loopExpression = new RangedLoopExpression<Id>(iteratedVariable,
                    iterator, body);
                break;
            case SymbolType.String:
                loopExpression = new RangedLoopExpression<string>(iteratedVariable,
                    iterator, body);
                break;
            case SymbolType.Date:
                loopExpression = new RangedLoopExpression<DateTime>(iteratedVariable,
                    iterator, body);
                break;
            case SymbolType.Array:
                switch (iterated.ArrayType()) {
                    case SymbolType.Void:
                        loopExpression = new RangedLoopExpression<Array<Void>>(iteratedVariable,
                            iterator, body);
                        break;
                    case SymbolType.Boolean:
                        loopExpression = new RangedLoopExpression<Array<bool>>(iteratedVariable,
                            iterator, body);
                        break;
                    case SymbolType.Integer:
                        loopExpression = new RangedLoopExpression<Array<int>>(iteratedVariable,
                            iterator, body);
                        break;
                    case SymbolType.Float:
                        loopExpression = new RangedLoopExpression<Array<float>>(iteratedVariable,
                            iterator, body);
                        break;
                    case SymbolType.Id:
                        loopExpression = new RangedLoopExpression<Array<Id>>(iteratedVariable,
                            iterator, body);
                        break;
                    case SymbolType.String:
                        loopExpression = new RangedLoopExpression<Array<string>>(iteratedVariable,
                            iterator, body);
                        break;
                    case SymbolType.Date:
                        loopExpression = new RangedLoopExpression<Array<DateTime>>(iteratedVariable,
                            iterator, body);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return loopExpression;
    }

    /**
     * Parse the expression describing a Range symbol.
     * For instance : Range(0, 10) describes the integer-based
     * range between 0 and 9 (included).
     */
    private static IIterator ParseRangeExpression(string[] tokens,
        ParserContext context, out ISymbol iterationSymbol) {
        iterationSymbol = null;
        if (tokens.Length < 5 || tokens[0] != "Range" || tokens[1] != "(") {
            Debug.LogError( "Control.ParseRangeExpression(\"" +
                           $"{string.Join(" ", tokens)}\") : syntax error.");
            return null;
        }

        string expression = string.Join(" ", tokens);
        List<string> parametersString = Parser.ProcessList(expression, '(', ')', ',');
        if (parametersString.Count != 2) {
            Debug.LogError( "Control.ParseRangeExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : wrong arity " +
                            $"({parametersString.Count} instead of 2).");
            return null;
        }

        IExpression start = Parser.ParseExpression(parametersString[0], context);
        if (start == null) {
            Debug.LogError( "Control.ParseRangeExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : parsing error for " +
                            $"range start \"{parametersString[0]}\".");
            return null;
        }
        if (start.Type() != SymbolType.Integer) {
            Debug.LogError( "Control.ParseRangeExpression(\"" +
                           $"{string.Join(" ", tokens)}\") : unsupported type.");
            return null;
        }

        IExpression end = Parser.ParseExpression(parametersString[1], context);
        if (end == null) {
            Debug.LogError( "Control.ParseRangeExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : parsing error for " +
                            $"range end \"{parametersString[1]}\".");
            return null;
        }
        if (end.Type() != start.Type() || end.ArrayType() != start.ArrayType()) {
            Debug.LogError( "Control.ParseRangeExpression(\"" +
                            $"{string.Join(" ", tokens)}\") : incompatible types.");
            return null;
        }

        return null;
    }
}

}