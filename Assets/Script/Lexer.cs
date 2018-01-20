using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script {

public static class Lexer {
    private static readonly string[] TokenDelimiters = {
        " ", "(", ")", ",", ";",
        "+", "-", "*", "/", "^",
        "=", "==", "!=", ">", "<", ">=", "<=",
    };

    public static List<string> Tokenize(string script) {
        bool temp;
        return Tokenize(script, out temp);
    }

    public static List<string> Tokenize(string script, out bool isAssignment) {
        string token = "";
        bool assignmentDoubleLetter = false;
        isAssignment = false;
        List<string> tokens = new List<string>();
        for (int i = 0; i < script.Length; i++) {
            char c = script[i] == '\n' ? ' ' : script[i]; // treat line returns as spaces
            if (c == '\'') { // string literal
                token = token.Trim();
                if (token != "") tokens.Add(token);
                token = $"{c}";
                bool ends = false;
                char openingChar = c;
                for (int j = i + 1; j < script.Length; j++) {
                    char character = script[j];
                    token += character;
                    if (character == openingChar && script[j - 1] != '\\') {
                        tokens.Add(token);
                        token = "";
                        ends = true;
                        i = j;
                        break;
                    }
                }
                if (!ends) {
                    Debug.LogError($"Parser.Tokenize(\"{script}\") : string quote must ends.");
                    return null;
                }
                continue;
            }
            string cString = $"{c}";
            bool isOperator = TokenDelimiters.Contains(cString);
            if (isOperator) {
                if (i + 1 < script.Length) {
                    if ((c == '=' || c == '+' || c == '-' || c == '*' || c == '/' ||
                         c == '^' || c == '%' || c == '!' || c == '<' || c == '>') &&
                        script[i + 1] == '=') { // "==", "+=", "-=", "!=", ">=", ...
                        assignmentDoubleLetter = true;
                        token += c;
                        continue;
                    }
                    if ((c == '+' || c == '-') && char.IsDigit(script[i + 1])) { // number sign
                        // TODO : support math expressions such as "3+5" => 3 + 5
                        token += c;
                        continue;
                    }
                    // date support : treat every "yyyy/MM/dd" as a single token
                    if (i > 0 && c == '/' && char.IsDigit(script[i - 1]) &&
                        char.IsDigit(script[i + 1])) {
                        token += c;
                        continue;
                    }
                }
                if (assignmentDoubleLetter) token += c;
                token = token.Trim();

                // assignment basic identification
                if (!assignmentDoubleLetter && c == '=') isAssignment = true;
                if (!isAssignment) {
                    string tested = assignmentDoubleLetter ? token : $"{c}";
                    AssignmentType assignmentType;
                    if (Operations.AssignmentTypeFromString(tested, out assignmentType)) // assignment ?
                        isAssignment = true;
                }

                if (token != "") tokens.Add(token);
                if (!assignmentDoubleLetter && c != ' ') tokens.Add(cString);
                assignmentDoubleLetter = false;
                token = "";
                continue;
            }
            token += c;
        }
        tokens.Add(token.Trim());
        // tokens cleaning (TODO : check tokenization to avoid this)
        tokens = tokens.Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        return tokens;
    }
}

}
