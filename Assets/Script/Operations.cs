using System;

namespace Script {

[Serializable]
public enum OperatorType {
    IllegalOperation,
    OpeningParenthesis,
    ClosingParenthesis,
    LogicalAnd,
    LogicalOr,
    Addition,
    Substraction,
    Multiplication,
    Division,
    Modulo,
    Power,
    Equal,
    Different,
    Superior,
    SuperiorOrEqual,
    Inferior,
    InferiorOrEqual,
}

[Serializable]
public enum AssignmentType {
    IllegalAssignment,
    Assign,
    AddDifference,
    SubstractDifference,
    MultiplyBy,
    DivideBy,
    PoweredBy,
    ModuloBy,
}

public static class Operations {
    public static readonly string[] ComparisonOperators = {
        "==",
        "!=",
        ">",
        ">=",
        "<",
        "<=",
    };

    public static bool IsComparisonOperator(OperatorType type) {
        switch (type) {
            case OperatorType.Equal:
            case OperatorType.Different:
            case OperatorType.Superior:
            case OperatorType.SuperiorOrEqual:
            case OperatorType.Inferior:
            case OperatorType.InferiorOrEqual:
                return true;
            default:
                return false;
        }
    }

    public static bool OperatorTypeFromString(string operation,
        out OperatorType type) {
        switch (operation) {
            case "(": type = OperatorType.OpeningParenthesis; return true;
            case ")": type = OperatorType.ClosingParenthesis; return true;
            case "&&": type = OperatorType.LogicalAnd; return true;
            case "||": type = OperatorType.LogicalOr; return true;
            case "+": type = OperatorType.Addition; return true;
            case "-": type = OperatorType.Substraction; return true;
            case "*": type = OperatorType.Multiplication; return true;
            case "/": type = OperatorType.Division; return true;
            case "%": type = OperatorType.Modulo; return true;
            case "^": type = OperatorType.Power; return true;
            // comparison operators
            case "==": type = OperatorType.Equal; return true;
            case "!=": type = OperatorType.Different; return true;
            case ">": type = OperatorType.Superior; return true;
            case ">=": type = OperatorType.SuperiorOrEqual; return true;
            case "<": type = OperatorType.Inferior; return true;
            case "<=": type = OperatorType.InferiorOrEqual; return true;
            default: type = OperatorType.IllegalOperation; return false;
        }
    }

    public static int OperatorPriority(OperatorType operation) {
        switch (operation) {
            case OperatorType.OpeningParenthesis: return 7;
            case OperatorType.ClosingParenthesis: return 7;
            case OperatorType.Power: return 6;
            case OperatorType.Modulo: return 5;
            case OperatorType.Multiplication: return 4;
            case OperatorType.Division: return 4;
            case OperatorType.Addition: return 3;
            case OperatorType.Substraction: return 3;
            case OperatorType.LogicalAnd: return 2;
            case OperatorType.LogicalOr: return 1;
            case OperatorType.Equal:
            case OperatorType.Different:
            case OperatorType.Superior:
            case OperatorType.SuperiorOrEqual:
            case OperatorType.Inferior:
            case OperatorType.InferiorOrEqual: return 0;
            default: return -1;
        }
    }

    public static bool AssignmentTypeFromString(string operation,
        out AssignmentType type) {
        switch (operation) {
            case "=": type = AssignmentType.Assign; return true;
            case "+=": type = AssignmentType.AddDifference; return true;
            case "-=": type = AssignmentType.SubstractDifference; return true;
            case "*=": type = AssignmentType.MultiplyBy; return true;
            case "/=": type = AssignmentType.DivideBy; return true;
            case "^=": type = AssignmentType.PoweredBy; return true;
            case "%=": type = AssignmentType.ModuloBy; return true;
            default: type = AssignmentType.IllegalAssignment; return false;
        }
    }
}

}
