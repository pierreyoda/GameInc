using System;
using System.Collections.Generic;
using Database;
using Script;
using UnityEngine;

public class EngineFeaturesController : MonoBehaviour {
    [Serializable]
    public class WorldEngineFeature {
        [SerializeField] private EngineFeature info;
        public EngineFeature Info => info;

        [SerializeField] private bool allowed = false;
        public bool Allowed => allowed;

        [SerializeField] private List<Expression<bool>> requirements;
        [SerializeField] private List<IExpression> effects;

        public WorldEngineFeature(EngineFeature info,
            List<Expression<bool>> requirements, List<IExpression> effects) {
            this.info = info;
            this.requirements = requirements;
            this.effects = effects;
        }

        public bool CheckFeature(IScriptContext context) {
            // requirement check : all requirements must evaluate to True
            foreach (Expression<bool> requirement in requirements) {
                ISymbol result = requirement.Evaluate(context);
                if (result == null) {
                    Debug.Log($"EngineFeature \"{info.Id}\" : error while evaluating requirement \"{requirement.Script()}\".");
                    return true;
                }
                if (result.Type() != SymbolType.Boolean) {
                    Debug.Log($"EngineFeature \"{info.Id}\" : non-boolean requirement of type {result.Type()} \"{requirement.Script()}\".");
                    return true;
                }
                bool validated = ((Symbol<bool>) result).Value;
                if (!validated) return false;
            }

            // action when triggered
            Debug.Log($"WorldEngineFeature - Engine feature {info.Id} is now allowed.");
            allowed = true;

            return true;
        }
    }

    [SerializeField] private List<WorldEngineFeature> worldFeatures = new List<WorldEngineFeature>();

    [SerializeField] private List<string> authorizedFeaturesIDs = new List<string>();
    public List<string> AuthorizedFeaturesIDs => authorizedFeaturesIDs;

    public void InitFeatures(List<EngineFeature> engineFeatures,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        foreach (EngineFeature f in engineFeatures) {
            // Parse the requirements and effects
            List<Expression<bool>> requirements = new List<Expression<bool>>();
            List<IExpression> effects = new List<IExpression>();
            foreach (string requirement in f.Requirements) {
                Expression<bool> condition = Parser.ParseComparison(requirement,
                    localVariables, globalVariables, functions);
                if (condition == null) {
                    Debug.LogError(
                        $"EngineFeaturesController - Requirement parsing error for Event (ID = {f.Id}). Ignoring \"{requirement}\".");
                    continue;
                }
                if (condition.Type() != SymbolType.Boolean) {
                    Debug.LogError(
                        $"EngineFeaturesController - Requirement expression {condition.Type()} and not boolean for EngineFeature (ID = {f.Id}). Ignoring \"{requirement}\".");
                    continue;
                }
                requirements.Add(condition);
            }
            foreach (string effect in f.Effects) {
                IExpression action = Parser.ParseExpression(effect,
                    localVariables, globalVariables, functions);
                if (action == null) {
                    Debug.LogError($"EngineFeaturesController - Effect parsing error for EngineFeature (ID = {f.Id}). Ignoring \"{effect}\".");
                    continue;
                }
                effects.Add(action);
            }

            // Store the WorldEvent and its computed metadata
            WorldEngineFeature worldFeature = new WorldEngineFeature(f, requirements, effects);
            worldFeatures.Add(worldFeature);
        }
    }

    public void CheckFeatures(IScriptContext context) {
        foreach (WorldEngineFeature feature in worldFeatures) {
            if (feature.Allowed)
                continue;
            if (feature.CheckFeature(context))
                context.C().AllowEngineFeature(feature.Info.Id);
        }
    }
}
