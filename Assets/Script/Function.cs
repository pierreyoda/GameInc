using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script {

public delegate T FunctionCall<out T>(IScriptContext c, ISymbol[] p);

public interface IFunction {
    string Name();
    SymbolType ReturnType();
    SymbolType[] Parameters();
}

[Serializable]
public class Function<T> : IFunction {
    [SerializeField] private string name;
    public string Name() => name;

    [SerializeField] private SymbolType returnType;
    public SymbolType ReturnType() => returnType;

    [SerializeField] private SymbolType[] parameters; // void : any type
    public SymbolType[] Parameters() => parameters;

    [SerializeField] private readonly FunctionCall<T> lambda;
    public FunctionCall<T> Lambda => lambda;

    private Function(string name, SymbolType returnType, SymbolType[] parameters,
        FunctionCall<T> lambda) {
        this.name = name;
        this.returnType = returnType;
        this.parameters = parameters;
        this.lambda = lambda;
    }

    public static List<IFunction> DefaultFunctions() {
        List<IFunction> functions = new List<IFunction>();

        // Conversion
        functions.Add(new Function<int>("float.ToInt", SymbolType.Integer,
            new[] {SymbolType.Float}, (c, p) => (int) (p[0] as FloatSymbol).Value));
        functions.Add(new Function<int>("string.ToInt", SymbolType.Integer,
            new[] {SymbolType.String}, (c, p) => {
                int result;
                if (!int.TryParse((p[0] as StringSymbol).Value, Parser.NumberStyleInteger,
                    Symbol<int>.CultureInfo, out result)) {
                    Debug.LogError($"Function ToInt({p[0].ValueString()}) : cannot parse as Integer.");
                    return 0;
                }
                return result;
            }));
        functions.Add(new Function<float>("int.ToFloat", SymbolType.Float,
            new[] {SymbolType.Integer}, (c, p) => (float) (p[0] as IntegerSymbol).Value));
        functions.Add(new Function<float>("string.ToFloat", SymbolType.Float,
            new[] {SymbolType.String}, (c, p) => {
                float result;
                if (!float.TryParse((p[0] as StringSymbol).Value, Parser.NumberStyleFloat,
                    Symbol<float>.CultureInfo, out result)) {
                    Debug.LogError($"Function ToFloat({p[0].ValueString()}) : cannot parse as Float.");
                    return 0f;
                }
                return result;
            }));
        // Generic
        functions.Add(new Function<string>("ToString", SymbolType.String,
            new [] { SymbolType.Void }, (c, p) => p[0].ValueString()));
        // Math
        functions.Add(new Function<float>("Math.Cos", SymbolType.Float,
            new [] { SymbolType.Float }, (c, p) => Mathf.Cos((p[0] as FloatSymbol).Value)));
        functions.Add(new Function<float>("Math.Sin", SymbolType.Float,
            new [] { SymbolType.Float }, (c, p) => Mathf.Sin((p[0] as FloatSymbol).Value)));
        functions.Add(new Function<float>("Math.Tan", SymbolType.Float,
            new [] { SymbolType.Float }, (c, p) => Mathf.Tan((p[0] as FloatSymbol).Value)));
        functions.Add(new Function<float>("Math.Abs", SymbolType.Float,
            new [] { SymbolType.Float }, (c, p) => Mathf.Abs((p[0] as FloatSymbol).Value)));
        // Random
        functions.Add(new Function<float>("Random.Next", SymbolType.Float,
            new SymbolType[0], (c, p) => Random.value));
        functions.Add(new Function<float>("Random.Range", SymbolType.Float,
            new [] { SymbolType.Float, SymbolType.Float },
            (c, p) => Random.Range((p[0] as FloatSymbol).Value, (p[1] as FloatSymbol).Value)));
        // Company
        functions.Add(new Function<bool>("Company.SetFeature",
            SymbolType.Boolean, new [] { SymbolType.Id, SymbolType.Boolean },
            (c, p) => {
                string featureId = (p[0] as IdSymbol).Value;
                bool enabled = (p[1] as BooleanSymbol).Value;
                if (!c.C().SetFeature(featureId, enabled)) {
                    Debug.LogError($"Function Company.SetFeature({featureId}, {enabled}) : " +
                                   $"invalid Feature ID \"{featureId}\".");
                    return false;
                }
                return true;
            }));
        // Projects Statistics
        functions.Add(new Function<int>("Company.Projects.CompletedGamesCount",
            SymbolType.Integer, new SymbolType[0],
            (c, p) => c.C().CompletedProjects.Games.Count));
        functions.Add(new Function<int>("Company.Projects.CompletedGames.WithEngineFeatureCount",
            SymbolType.Integer, new [] { SymbolType.Id },
            (c, p) => c.C().CompletedProjects.GamesWithEngineFeature((p[0] as IdSymbol).Value).Count));
        // Games
        functions.Add(new Function<float>("CurrentGame.Score",
            SymbolType.Float, new [] { SymbolType.Id },
            (c, p) => {
                GameProject game = c.C().CurrentGame();
                string scoreId = (p[0] as IdSymbol).Value;
                if (game == null) {
                    Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no current Game Project.");
                    return 0f;
                }
                Project.ProjectScore score = game.Scores.Find(s => s.Id == scoreId);
                if (score == null) {
                    Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no such Score ID.");
                    return 0f;
                }
                return score.score;
            }));
        functions.Add(new Function<float>("CurrentGame.ModifyScore",
            SymbolType.Float, new [] { SymbolType.Id, SymbolType.Float },
            (c, p) => {
                GameProject game = c.C().CurrentGame();
                string scoreId = (p[0] as IdSymbol).Value;
                if (game == null) {
                    Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no current Game Project.");
                    return 0f;
                }
                Project.ProjectScore score = game.Scores.Find(s => s.Id == scoreId);
                if (score == null) {
                    Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no such Score ID.");
                    return 0f;
                }
                score.score += (p[1] as FloatSymbol).Value;
                return score.score;
            }));
        // Employee Skills Proficiency
        functions.Add(new Function<float>("CurrentEmployee.Skill",
            SymbolType.Float, new [] { SymbolType.Id },
            (c, p) => {
                string skillId = (p[0] as IdSymbol).Value;
                Employee employee = c.CurrentEmployee();
                if (employee == null) {
                    Debug.LogError($"Function CurrentEmployee.Skill({skillId}) : no current Employee set.");
                    return 0f;
                }
                EmployeeSkill employeeSkill = Array.Find(employee.EmployeeSkills,
                    ec => ec.Id == skillId);
                if (employeeSkill == null) {
                    Debug.LogError($"Function CurrentEmployee.Skill({skillId}) : no such Skill for Employee \"{employee.Name}\".");
                    return 0f;
                }
                return employeeSkill.Proficiency;
            }));

        return functions;
    }
}

}
