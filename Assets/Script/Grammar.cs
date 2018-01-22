using System;
using UnityEngine;

namespace Script {

[Serializable]
public class Grammar {
    [SerializeField] private string variableDeclarator;
    public string VariableDeclarator => variableDeclarator;

    [SerializeField] private string typeDeclarator;
    public string TypeDeclarator => typeDeclarator;

    public static Grammar DefaultGrammar() {
        return new Grammar {
            variableDeclarator = "let",
            typeDeclarator = ":",
        };
    }

    public bool ValidateVariableName(string variableName) {
        if (variableName.Length == 0) return false;
        if (!char.IsLetter(variableName[0])) return false;
        for (int i = 1; i < variableName.Length; i++) {
            char c = variableName[i];
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return true;
    }

    public bool ValidateIdLiteral(string idLiteral) {
        if (idLiteral.Length == 0) return false;
        if (idLiteral[0] != '@') return false;
        for (int i = 1; i < idLiteral.Length; i++) {
            char c = idLiteral[i];
            if (c == '@') return false;
        }
        return true;
    }
}

}
