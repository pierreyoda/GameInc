using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.WSA;

namespace Script {

public enum SymbolType {
    Invalid,
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

    public abstract string AsString();
    public string ValueString() => AsString();

    public abstract Symbol<T> Operation(Symbol<T> right, OperatorType type);
    public abstract Symbol<T> Assignment(Symbol<T> right, AssignmentType type);
    public abstract Symbol<bool> CompareTo(Symbol<T> other, ComparisonType type);

    protected Symbol<T> IllegalAssignment(Symbol<T> right, string label) {
        Debug.LogError($"Script Error : illegal assignment : {expression} {label} {right.expression}");
        return null;
    }
    protected Symbol<T> IllegalOperation(Symbol<T> right, string label) {
        Debug.LogError($"Script Error : illegal operation {label} between \"{expression}\" and \"{right.expression}\".");
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
}

public class BooleanSymbol : Symbol<bool> {
    public BooleanSymbol(bool value)
        : base(value, value ? "true" : "false", SymbolType.Boolean) { }

    public override string AsString() => Value ? "true" : "false";

    public override Symbol<bool> Operation(Symbol<bool> right, OperatorType type) {
        return IllegalOperation(right, "of any kind");
    }

    public override Symbol<bool> Assignment(Symbol<bool> right, AssignmentType type) {
        switch (type) {
            case AssignmentType.Assign: Value = right.Value; return this;
            default: return IllegalAssignment(right, "=");
        }
    }

    public override Symbol<bool> CompareTo(Symbol<bool> other,
        ComparisonType type) {
        switch (type) {
            case ComparisonType.Equal: return new BooleanSymbol(Value == other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for BooleanSymbol.");
                return null;
        }
    }
}

public class IntegerSymbol : Symbol<int> {
    public IntegerSymbol(int value)
        : base(value, value.ToString(), SymbolType.Integer) { }

    public override string AsString() => Value.ToString(CultureInfo);

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
        ComparisonType type) {
        switch (type) {
            case ComparisonType.Equal: return new BooleanSymbol(Value == other.Value);
            case ComparisonType.Superior: return new BooleanSymbol(Value < other.Value);
            case ComparisonType.SuperiorOrEqual: return new BooleanSymbol(Value >= other.Value);
            case ComparisonType.Inferior: return new BooleanSymbol(Value < other.Value);
            case ComparisonType.InferiorOrEqual: return new BooleanSymbol(Value <= other.Value);
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

public class FloatSymbol : Symbol<float> {
    public FloatSymbol(float value)
        : base(value, value.ToString(CultureInfo), SymbolType.Float) { }

    public override string AsString() => Value.ToString(CultureInfo);

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
        ComparisonType type) {
        switch (type) {
            case ComparisonType.Equal: return new BooleanSymbol(Math.Abs(Value - other.Value) < FLOAT_EPSILON);
            case ComparisonType.Superior: return new BooleanSymbol(Value < other.Value);
            case ComparisonType.SuperiorOrEqual: return new BooleanSymbol(Value >= other.Value);
            case ComparisonType.Inferior: return new BooleanSymbol(Value < other.Value);
            case ComparisonType.InferiorOrEqual: return new BooleanSymbol(Value <= other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for FloatSymbol.");
                return null;
        }
    }
}

public class IdSymbol : Symbol<string> {
    public IdSymbol(string value)
        : base(value, $"{value}", SymbolType.Id) { }

    public override string AsString() => Value;

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
        ComparisonType type) {
        switch (type) {
            case ComparisonType.Equal: return new BooleanSymbol(Value == other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for IdSymbol.");
                return null;
        }
    }
}

public class StringSymbol : Symbol<string> {
    public StringSymbol(string value)
        : base(value, $"\"{value}\"", SymbolType.String) { }

    public override string AsString() => Value;

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
        ComparisonType type) {
        switch (type) {
            case ComparisonType.Equal: return new BooleanSymbol(Value == other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for StringSymbol.");
                return null;
        }
    }
}

public class DateSymbol : Symbol<DateTime> {
    public DateSymbol(DateTime value)
        : base(value, $"\"{value:yyyy/MM/dd}\"", SymbolType.Date) { }

    public override string AsString() {
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
        ComparisonType type) {
        switch (type) {
            case ComparisonType.Equal: return new BooleanSymbol(Value == other.Value);
            case ComparisonType.Superior: return new BooleanSymbol(Value < other.Value);
            case ComparisonType.SuperiorOrEqual: return new BooleanSymbol(Value >= other.Value);
            case ComparisonType.Inferior: return new BooleanSymbol(Value < other.Value);
            case ComparisonType.InferiorOrEqual: return new BooleanSymbol(Value <= other.Value);
            default:
                Debug.LogError($"Unsupported operation {type} for DateSymbol.");
                return null;
        }
    }
}

}
