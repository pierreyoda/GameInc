using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Database {

/// <summary>
/// A multi-lines text element supporting multiple translations and embedded variables.
/// Uses a custom serialization text format to enable multi-lines strings.
/// </summary>
[Serializable]
public class Text : DatabaseElement {
    public static string[] SUPPORTED_LANGUAGES = { "English" };
    private static char CHAR_TEXT_ITEM_START = '=';
    private static char CHAR_LANGUAGE_START = '.';

    [SerializeField] private string[] textEnglish;
    public string[] TextEnglish => textEnglish;

    public Text(string id, string[] textEnglish) : base(id, id) {
        this.textEnglish = textEnglish;
    }

    public override bool IsValid() {
        bool valid = true;
        foreach (string line in textEnglish) {
            if (!IsLineValid(line)) {
                valid = false;
                break;
            }
        }
        return valid && base.IsValid();
    }

    private bool IsLineValid(string line) {
        string[] tokens = line.Split(' ');
        foreach (string token in tokens) {
            string tokenTrim = token.Trim();
            if (tokenTrim.Length == 0) continue;
            // game variable
            if (tokenTrim.StartsWith("$")) {
                if (tokenTrim.Length == 1) {
                    Debug.LogError($"Text with ID = {Id} : empty game variable name.");
                    return false;
                }
                string variableName = tokenTrim.Substring(1);
                if (!Event.SUPPORTED_VARIABLES.Contains(variableName)) {
                    Debug.LogError($"Text with ID = {Id} : unkown game variable \"{variableName}\".");
                    return false;
                }
            } else if (tokenTrim == "@") {
                Debug.LogError($"Text with ID = {Id} : empty event variable name.");
                return false;
            }
        }
        return true;
    }

    public static List<Text> LoadTextsFile(string filePath) {
        List<Text> texts = new List<Text>();

        int itemStartLine = 0;
        string id = "";
        string language = "";
        List<string> textEnglish = new List<string>();

        List<string> lines = File.ReadLines(filePath).ToList();
        for (int i = 0; i < lines.Count; i++) {
            string line = lines[i].TrimEnd();
            // text item addition
            if (line.StartsWith($"{CHAR_TEXT_ITEM_START}") || i == lines.Count - 1) {
                if (i == lines.Count - 1) textEnglish.Add(line);
                if (id != "" && language != "" && textEnglish.Count > 0) {
                    texts.Add(new Text(id, textEnglish.ToArray()));
                    language = "";
                    textEnglish.Clear();
                }
            }
            // textitem
            if (line.StartsWith($"{CHAR_TEXT_ITEM_START}")) {
                id = line.Substring(1);
                if (i + 2 >= lines.Count) {
                    Debug.LogError($"Database - Text.LoadTextsFile(\"{filePath}\") : invalid item (ID = \"{id}\").");
                    return null;
                }
                itemStartLine = i;
            } else if (line.StartsWith($"{CHAR_LANGUAGE_START}")) {
                language = line.Substring(1);
                if (!SUPPORTED_LANGUAGES.Contains(language)) {
                    Debug.LogError($"Database - Text.LoadTextsFile(\"{filePath}\") : unsupported language \"{language}\").");
                    return null;
                }
            } else if (language != "" && i >= itemStartLine + 2) {
                textEnglish.Add(line);
            }
        }

        return texts;
    }
}

}
