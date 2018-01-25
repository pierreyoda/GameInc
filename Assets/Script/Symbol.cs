using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Script {

[Serializable]
public class Void { }

[Serializable]
public struct Id {
    public readonly string Identifier;

    public Id(string value) {
        Identifier = value;
    }

    public override String ToString() => Identifier;
}

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
    Array,
}

public interface ISymbol {
    SymbolType Type();
    SymbolType ArrayType();
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
    public SymbolType Type() => type;

    [SerializeField] protected SymbolType arrayType = SymbolType.Invalid;
    public SymbolType ArrayType() => arrayType;

    protected Symbol(T value, string expression, SymbolType type) {
        this.value = value;
        this.expression = expression;
        this.type = type;
    }

    protected abstract string AsString();
    public string ValueString() => AsString();

    public abstract Symbol<T> Operation(Symbol<T> right, OperatorType type);
    public abstract Symbol<T> Assignment(Symbol<T> right, AssignmentType type);
    public abstract Symbol<bool> CompareTo(Symbol<T> other, OperatorType type,
        IScriptContext context);

    protected Symbol<T> IllegalAssignment(Symbol<T> right, string label) {
        Debug.LogError($"Script Error : illegal assignment : {expression} {label} {right.expression}");
        return null;
    }
    protected Symbol<T> IllegalOperation(Symbol<T> right, string label) {
        Debug.LogError($"Script Error : illegal operation {label} between \"{expression}\" and \"{right.expression}\".");
        return null;
    }

    public static bool SymbolTypeFromString(string name, out SymbolType type) {
        // array
        if (name.EndsWith("[]")) {
            type = SymbolType.Array;
            int index = name.IndexOf('[');
            string arrayType = name.Substring(0, index);
            SymbolType tmp;
            return SymbolTypeFromString(arrayType, out tmp);
        }
        // scalar
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
            case SymbolType.Id: return new IdSymbol(value.ToString()) as Symbol<T>; // TODO : find better way ?
            case SymbolType.String: return new StringSymbol(value as string) as Symbol<T>;
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
            case "Count":
                return type == SymbolType.Array ? "array.Count" : null;
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

    public override Symbol<bool> CompareTo(Symbol<Void> other, OperatorType type,
        IScriptContext context) {
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
            default: return IllegalAssignment(right, type.ToString());
        }
    }

    public override Symbol<bool> CompareTo(Symbol<bool> other, OperatorType type,
        IScriptContext context) {
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
            default: return IllegalOperation(right, type.ToString());
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
            default: return IllegalAssignment(right, type.ToString());
        }
    }

    public override Symbol<bool> CompareTo(Symbol<int> other, OperatorType type,
        IScriptContext context) {
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
            default: return IllegalOperation(right, type.ToString());
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
            default: return IllegalAssignment(right, type.ToString());
        }
    }

    public override Symbol<bool> CompareTo(Symbol<float> other, OperatorType type,
        IScriptContext context) {
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
public class IdSymbol : Symbol<Id> {
    public IdSymbol(string value)
        : base(new Id(value), $"@{value}", SymbolType.Id) {
    }

    protected override string AsString() => $"@{Value.Identifier}";

    public override Symbol<Id> Operation(Symbol<Id> right, OperatorType type) {
        return IllegalOperation(right, "of any kind");
    }

    public override Symbol<Id> Assignment(Symbol<Id> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = new Id(right.Value.Identifier); return this;
            default: return IllegalAssignment(right, type.ToString());
        }
    }

    public override Symbol<bool> CompareTo(Symbol<Id> other, OperatorType type,
        IScriptContext context) {
        switch (type) {
            case OperatorType.Equal: return new BooleanSymbol(Value.Identifier == other.Value.Identifier);
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
            default: return IllegalAssignment(right, type.ToString());
        }
    }

    public override Symbol<bool> CompareTo(Symbol<string> other, OperatorType type,
        IScriptContext context) {
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
            default: return IllegalAssignment(right, type.ToString());
        }
    }

    public override Symbol<bool> CompareTo(Symbol<DateTime> other, OperatorType type,
        IScriptContext context) {
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

[Serializable]
public class ArraySymbol<T> : Symbol<T> {
    [SerializeField] private List<Expression<T>> elements;
    public IReadOnlyList<Expression<T>> Elements => elements.AsReadOnly();

    public ArraySymbol(List<Expression<T>> elements, SymbolType arrayType)
        : base(default(T), $"[{string.Join(", ", elements.Select(s => s.Script()))}]",
            SymbolType.Array) {
        this.elements = elements;
        this.arrayType = arrayType;
    }

    protected override string AsString() {
        return '[' + string.Join(", ", elements.Select(v => v.Script())) + ']';
    }

    public override Symbol<T> Operation(Symbol<T> right, OperatorType type) {
        ArraySymbol<T> rightArray = right as ArraySymbol<T>;
        Assert.IsNotNull(rightArray);
        switch (type) {
            case OperatorType.Addition:
                List<Expression<T>> list = new List<Expression<T>>(elements);
                list.AddRange(rightArray.elements);
                return new ArraySymbol<T>(list, arrayType);
            default:
                return IllegalOperation(right, type.ToString());
        }
    }

    public override Symbol<T> Assignment(Symbol<T> right, AssignmentType type) {
        ArraySymbol<T> rightArray = right as ArraySymbol<T>;
        Assert.IsNotNull(rightArray);
        switch (type) {
            case AssignmentType.Assign: elements = new List<Expression<T>>(rightArray.elements); return this;
            default: return IllegalAssignment(right, type.ToString());
        }
    }

    public override Symbol<bool> CompareTo(Symbol<T> other, OperatorType type,
        IScriptContext context) {
        ArraySymbol<T> otherArray = other as ArraySymbol<T>;
        Assert.IsNotNull(otherArray);
        switch (type) {
            case OperatorType.Equal:
                if (elements.Count != otherArray.elements.Count)
                    return new BooleanSymbol(false);
                for (int i = 0; i < elements.Count; i++) {
                    Symbol<T> left = elements[i].Evaluate(context);
                    if (left == null) {
                        Debug.LogError($"ArraySymbol<{typeof(T)}>.CompareTo : evaluation " +
                                       $"error for self Array at element n°{i}.");
                        return new BooleanSymbol(false);
                    }
                    Symbol<T> right = otherArray.elements[i].Evaluate(context);
                    if (right == null) {
                        Debug.LogError($"ArraySymbol<{typeof(T)}>.CompareTo : evaluation " +
                                       $"error for other Array at element n°{i}.");
                        return new BooleanSymbol(false);
                    }
                    if (!left.CompareTo(right, OperatorType.Equal, context).Value)
                        return new BooleanSymbol(false);
                }
                return new BooleanSymbol(true);
            default:
                Debug.LogError($"Unsupported operation {type} for ArraySymbol.");
                return null;
        }
    }
}

}
