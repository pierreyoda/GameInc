using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Script {

public interface IExpression {
    string Script();
    SymbolType Type();
    ISymbol EvaluateAsISymbol(IScriptContext context);
}

[Serializable]
public abstract class Expression<T> : IExpression {
    [SerializeField] private SymbolType type;
    public SymbolType Type() => type;

    [SerializeField] private string script;
    public string Script() => script;

    protected Expression(string script, SymbolType type) {
        this.type = type;
        this.script = script;
    }

    public abstract Symbol<T> Evaluate(IScriptContext c);
    public ISymbol EvaluateAsISymbol(IScriptContext c) {
        switch (type) {
            case SymbolType.Boolean: return Evaluate(c) as Symbol<bool>;
            case SymbolType.Integer: return Evaluate(c) as Symbol<int>;
            case SymbolType.Float: return Evaluate(c) as Symbol<float>;
            case SymbolType.Id:
            case SymbolType.String: return Evaluate(c) as Symbol<string>;
            case SymbolType.Date: return Evaluate(c) as Symbol<DateTime>;
            default: return null;
        }
    }
}

[Serializable]
public class LocalVariableExpression<T> : Expression<T> {
    [SerializeField] private LocalVariable localVariable;

    public LocalVariableExpression(LocalVariable localVariable)
        : base($"@{localVariable.Name}", localVariable.Type) {
        this.localVariable = localVariable;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        return localVariable.Value as Symbol<T>;
    }
}

[Serializable]
public class GlobalVariableExpression<T> : Expression<T> {
    [SerializeField] private GlobalVariable globalVariable;

    public GlobalVariableExpression(GlobalVariable globalVariable)
        : base($"${globalVariable.Name}", globalVariable.Type) {
        this.globalVariable = globalVariable;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        return globalVariable.FromContext(c) as Symbol<T>;
    }
}

[Serializable]
public class SymbolExpression<T> : Expression<T> {
    [SerializeField] private Symbol<T> symbol;

    public SymbolExpression(Symbol<T> symbol)
        : base(symbol.Expression, symbol.Type()) {
        this.symbol = symbol;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        return symbol;
    }
}

[Serializable]
public class OperationExpression<T>: Expression<T> {
    [SerializeField] private OperatorType type;
    [SerializeField] private Expression<T> left;
    [SerializeField] private Expression<T> right;

    public OperationExpression(OperatorType type, Expression<T> left,
        Expression<T> right) : base($"{type} [{left.Script()} {right.Script()}]",
            left.Type()) {
        this.type = type;
        this.left = left;
        this.right = right;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        return left.Evaluate(c).Operation(right.Evaluate(c), type);
    }
}
[Serializable]
public class AssignmentExpression<T>: Expression<T> {
    [SerializeField] private AssignmentType type;
    [SerializeField] private Expression<T> variable;
    [SerializeField] private Expression<T> expression;
    [SerializeField] private bool global;

    public AssignmentExpression(AssignmentType type, Expression<T> variable,
        Expression<T> expression)
        : base($"{variable.Script()} {type} {expression.Script()}", variable.Type()) {
        string name = variable.Script();
        Assert.IsTrue(name.StartsWith("@") || name.StartsWith("$"));
        this.type = type;
        this.variable = variable;
        this.expression = expression;
        global = name.StartsWith("$");
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        Symbol<T> value = expression.Evaluate(c);
        if (value == null) {
            Debug.LogError($"Script Error in AssignmentExpression(type = {type}) : right operand evaluation error (\"{expression.Script()}\").");
            return null;
        }
        Symbol<T> assigned = variable.Evaluate(c);
        if (assigned == null) {
            Debug.LogError($"Script Error in AssignmentExpression(type = {type}) : right operand evaluation error (\"{variable.Script()}\").");
            return null;
        }
        ISymbol result = assigned.Assignment(value, type);
        dynamic resultTyped;
        switch (result.Type()) {
            case SymbolType.Boolean: resultTyped = new BooleanSymbol(Convert.ToBoolean(((Symbol<T>) result).Value)); break;
            case SymbolType.Integer: resultTyped = new IntegerSymbol(Convert.ToInt32(((Symbol<T>) result).Value)); break;
            case SymbolType.Float: resultTyped = new FloatSymbol(Convert.ToSingle(((Symbol<T>) result).Value)) as Symbol<T>; break;
            case SymbolType.Id: resultTyped = new IdSymbol(Convert.ToString(((Symbol<T>) result).Value)) as Symbol<T>; break;
            case SymbolType.String: resultTyped = new StringSymbol(Convert.ToString(((Symbol<T>) result).Value)) as Symbol<T>; break;
            case SymbolType.Date: resultTyped = new DateSymbol(Convert.ToDateTime(((Symbol<T>) result).Value)) as Symbol<T>; break;
            default: throw new ArgumentOutOfRangeException();
        }
        return global ? EvaluateGlobal(c, resultTyped as Symbol<T>) :
            EvaluateLocal(c, resultTyped as Symbol<T>);
    }

    private Symbol<T> EvaluateLocal(IScriptContext c, Symbol<T> value) {
        string variableName = variable.Script().Substring(1);
        LocalVariable localVariable = c.LocalVariables().Find(l => l.Name == variableName);
        if (localVariable == null) {
            Debug.LogError($"Script Error in AssignmentExpression(type = {type}) : no such local variable as (\"{variableName}\").");
            return null;
        }
        localVariable.Value = value;
        return value;
    }

    private Symbol<T> EvaluateGlobal(IScriptContext c, ISymbol value) {
        string variableName = variable.Script().Substring(1);
        LocalVariable localVariable = c.LocalVariables().Find(l => l.Name == variableName);
        if (localVariable == null) {
            Debug.LogError($"Script Error in AssignmentExpression(type = {type}) : no such local variable as \"${variableName}\".");
            return null;
        }
        if (!c.SetGlobalVariable(variableName, value)) {
            Debug.LogError($"Script Error in AssignmentExpression(type = {type}) : cannot assign \"${variableName}\".");
            return null;
        }
        return value as Symbol<T>;
    }
}

[Serializable]
public class ComparisonExpression<T> : Expression<bool> {
    [SerializeField] private ComparisonType type;
    [SerializeField] private Expression<T> left;
    [SerializeField] private Expression<T> right;

    public ComparisonExpression(ComparisonType type,
        Expression<T> left, Expression<T> right)
        : base($"{left.Script()} {type} {right.Script()}",
            SymbolType.Boolean) {
        this.type = type;
        this.left = left;
        this.right = right;
    }

    public override Symbol<bool> Evaluate(IScriptContext c) {
        Symbol<T> leftValue = left.Evaluate(c);
        Symbol<T> rightValue = right.Evaluate(c);
        return leftValue.CompareTo(rightValue, type);
    }
}

    [Serializable]
    public class FunctionExpression<T> : Expression<T> {
        [SerializeField] private Function<T> metadata;
        [SerializeField] private IExpression[] parameters;

        public FunctionExpression(Function<T> metadata, IExpression[] parameters) :
            base($"{metadata.Name()}({string.Join(", ", parameters.Select(p => p.Script()))})",
                metadata.ReturnType()) {
            Assert.IsTrue(metadata.Parameters().Length == parameters.Length);
            this.metadata = metadata;
            this.parameters = parameters;
        }

        public override Symbol<T> Evaluate(IScriptContext c) {
            List<ISymbol> symbols = new List<ISymbol>();
            for (int i = 0; i < parameters.Length; i++) {
                SymbolType type = metadata.Parameters()[i];
                IExpression parameter = parameters[i];
                if (parameter.Type() != type) {
                    Debug.LogError($"Script Error : Function Call \"{metadata.Name()}\" : parameter n°{i+1} must be of type {type} (\"{parameter.Script()}\").");
                    return null;
                }
                ISymbol symbol;
                switch (type) {
                    case SymbolType.Boolean: symbol = (parameter as Expression<bool>).Evaluate(c); break;
                    case SymbolType.Integer: symbol = (parameter as Expression<int>).Evaluate(c); break;
                    case SymbolType.Float: symbol = (parameter as Expression<float>).Evaluate(c); break;
                    case SymbolType.Id:
                    case SymbolType.String: symbol = (parameter as Expression<string>).Evaluate(c); break;
                    case SymbolType.Date: symbol = (parameter as Expression<DateTime>).Evaluate(c); break;
                    default: symbol = null; break;
                }
                if (symbol == null) {
                    Debug.LogError($"Script Error : Function Call \"{metadata.Name()}\" : error while evaluating parameter n°{i+1} (\"{parameter.Script()}\").");
                    return null;
                }
                symbols.Add(symbol);
            }
            T result = metadata.Lambda(c, symbols.ToArray());
            switch (metadata.ReturnType()) {
                case SymbolType.Boolean: return new BooleanSymbol(Convert.ToBoolean(result)) as Symbol<T>;
                case SymbolType.Integer: return new IntegerSymbol(Convert.ToInt32(result)) as Symbol<T>;
                case SymbolType.Float: return new FloatSymbol(Convert.ToSingle(result)) as Symbol<T>;
                case SymbolType.Id: return new IdSymbol(Convert.ToString(result)) as Symbol<T>;
                case SymbolType.String: return new StringSymbol(Convert.ToString(result)) as Symbol<T>;
                case SymbolType.Date: return new DateSymbol(Convert.ToDateTime(result)) as Symbol<T>;
                default: return null;
            }
        }
    }

}
