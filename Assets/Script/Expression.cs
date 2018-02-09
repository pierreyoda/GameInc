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
        if (result != null) return result;
        Debug.LogWarning($"{typeof(T)} : {script}");
        Debug.LogError($"Script : Expression.EvaluateAsISymbol : evaluation error in \"{script}\".");
        return null;
    }
}

public interface IVariableExpression {
    string Representation();
    SymbolType VariableType();
    ISymbol Assign(IScriptContext context, AssignmentType type, ISymbol right);
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
        Symbol<T> leftValue = left.Evaluate(c);
        if (leftValue == null) {
            Debug.LogError($"Script : OperatorExpression(type = {type}) : left " +
                           $"operand evaluation error (\"{left.Script()}\").");
            return null;
        }
        Symbol<T> rightValue = right.Evaluate(c);
        if (rightValue == null) {
            Debug.LogError($"Script : OperatorExpression(type = {type}) : right " +
                           $"operand evaluation error (\"{left.Script()}\").");
            return null;
        }
        return leftValue.Operation(rightValue, type);
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
        if (!returnsValue) return new VoidSymbol() as Symbol<TR>;
        Symbol<TR> resultTyped = resultUntyped as Symbol<TR>;
        Assert.IsNotNull(resultTyped);
        return resultTyped;
    }
}

[Serializable]
public class ArrayAssignmentExpression<T>: Expression<Array<T>> { // TODO : refactor AssignmentExpression to remove this if possible
    [SerializeField] private AssignmentType type;
    [SerializeField] private IVariableExpression variable;
    [SerializeField] private ArrayExpression<T, Array<T>> expression;

    public ArrayAssignmentExpression(AssignmentType type, IVariableExpression variable,
        ArrayExpression<T, Array<T>> expression)
        : base($"{variable.Representation()} {type} {expression.Script()}", variable.VariableType()) {
        this.type = type;
        this.variable = variable;
        this.expression = expression;
        arrayType = expression.ArrayType();
        // type checking
        Assert.IsTrue(variable.VariableType() == expression.Type());
    }

    public override Symbol<Array<T>> Evaluate(IScriptContext c) {
        ISymbol value = expression.Evaluate(c);
        if (value == null) {
            Debug.LogError($"Script : ArrayAssignmentExpression(type = {type}) : right " +
                           $"operand evaluation error (\"{expression.Script()}\").");
            return null;
        }
        ISymbol resultUntyped = variable.Assign(c, type, value);
        if (resultUntyped == null) {
            Debug.LogError($"Script : ArrayAssignmentExpression(type = {type}) : error while assigning.");
            return null;
        }
        Symbol<Array<T>> resultTyped = resultUntyped as Symbol<Array<T>>;
        Assert.IsNotNull(resultTyped);
        return resultTyped;
    }
}

[Serializable]
public class VoidArrayAssignmentExpression<T>: Expression<Void> { // TODO : avoid this
    [SerializeField] private ArrayAssignmentExpression<T> arrayAssignment;

    public VoidArrayAssignmentExpression(ArrayAssignmentExpression<T> arrayAssignment)
        : base(arrayAssignment.Script(), SymbolType.Array) {
        this.arrayAssignment = arrayAssignment;
    }

    public override Symbol<Void> Evaluate(IScriptContext c) {
        ISymbol value = arrayAssignment.Evaluate(c);
        if (value != null) return new VoidSymbol();
        Debug.LogError( "Script : VoidArrayAssignmentExpression : Array assignment " +
                        $"evaluation error (\"{arrayAssignment.Script()}\").");
        return null;
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
                Debug.LogError($"Script : Function Call \"{metadata.Name()}\" : " +
                               $"parameter n°{i+1} must be of type {type} (\"{parameter.Script()}\").");
                return null;
            }
            ISymbol symbol = parameter.EvaluateAsISymbol(c);
            if (symbol == null) {
                Debug.LogError($"Script : Function Call \"{metadata.Name()}\" : " +
                               $"error while evaluating parameter n°{i+1} (\"{parameter.Script()}\").");
                return null;
            }
            symbols.Add(symbol);
        }
        Symbol<T> result = metadata.Lambda(c, symbols.ToArray());
        if (result == null) {
            Debug.LogError($"Script : Function Call \"{metadata.Name()}\" : evaluation error.");
            return null;
        }
        if (result.Type() == metadata.ReturnType()) return result;
        Debug.LogError($"Script : Function Call \"{metadata.Name()}\" : type error " +
                       $"({result.Type()} returned instead of the expected {metadata.ReturnType()}).");
        return null;
    }
}

[Serializable]
public class ArrayExpression<TA, T> : Expression<T> {
    [SerializeField] private ArraySymbol<TA> array;

    public ArrayExpression(ArraySymbol<TA> array, SymbolType arrayType)
        : base($"[{string.Join(", ", array.Value.Elements.Select(e => e.Script()))}]",
            SymbolType.Array) {
        this.array = array;
        this.arrayType = arrayType;
    }

    public override Symbol<T> Evaluate(IScriptContext c) {
        List<Expression<TA>> items = new List<Expression<TA>>();
        for (int i = 0; i < array.Value.Elements.Count; i++) {
            Symbol<TA> item = array.Value.Elements[i].Evaluate(c);
            if (item == null) {
                Debug.LogError( "Script : ArrayExpression.Evaluate : " +
                               $"evaluation error for element n°{i+1}.");
                return null;
            }
            items.Add(new SymbolExpression<TA>(item));
        }
        return new ArraySymbol<TA>(items, arrayType) as Symbol<T>;
    }
}

}
