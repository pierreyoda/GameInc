using System;
using UnityEngine;

namespace Script {

[Serializable]
public enum OperatorType {
    IllegalOperation,
    OpeningParenthesis,
    ClosingParenthesis,
    Addition,
    Substraction,
    Multiplication,
    Division,
    Modulo,
    Power,
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


public enum ComparisonType {
    IllegalComparison,
    Equal,
    Different,
    Superior,
    SuperiorOrEqual,
    Inferior,
    InferiorOrEqual,
}

public class Operations {
    public static bool OperatorTypeFromString(string operation,
        out OperatorType type) {
        switch (operation) {
            case "(": type = OperatorType.OpeningParenthesis; return true;
            case ")": type = OperatorType.ClosingParenthesis; return true;
            case "+": type = OperatorType.Addition; return true;
            case "-": type = OperatorType.Substraction; return true;
            case "*": type = OperatorType.Multiplication; return true;
            case "/": type = OperatorType.Division; return true;
            case "%": type = OperatorType.Modulo; return true;
            case "^": type = OperatorType.Power; return true;
            default: type = OperatorType.IllegalOperation; return false;
        }
    }

    public static int OperatorPriority(OperatorType operation) {
        switch (operation) {
            case OperatorType.OpeningParenthesis: return 6;
            case OperatorType.ClosingParenthesis: return 6;
            case OperatorType.Power: return 5;
            case OperatorType.Modulo: return 4;
            case OperatorType.Multiplication: return 3;
            case OperatorType.Division: return 3;
            case OperatorType.Addition: return 2;
            case OperatorType.Substraction: return 2;
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

    public static readonly string[] ComparisonOperators = {
        "==",
        "!=",
        ">",
        ">=",
        "<",
        "<=",
    };

    public static bool ComparisonTypeFromString(string comparison,
        out ComparisonType type) {
        switch (comparison) {
            case "==": type = ComparisonType.Equal; return true;
            case "!=": type = ComparisonType.Different; return true;
            case ">": type = ComparisonType.Superior; return true;
            case ">=": type = ComparisonType.SuperiorOrEqual; return true;
            case "<": type = ComparisonType.Inferior; return true;
            case "<=": type = ComparisonType.InferiorOrEqual; return true;
            default: type = ComparisonType.IllegalComparison; return false;
        }
    }
}

}
