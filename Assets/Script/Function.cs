using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script {

public delegate Symbol<T> FunctionCall<T>(IScriptContext c, ISymbol[] p);

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
            new[] {SymbolType.Float},
            (c, p) => new IntegerSymbol((int) ((FloatSymbol) p[0]).Value)));
        functions.Add(new Function<int>("string.ToInt", SymbolType.Integer,
            new[] {SymbolType.String}, (c, p) => {
                int result;
                if (!int.TryParse(((StringSymbol) p[0]).Value, Parser.NumberStyleInteger,
                    Symbol<int>.CultureInfo, out result)) {
                    Debug.LogError($"Function ToInt({p[0].ValueString()}) : cannot parse as Integer.");
                    return null;
                }
                return new IntegerSymbol(result);
            }));
        functions.Add(new Function<float>("int.ToFloat", SymbolType.Float,
            new[] {SymbolType.Integer},
            (c, p) => new FloatSymbol(((IntegerSymbol) p[0]).Value)));
        functions.Add(new Function<float>("string.ToFloat", SymbolType.Float,
            new[] {SymbolType.String}, (c, p) => {
                float result;
                if (!float.TryParse(((StringSymbol) p[0]).Value, Parser.NumberStyleFloat,
                    Symbol<float>.CultureInfo, out result)) {
                    Debug.LogError($"Function ToFloat({p[0].ValueString()}) : cannot parse as Float.");
                    return null;
                }
                return new FloatSymbol(result);
            }));
        // Generic
        functions.Add(new Function<string>("ToString", SymbolType.String,
            new [] { SymbolType.Void }, (c, p) => new StringSymbol(p[0].ValueString())));
        // Math
        functions.Add(new Function<float>("Math.Cos", SymbolType.Float,
            new [] { SymbolType.Float },
            (c, p) => new FloatSymbol(Mathf.Cos(((FloatSymbol) p[0]).Value))));
        functions.Add(new Function<float>("Math.Sin", SymbolType.Float,
            new [] { SymbolType.Float },
            (c, p) => new FloatSymbol(Mathf.Sin(((FloatSymbol) p[0]).Value))));
        functions.Add(new Function<float>("Math.Tan", SymbolType.Float,
            new [] { SymbolType.Float },
            (c, p) => new FloatSymbol(Mathf.Tan(((FloatSymbol) p[0]).Value))));
        functions.Add(new Function<float>("Math.Abs", SymbolType.Float,
            new [] { SymbolType.Float },
            (c, p) => new FloatSymbol(Mathf.Abs(((FloatSymbol) p[0]).Value))));
        // Random
        functions.Add(new Function<float>("Random.Next", SymbolType.Float,
            new SymbolType[0], (c, p) => new FloatSymbol(Random.value)));
        functions.Add(new Function<float>("Random.Range", SymbolType.Float,
            new [] { SymbolType.Float, SymbolType.Float },
            (c, p) => new FloatSymbol(Random.Range(((FloatSymbol) p[0]).Value,
                                                   ((FloatSymbol) p[1]).Value))));
        // Arrays
        functions.Add(new Function<int>("array.Count", SymbolType.Integer,
            new [] { SymbolType.Array }, (c, p) => {
                int count;
                switch (p[0].ArrayType()) {
                    case SymbolType.Void: count = ((ArraySymbol<Void>) p[0]).Value.Elements.Count; break;
                    case SymbolType.Boolean: count = ((ArraySymbol<bool>) p[0]).Value.Elements.Count; break;
                    case SymbolType.Integer: count = ((ArraySymbol<int>) p[0]).Value.Elements.Count; break;
                    case SymbolType.Float: count = ((ArraySymbol<float>) p[0]).Value.Elements.Count; break;
                    case SymbolType.Id: count = ((ArraySymbol<Id>) p[0]).Value.Elements.Count; break;
                    case SymbolType.String: count = ((ArraySymbol<string>) p[0]).Value.Elements.Count; break;
                    case SymbolType.Date: count = ((ArraySymbol<DateTime>) p[0]).Value.Elements.Count; break;
                    default:
                        Debug.LogError( "Function array.Count : unsupported Array " +
                                       $"type \"{p[0].ArrayType()}\".");
                        return null;
                }
                return new IntegerSymbol(count);
            }));
        // Company
        functions.Add(new Function<bool>("Company.SetFeature",
            SymbolType.Boolean, new [] { SymbolType.Id, SymbolType.Boolean },
            (c, p) => {
                string featureId = ((IdSymbol) p[0]).Value.Identifier;
                bool enabled = ((BooleanSymbol) p[1]).Value;
                bool result = c.C().SetFeature(featureId, enabled);
                if (!result)
                    Debug.LogError($"Function Company.SetFeature({featureId}, {enabled}) : " +
                                   $"invalid Feature ID \"{featureId}\".");
                return new BooleanSymbol(result);
            }));
        // Projects Statistics
        functions.Add(new Function<int>("Company.Projects.CompletedGamesCount",
            SymbolType.Integer, new SymbolType[0],
            (c, p) => new IntegerSymbol(c.C().CompletedProjects.Games.Count)));
        functions.Add(new Function<int>("Company.Projects.CompletedGames.WithEngineFeatureCount",
            SymbolType.Integer, new [] { SymbolType.Id },
            (c, p) => new IntegerSymbol(c.C().CompletedProjects.GamesWithEngineFeature(
                ((IdSymbol) p[0]).Value.Identifier).Count)));
        // Games
        functions.Add(new Function<float>("CurrentGame.Score",
            SymbolType.Float, new [] { SymbolType.Id },
            (c, p) => {
                GameProject game = c.C().CurrentGame();
                string scoreId = ((IdSymbol) p[0]).Value.Identifier;
                if (game == null) {
                    Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no current Game Project.");
                    return null;
                }
                Project.ProjectScore score = game.Scores.Find(s => s.Id == scoreId);
                if (score != null) return new FloatSymbol(score.score);
                Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no such Score ID.");
                return null;
            }));
        functions.Add(new Function<float>("CurrentGame.ModifyScore",
            SymbolType.Float, new [] { SymbolType.Id, SymbolType.Float },
            (c, p) => {
                GameProject game = c.C().CurrentGame();
                string scoreId = ((IdSymbol) p[0]).Value.Identifier;
                if (game == null) {
                    Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no current Game Project.");
                    return null;
                }
                Project.ProjectScore score = game.Scores.Find(s => s.Id == scoreId);
                if (score == null) {
                    Debug.LogError($"Function Company.CurrentGame.Score({scoreId}) : no such Score ID.");
                    return null;
                }
                score.score += ((FloatSymbol) p[1]).Value;
                return new FloatSymbol(score.score);
            }));
        // Employee Skills Proficiency
        functions.Add(new Function<float>("CurrentEmployee.Skill",
            SymbolType.Float, new [] { SymbolType.Id },
            (c, p) => {
                string skillId = ((IdSymbol) p[0]).Value.Identifier;
                Employee employee = c.CurrentEmployee();
                if (employee == null) {
                    Debug.LogError($"Function CurrentEmployee.Skill({skillId}) : no current Employee set.");
                    return null;
                }
                EmployeeSkill employeeSkill = Array.Find(employee.EmployeeSkills,
                    ec => ec.Id == skillId);
                if (employeeSkill == null) {
                    Debug.LogError($"Function CurrentEmployee.Skill({skillId}) : no such Skill for Employee \"{employee.Name}\".");
                    return null;
                }
                return new FloatSymbol(employeeSkill.Proficiency);
            }));

        return functions;
    }
}

}
