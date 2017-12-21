using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Database {

public class JsonFormatter {
    /// <summary>
    /// Format the given JSON data file to strip it of any unsupported feature :
    /// - Single-line comments (example : "// comment").
    /// - Trailing commas in arrays and objects, friendlier with Git. Example :
    /// {
    ///     "item1": 1,
    ///     "item2": [
    ///         4,
    ///         5,
    ///     ],
    /// }
    ///
    /// - Multi-lines strings. Example :
    /// {
    ///     "text" : """
    /// my
    /// multi
    /// lines
    /// string
    /// """,
    ///     "value" : 0,
    /// }
    ///
    /// </summary>
    /// <param name="json">The clean JSON file.</param>
    /// <returns></returns>
    public static string Format(string json) {
        string cleaned = "";

        int multistringStart = -1;
        string[] lines = json.Split('\n');
        List<string> multistrings = new List<string>();
        for (int i = 0; i < lines.Length; i++) {
            string lineTrimmed = lines[i].Trim();
            int commentStart = lineTrimmed.IndexOf("//", StringComparison.Ordinal);
            if (commentStart != -1)
                lineTrimmed = lineTrimmed.Substring(0, commentStart).TrimEnd();

            // Remove trailing commas
            if (lineTrimmed.EndsWith(",") && i + 1 < lines.Length && multistringStart == -1) {
                bool trailingComma = true;
                for (int j = i + 1; j < lines.Length; j++) {
                    string nextLine = lines[j].Trim();
                    if (nextLine.StartsWith("//")) continue;
                    if (nextLine.StartsWith("}") || nextLine.StartsWith("]")) break;
                    cleaned += lineTrimmed + "\n";
                    trailingComma = false;
                    break;
                }
                if (trailingComma)
                    cleaned += lineTrimmed.Substring(0, lineTrimmed.Length - 1).TrimEnd() + "\n";
                continue;
            }

            // Empty line : count only inside multi-lines string
            if (lineTrimmed.Length == 0 && multistringStart == -1) continue;

            // Process multi-lines string literals
            int multistringDelimiterIndex =
                lineTrimmed.IndexOf("\"\"\"", StringComparison.Ordinal); // NB : position of last double quote
            if (multistringStart == -1 && multistringDelimiterIndex != -1) { // start
                multistringStart = multistringDelimiterIndex;
                multistrings.Add(lineTrimmed.Substring(0, multistringStart - 1));
                continue;
            }
            if (multistringStart != -1 && multistringDelimiterIndex != -1) { // end
                if (multistringDelimiterIndex >= 2)
                    multistrings.Add(lineTrimmed.Substring(0, multistringDelimiterIndex - 2));
                string comma = lineTrimmed.EndsWith(",") ? "," : "";

                if (multistrings.Count == 0) {
                    Debug.LogError("Database.FormatJson : invalid multi-lines string.");
                    return null;
                }
                string start = multistrings.First();
                string literal = string.Join("\\n", multistrings.Skip(1));
                cleaned += $"{start} \"{literal}\"{comma}\n";

                multistrings.Clear();
                multistringStart = -1;
                continue;
            }
            if (multistringStart >= 0) { // middle
                multistrings.Add(lineTrimmed);
                continue;
            }

            // Ignore single-line comments (except in multi-line strings)
            if (multistringStart == -1 && commentStart != -1) {
                cleaned += lineTrimmed.Substring(0, commentStart).TrimEnd() + "\n";
                continue;
            }

            cleaned += $"{lineTrimmed}\n";
        }

        return cleaned;
    }
}

}
