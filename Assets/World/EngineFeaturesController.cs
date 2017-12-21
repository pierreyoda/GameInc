using System;
using System.Collections.Generic;
using System.Linq;
using Database;
using UnityEngine;
using static ScriptParser;

public class EngineFeaturesController : MonoBehaviour {
    [Serializable]
    public class WorldEngineFeature {
        [SerializeField] private EngineFeature info;
        public EngineFeature Info => info;

        [SerializeField] private bool allowed = false;
        public bool Allowed => allowed;

        [SerializeField] private List<ScriptCondition> requirements;
        [SerializeField] private List<ScriptAction> effects;

        public WorldEngineFeature(EngineFeature info,
            List<ScriptCondition> requirements, List<ScriptAction> effects) {
            this.info = info;
            this.requirements = requirements;
            this.effects = effects;
        }

        public bool CheckFeature(EventsController ec, DateTime d, GameDevCompany c) {
            // requirement check : all requirements must evaluate to True
            foreach (ScriptCondition requirement in requirements) {
                if (!requirement(ec, d, c)) return false;
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

    public void InitFeatures(List<EngineFeature> engineFeatures) {
        foreach (EngineFeature f in engineFeatures) {
            // Parse the requirements and effects
            List<ScriptCondition> requirements = new List<ScriptCondition>();
            List<ScriptAction> effects = new List<ScriptAction>();
            foreach (string requirement in f.Requirements) {
                ScriptCondition trigger = ParseCondition(requirement);
                if (trigger == null) {
                    Debug.LogError($"EngineFeaturesController - Requirement parsing error for EngineFeature (ID = {f.Id}).");
                    break;
                }
                requirements.Add(trigger);
            }
            foreach (string effect in f.Effects) {
                ScriptAction trigger = ParseAction(effect);
                if (trigger == null) {
                    Debug.LogError($"EngineFeaturesController - Effect parsing error for EngineFeature (ID = {f.Id}).");
                    break;
                }
                effects.Add(trigger);
            }

            // Store the WorldEvent and its computed metadata
            WorldEngineFeature worldFeature = new WorldEngineFeature(f, requirements, effects);
            worldFeatures.Add(worldFeature);
        }
    }

    public void CheckFeatures(EventsController ec, DateTime d, GameDevCompany c) {
        foreach (WorldEngineFeature feature in worldFeatures) {
            if (feature.Allowed)
                continue;
            if (feature.CheckFeature(ec, d, c))
                c.AllowEngineFeature(feature.Info.Id);
        }
    }

    private static ScriptAction ParseEngineFeatureEffect(string effect) {
        string[] tokens = effect.Split(' ');
        if (tokens.Length < 3) return null;

        string leftName = tokens[0];
        if (!leftName.StartsWith("$Game.")) {
            Debug.LogError($"EngineFeaturesController.ParseEngineFeatureEffect(\"{effect}\") : no game scores assignment.");
            return null;
        }

        string operation = tokens[1];
        if (operation != "+=" && operation != "-=") {
            Debug.LogError($"EngineFeaturesController.ParseEngineFeatureEffect(\"{effect}\") : illegal operation.");
            return null;
        }

        VariableFloat rightValue = ParseExpressionFloat(tokens.Skip(2));
        if (rightValue == null) {
            Debug.LogError($"EngineFeaturesController.ParseEngineFeatureEffect(\"{effect}\") : right operand parsing error.");
            return null;
        }

        string affectedScore = leftName.Split('.')[1];
        return (ec, d, c) => c.CurrentGame().ModifyScore(affectedScore, rightValue(ec, d, c));
    }
}
