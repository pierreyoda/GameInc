using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.WSA;

namespace Script {

[Serializable]
public class Void { }

[Serializable]
public enum SymbolType {
    Invalid,
    Void,
    Boolean,
    Integer,
    Float,
    Id,
    String,
    Date,
}

public interface ISymbol {
    SymbolType Type();
    string ValueString();
}

[Serializable]
public abstract class Symbol<T> : ISymbol {
    public static readonly CultureInfo CultureInfo =
        CultureInfo.GetCultureInfo("en-US");
    protected static readonly float FLOAT_EPSILON = 0.000001f;

    [SerializeField] private T value;
    public T Value {
        get { return value; }
        set { this.value = value; }
    }

    [SerializeField] private string expression;
    public string Expression => expression;

    [SerializeField] private SymbolType type;
    public SymbolType Type() { return type; }

    protected Symbol(T value, string expression, SymbolType type) {
        this.value = value;
        this.expression = expression;
        this.type = type;
    }

    protected abstract string AsString();
    public string ValueString() => AsString();

    public abstract Symbol<T> Operation(Symbol<T> right, OperatorType type);
    public abstract Symbol<T> Assignment(Symbol<T> right, AssignmentType type);
    public abstract Symbol<bool> CompareTo(Symbol<T> other, OperatorType type);

    protected Symbol<T> InvokeFunction(IScriptContext context,
        string fullFunctionName, ISymbol[] parameters) {
        IFunction function = context.Functions().Find(
            f => f.Name() == fullFunctionName);
        if (function == null) {
            Debug.LogError($"Symbol.InvokeFunction : no \"{fullFunctionName}\" function found.");
            return null;
        }
        Function<T> typedFunction = function as Function<T>;
        if (typedFunction == null) {
            Debug.LogError($"Symbol.InvokeFunction : \"{fullFunctionName}\" returns " +
                           $"{function.ReturnType()} instead of {type}.");
            return null;
        }
        Symbol<T> result = SymbolFromValue(typedFunction.Lambda(context, parameters),
            typedFunction.ReturnType());
        if (result == null) {
            Debug.LogError($"Symbol.InvokeFunction(\"{fullFunctionName}\", ...) :" +
                            " execution error.");
            return null;
        }
        return result;
    }

    protected Symbol<T> IllegalAssignment(Symbol<T> right, string label) {
        Debug.LogError($"Script Error : illegal assignment : {expression} {label} {right.expression}");
        return null;
    }
    protected Symbol<T> IllegalOperation(Symbol<T> right, string label) {
        Debug.LogError($"Script Error : illegal operation {label} between \"{expression}\" and \"{right.expression}\".");
        return null;
    }

    protected Symbol<T> IllegalInvocation(string functionName) {
        Debug.LogError($"Script Error : illegal invocation on type {type} for " +
                       $"function \"{functionName}\".");
        return null;
    }

    public static bool SymbolTypeFromString(string name, out SymbolType type) {
        switch (name) {
            case "bool": type = SymbolType.Boolean; return true;
            case "int": type = SymbolType.Integer; return true;
            case "float": type = SymbolType.Float; return true;
            case "id": type = SymbolType.Id; return true;
            case "string": type = SymbolType.String; return true;
            case "date": type = SymbolType.Date; return true;
            default: type = SymbolType.Invalid; return false;
        }
    }

    public static Symbol<T> SymbolFromValue(T value, SymbolType type) {
        switch (type) {
            case SymbolType.Void: return new VoidSymbol() as Symbol<T>;
            case SymbolType.Boolean: return new BooleanSymbol(Convert.ToBoolean(value)) as Symbol<T>;
            case SymbolType.Integer: return new IntegerSymbol(Convert.ToInt32(value)) as Symbol<T>;
            case SymbolType.Float: return new FloatSymbol(Convert.ToSingle(value)) as Symbol<T>;
            case SymbolType.Id: return new IdSymbol(Convert.ToString(value)) as Symbol<T>;
            case SymbolType.String: return new StringSymbol(Convert.ToString(value)) as Symbol<T>;
            case SymbolType.Date: return new DateSymbol(Convert.ToDateTime(value)) as Symbol<T>;
            default: return null;
        }
    }

    public static string FunctionNameFromInvocation(string invocationName,
        SymbolType type) {
        switch (invocationName) {
            case "ToInt":
                switch (type) {
                    case SymbolType.Float: return "float.ToInt";
                    case SymbolType.String: return "string.ToInt";
                    default: return null;
                }
            case "ToFloat":
                switch (type) {
                    case SymbolType.Integer: return "int.ToFloat";
                    case SymbolType.String: return "string.ToFloat";
                    default: return null;
                }
            default: return null;
        }
    }
}

[Serializable]
public class VoidSymbol : Symbol<Void> {
    public VoidSymbol() : base(new Void(), "$VOID$", SymbolType.Void) { }

    protected override string AsString() => "$VOID$";
    public override Symbol<Void> Operation(Symbol<Void> right, OperatorType type) {
        return IllegalOperation(right, "of any kind");
    }

    public override Symbol<Void> Assignment(Symbol<Void> right, AssignmentType type) {
        return IllegalAssignment(right, "of any kind");
    }

    public override Symbol<bool> CompareTo(Symbol<Void> other, OperatorType type) {
        Debug.LogError("Unsupported comparison of any kind for VoidSymbol.");
        return null;
    }
}

[Serializable]
public class BooleanSymbol : Symbol<bool> {
    public BooleanSymbol(bool value)
        : base(value, value ? "true" : "false", SymbolType.Boolean) { }

    protected override string AsString() => Value ? "true" : "false";

    public override Symbol<bool> Operation(Symbol<bool> right, OperatorType type) {
        switch (type) {
            case OperatorType.LogicalAnd:
                return new BooleanSymbol(Value && right.Value);
            case OperatorType.LogicalOr:
                return new BooleanSymbol(Value || right.Value);
            default:
                Debug.LogError($"Script Error : illegal operation {type} between " +
                                "two boolean Symbols.");
                return null;
        }
    }

    public override Symbol<bool> Assignment(Symbol<bool> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = right.Value; return this;
            default: return IllegalAssignment(right, "=");
        }
    }

    public override Symbol<bool> CompareTo(Symbol<bool> other,
        OperatorType type) {
        switch (type) {
            case OperatorType.Equal: return new BooleanSymbol(Value == other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for BooleanSymbol.");
                return null;
        }
    }
}

[Serializable]
public class IntegerSymbol : Symbol<int> {
    public IntegerSymbol(int value)
        : base(value, value.ToString(), SymbolType.Integer) { }

    protected override string AsString() => Value.ToString(CultureInfo);

    public override Symbol<int> Operation(Symbol<int> right, OperatorType type) {
        switch (type) {
            case OperatorType.Addition: return new IntegerSymbol(Value + right.Value);
            case OperatorType.Substraction: return new IntegerSymbol(Value - right.Value);
            case OperatorType.Multiplication: return new IntegerSymbol(Value * right.Value);
            case OperatorType.Division: return new IntegerSymbol(Value / right.Value);
            case OperatorType.Modulo: return new IntegerSymbol(Value % right.Value);
            case OperatorType.Power: return new IntegerSymbol(Value ^ right.Value);
            default: return IllegalOperation(right, "");
        }
    }

    public override Symbol<int> Assignment(Symbol<int> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = right.Value; return this;
            case AssignmentType.AddDifference: Value += right.Value; return this;
            case AssignmentType.SubstractDifference: Value -= right.Value; return this;
            case AssignmentType.MultiplyBy: Value *= right.Value; return this;
            case AssignmentType.DivideBy: Value /= right.Value; return this;
            case AssignmentType.PoweredBy: Value %= right.Value; return this;
            case AssignmentType.ModuloBy: Value ^= right.Value; return this;
            default: return IllegalAssignment(right, "");
        }
    }

    public override Symbol<bool> CompareTo(Symbol<int> other,
        OperatorType type) {
        switch (type) {
            case OperatorType.Equal: return new BooleanSymbol(Value == other.Value);
            case OperatorType.Superior: return new BooleanSymbol(Value > other.Value);
            case OperatorType.SuperiorOrEqual: return new BooleanSymbol(Value >= other.Value);
            case OperatorType.Inferior: return new BooleanSymbol(Value < other.Value);
            case OperatorType.InferiorOrEqual: return new BooleanSymbol(Value <= other.Value);
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

[Serializable]
public class FloatSymbol : Symbol<float> {
    public FloatSymbol(float value)
        : base(value, value.ToString(CultureInfo), SymbolType.Float) { }

    protected override string AsString() => Value.ToString(CultureInfo);

    public override Symbol<float> Operation(Symbol<float> right, OperatorType type) {
        switch (type) {
            case OperatorType.Addition: return new FloatSymbol(Value + right.Value);
            case OperatorType.Substraction: return new FloatSymbol(Value - right.Value);
            case OperatorType.Multiplication: return new FloatSymbol(Value * right.Value);
            case OperatorType.Division: return new FloatSymbol(Value / right.Value);
            case OperatorType.Modulo: return new FloatSymbol(Value % right.Value);
            case OperatorType.Power: return new FloatSymbol(Mathf.Pow(Value, right.Value));
            default: return IllegalOperation(right, "");
        }
    }

    public override Symbol<float> Assignment(Symbol<float> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = right.Value; return this;
            case AssignmentType.AddDifference: Value += right.Value; return this;
            case AssignmentType.SubstractDifference: Value -= right.Value; return this;
            case AssignmentType.MultiplyBy: Value *= right.Value; return this;
            case AssignmentType.DivideBy: Value /= right.Value; return this;
            case AssignmentType.PoweredBy: Value %= right.Value; return this;
            case AssignmentType.ModuloBy: Value = Mathf.Pow(Value, right.Value); return this;
            default: return IllegalAssignment(right, "");
        }
    }

    public override Symbol<bool> CompareTo(Symbol<float> other,
        OperatorType type) {
        switch (type) {
            case OperatorType.Equal: return new BooleanSymbol(Math.Abs(Value - other.Value) < FLOAT_EPSILON);
            case OperatorType.Superior: return new BooleanSymbol(Value > other.Value);
            case OperatorType.SuperiorOrEqual: return new BooleanSymbol(Value >= other.Value);
            case OperatorType.Inferior: return new BooleanSymbol(Value < other.Value);
            case OperatorType.InferiorOrEqual: return new BooleanSymbol(Value <= other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for FloatSymbol.");
                return null;
        }
    }
}

[Serializable]
public class IdSymbol : Symbol<string> {
    public IdSymbol(string value)
        : base(value, $"@{value}", SymbolType.Id) {
    }

    protected override string AsString() => $"@{Value}";

    public override Symbol<string> Operation(Symbol<string> right, OperatorType type) {
        return IllegalOperation(right, "of any kind");
    }

    public override Symbol<string> Assignment(Symbol<string> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = right.Value; return this;
            default: return IllegalAssignment(right, $"(type = {type})");
        }
    }

    public override Symbol<bool> CompareTo(Symbol<string> other,
        OperatorType type) {
        switch (type) {
            case OperatorType.Equal: return new BooleanSymbol(Value == other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for IdSymbol.");
                return null;
        }
    }
}

[Serializable]
public class StringSymbol : Symbol<string> {
    public StringSymbol(string value)
        : base(value, $"\"{value}\"", SymbolType.String) { }

    protected override string AsString() => Value;

    public override Symbol<string> Operation(Symbol<string> right, OperatorType type) {
        switch (type) {
            case OperatorType.Addition: return new StringSymbol(Value + right.Value);
            default: return IllegalOperation(right, "non-addition");
        }
    }

    public override Symbol<string> Assignment(Symbol<string> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = right.Value; return this;
            case AssignmentType.AddDifference: Value += right.Value; return this;
            default: return IllegalAssignment(right, $"(type = {type})");
        }
    }

    public override Symbol<bool> CompareTo(Symbol<string> other,
        OperatorType type) {
        switch (type) {
            case OperatorType.Equal: return new BooleanSymbol(Value == other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for StringSymbol.");
                return null;
        }
    }
}

[Serializable]
public class DateSymbol : Symbol<DateTime> {
    public DateSymbol(DateTime value)
        : base(value, $"\"{value:yyyy/MM/dd}\"", SymbolType.Date) { }

    protected override string AsString() {
        return $"{Value:yyyy/MM/dd}";
    }

    public override Symbol<DateTime> Operation(Symbol<DateTime> right, OperatorType type) {
        return IllegalOperation(right, "of any kind");
    }

    public override Symbol<DateTime> Assignment(Symbol<DateTime> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = right.Value; return this;
            default: return IllegalAssignment(right, $"(type = {type})");
        }
    }

    public override Symbol<bool> CompareTo(Symbol<DateTime> other,
        OperatorType type) {
        switch (type) {
            case OperatorType.Equal: return new BooleanSymbol(Value == other.Value);
            case OperatorType.Superior: return new BooleanSymbol(Value > other.Value);
            case OperatorType.SuperiorOrEqual: return new BooleanSymbol(Value >= other.Value);
            case OperatorType.Inferior: return new BooleanSymbol(Value < other.Value);
            case OperatorType.InferiorOrEqual: return new BooleanSymbol(Value <= other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for DateSymbol.");
                return null;
        }
    }
}

}
