using System;
using UnityEngine;

namespace Script {

[Serializable]
public class Grammar {
    [SerializeField] private string[] reservedKeywords;

    [SerializeField] private string variableDeclarator;
    public string VariableDeclarator => variableDeclarator;

    [SerializeField] private string constantDeclarator;
    public string ConstantDeclarator => constantDeclarator;

    [SerializeField] private string typeDeclarator;
    public string TypeDeclarator => typeDeclarator;

    [SerializeField] private string functionDeclarator;
    public string FunctionDeclarator => functionDeclarator;

    [SerializeField] private string functionTypeDeclarator;
    public string FunctionTypeDeclarator => functionTypeDeclarator;

    public static Grammar DefaultGrammar() {
        return new Grammar {
            reservedKeywords = new [] {
                "void", "bool", "int", "float", "id", "string", "date", "array",
                "let", "const", "fn",
            },
            variableDeclarator = "let",
            constantDeclarator = "const",
            typeDeclarator = ":",
            functionDeclarator = "fn",
            functionTypeDeclarator = "->",
        };
    }

    public bool ValidateVariableName(string variableName) {
        if (variableName.Length == 0) return false;
        if (!char.IsLetter(variableName[0])) return false;
        if (Array.Find(reservedKeywords, k => k == variableName) != null)
            return false;
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
