using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Script {

[Serializable]
public class LocalVariable {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private bool mutable;
    public bool Mutable => mutable;

    public SymbolType Type => value.Type();
    public SymbolType ArrayType => value.ArrayType();

    [SerializeField] private ISymbol value;
    public ISymbol Value {
        get { return value; }
        set { this.value = value; }
    }

    [SerializeField] private int references = 0;
    public int References => references;

    public void Reference() { references++; }
    public void Dereference() { references--; }

    public LocalVariable(string name, ISymbol value, bool mutable = false) {
        this.name = name;
        this.mutable = mutable;
        this.value = value;
    }
}

public delegate ISymbol GlobalVariableFromContext(IScriptContext context);

[Serializable]
public class GlobalVariable {
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private SymbolType type;
    public SymbolType Type => type;

    [SerializeField] private SymbolType arrayType;
    public SymbolType ArrayType => arrayType;

    [SerializeField] private GlobalVariableFromContext fromContext;
    public GlobalVariableFromContext FromContext => fromContext;

    public GlobalVariable(string name, GlobalVariableFromContext fromContext,
        SymbolType type, SymbolType arrayType = SymbolType.Invalid) {
        this.name = name;
        this.type = type;
        this.arrayType = arrayType;
        this.fromContext = fromContext;
    }
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
                           $"type mismatch ({right.Type()} instead of {localVariable.Type}.");
            return null;
        }
        Symbol<T> rightValue = right as Symbol<T>;
        Assert.IsNotNull(rightValue);
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
        ISymbol valueUntyped = globalVariable.FromContext(context);
        Symbol<T> value = valueUntyped as Symbol<T>;
        Assert.IsNotNull(value);
        Symbol<T> assignmentResult = value.Assignment(rightValue,
            assignmentType);
        return context.SetGlobalVariable(globalVariable.Name, assignmentResult) ?
            assignmentResult : null;
    }
}

}