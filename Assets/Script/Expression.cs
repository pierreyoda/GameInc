using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Script {

public interface IExpression {
    string Script();
    SymbolType Type();
    SymbolType ArrayType();
    ISymbol EvaluateAsISymbol(IScriptContext context);
}

[Serializable]
public abstract class Expression<T> : IExpression {
    [SerializeField] private SymbolType type;
    public SymbolType Type() => type;

    [SerializeField] protected SymbolType arrayType = SymbolType.Invalid;
    public SymbolType ArrayType() => arrayType;

    [SerializeField] private string script;
    public string Script() => script;

    protected Expression(string script, SymbolType type) {
        this.type = type;
        this.script = script;
    }

    public abstract Symbol<T> Evaluate(IScriptContext c);
    public ISymbol EvaluateAsISymbol(IScriptContext c) {
        ISymbol result = Evaluate(c);
        if (result == null) {
            Debug.LogError($"Script : Expression.EvaluateAsISymbol : evaluation error in \"{script}\".");
            return null;
        }
        return result;
    }
}

public interface IVariableExpression {
    string Representation();
    SymbolType VariableType();
    ISymbol Assign(IScriptContext context, AssignmentType type, ISymbol right);
}

[Serializable]
public class LocalVariableExpression<T> : Expression<T>, IVariableExpression {
    [SerializeField] private LocalVariable localVariable;

    public LocalVariableExpression(LocalVariable localVariable)
        : base($"{localVariable.Name}", localVariable.Type) {
        Assert.IsNotNull(localVariable);
        this.localVariable = localVariable;
        if (localVariable.Type == SymbolType.Array)
            arrayType = localVariable.ArrayType;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        return localVariable.Value as Symbol<T>;
    }

    public string Representation() => $"{localVariable.Name}";
    public SymbolType VariableType() => localVariable.Type;

    public ISymbol Assign(IScriptContext context, AssignmentType assignmentType,
        ISymbol right) {
        if (right.Type() != localVariable.Type) {
            Debug.LogError($"LocalVariableExpression({localVariable.Name}).Assign : " +
                           $"type mismatch ({right.Type()} instead of {localVariable.Type}");
            return null;
        }
        Symbol<T> rightValue = right as Symbol<T>;
        Assert.IsNotNull(rightValue);
        //Debug.LogWarning($"===> lv assignment : {localVariable.Type} = {right.ValueString()} = {rightValue.Value}");
        Symbol<T> localValue = localVariable.Value as Symbol<T>;
        Assert.IsNotNull(localValue);
        Symbol<T> assignmentResult = localValue.Assignment(rightValue,
            assignmentType);
        if (assignmentResult == null) {
            Debug.LogError($"LocalVariableExpression({localVariable.Name}).Assign : " +
                            "error while evaluating assignment.");
            return null;
        }
        localVariable.Value = assignmentResult;
        return assignmentResult;
    }
}

[Serializable]
public class GlobalVariableExpression<T> : Expression<T>, IVariableExpression {
    [SerializeField] private GlobalVariable globalVariable;

    public GlobalVariableExpression(GlobalVariable globalVariable)
        : base($"${globalVariable.Name}", globalVariable.Type) {
        this.globalVariable = globalVariable;
        if (globalVariable.Type == SymbolType.Array)
            arrayType = globalVariable.ArrayType;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        return globalVariable.FromContext(c) as Symbol<T>;
    }

    public string Representation() => $"${globalVariable.Name}";
    public SymbolType VariableType() => globalVariable.Type;

    public ISymbol Assign(IScriptContext context, AssignmentType assignmentType,
        ISymbol right) {
        if (right.Type() != globalVariable.Type) {
            Debug.LogError($"GlobalVariableExpression(${globalVariable.Name}).Assign : " +
                           $"type mismatch ({right.Type()} instead of {globalVariable.Type}");
            return null;
        }
        Symbol<T> rightValue = right as Symbol<T>;
        Assert.IsNotNull(rightValue);
        globalVariable.Value = globalVariable.FromContext(context); // refresh value
        Symbol<T> globalValue = globalVariable.Value as Symbol<T>;
        Assert.IsNotNull(globalValue);
        Symbol<T> assignmentResult = globalValue.Assignment(rightValue,
            assignmentType);
        return context.SetGlobalVariable(globalVariable.Name, assignmentResult) ?
            assignmentResult : null;
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
public class AssignmentExpression<TR, T>: Expression<TR> {
    [SerializeField] private AssignmentType type;
    [SerializeField] private IVariableExpression variable;
    [SerializeField] private Expression<T> expression;
    [SerializeField] private bool returnsValue;

    public AssignmentExpression(AssignmentType type, IVariableExpression variable,
        Expression<T> expression, bool returnsValue)
        : base($"{variable.Representation()} {type} {expression.Script()}", variable.VariableType()) {
        this.type = type;
        this.variable = variable;
        this.expression = expression;
        this.returnsValue = returnsValue;
        // type checking
        Assert.IsTrue(variable.VariableType() == expression.Type());
        if (returnsValue) Assert.IsTrue(typeof(TR) == typeof(T));
        else Assert.IsTrue(typeof(TR) == typeof(Void));
    }

    public override Symbol<TR> Evaluate(IScriptContext c) {
        ISymbol value = expression.Evaluate(c);
        if (value == null) {
            Debug.LogError($"Script : AssignmentExpression(type = {type}) : right " +
                           $"operand evaluation error (\"{expression.Script()}\").");
            return null;
        }
        ISymbol resultUntyped = variable.Assign(c, type, value);
        if (resultUntyped == null) {
            Debug.LogError($"Script : AssignmentExpression(type = {type}) : error while assigning.");
            return null;
        }
        if (returnsValue) {
            Symbol<TR> resultTyped = resultUntyped as Symbol<TR>;
            Assert.IsNotNull(resultTyped);
            return resultTyped;
        }
        return new VoidSymbol() as Symbol<TR>;
    }
}

[Serializable]
public class ComparisonExpression<T> : Expression<bool> {
    [SerializeField] private OperatorType type;
    [SerializeField] private Expression<T> left;
    [SerializeField] private Expression<T> right;

    public ComparisonExpression(OperatorType type,
        Expression<T> left, Expression<T> right)
        : base($"{left.Script()} {type} {right.Script()}",
            SymbolType.Boolean) {
        Assert.IsTrue(Operations.IsComparisonOperator(type));
        this.type = type;
        this.left = left;
        this.right = right;
    }

    public override Symbol<bool> Evaluate(IScriptContext c) {
        Symbol<T> leftValue = left.Evaluate(c);
        Symbol<T> rightValue = right.Evaluate(c);
        return leftValue.CompareTo(rightValue, type, c);
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
            if (parameter.Type() != type && type != SymbolType.Void) { // void : any type
                Debug.LogError($"Script Error : Function Call \"{metadata.Name()}\" : " +
                               $"parameter n°{i+1} must be of type {type} (\"{parameter.Script()}\").");
                return null;
            }
            ISymbol symbol = parameter.EvaluateAsISymbol(c);
            if (symbol == null) {
                Debug.LogError($"Script Error : Function Call \"{metadata.Name()}\" : " +
                               $"error while evaluating parameter n°{i+1} (\"{parameter.Script()}\").");
                return null;
            }
            symbols.Add(symbol);
        }
        T resultValue = metadata.Lambda(c, symbols.ToArray());
        Symbol<T> result = Symbol<T>.SymbolFromValue(resultValue,
            metadata.ReturnType());
        if (result == null) {
            Debug.LogError($"Script Error : Function Call \"{metadata.Name()}\" : type error.");
            return null;
        }
        return result;
    }
}

[Serializable]
public class ArrayExpression<T> : Expression<T> {
    [SerializeField] private ArraySymbol<T> array;

    public ArrayExpression(ArraySymbol<T> array, SymbolType arrayType)
        : base($"[{string.Join(", ", array.Elements.Select(e => e.Script()))}]",
            SymbolType.Array) {
        this.array = array;
        this.arrayType = arrayType;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        List<Expression<T>> items = new List<Expression<T>>();
        for (int i = 0; i < array.Elements.Count; i++) {
            Symbol<T> item = array.Elements[i].Evaluate(c);
            if (item == null) {
                Debug.LogError( "Script Error : ArrayExpression.Evaluate : " +
                               $"evaluation error for element n°{i+1}.");
                return null;
            }
            items.Add(new SymbolExpression<T>(item));
        }
        return new ArraySymbol<T>(items, arrayType);
    }
}

}
