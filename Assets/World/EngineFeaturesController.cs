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

        [SerializeField] private TypedExecutable<bool> requirement;
        [SerializeField] private Executable effect;

        public WorldEngineFeature(EngineFeature info,
            TypedExecutable<bool> requirement, Executable effect) {
            this.info = info;
            this.requirement = requirement;
            this.effect = effect;
        }

        public bool CheckFeature(IScriptContext context) {
            // requirement check
            bool validated;
            if (!requirement.Compute(context, out validated)) {
                Debug.LogError($"EngineFeature \"{info.Id}\" : error while evaluating requirement \"{info.Requirement}\".");
                return true;
            }
            if (!validated) return false;

            // action when triggered
            allowed = true;
            Debug.Log($"WorldEngineFeature - Engine feature {info.Id} is now allowed.");
            return true;
        }
    }

    [SerializeField] private List<WorldEngineFeature> worldFeatures = new List<WorldEngineFeature>();

    public void CreateFeatures(List<EngineFeature> engineFeatures,
        ParserContext parserContext) {
        foreach (EngineFeature f in engineFeatures) {
            // Parse the Requirement script
            TypedExecutable<bool> requirement = TypedExecutable<bool>.FromScript(
                f.Requirement, parserContext);
            if (requirement == null) {
                Debug.LogError( "EngineFeaturesController.InitFeatures : Requirement parsing " +
                               $"error for EngineFeature (ID = {f.Id}). Ignoring \"{f.Requirement}\".");
                continue;
            }

            // Parse the Effect script
            Executable effect = Executable.FromScript(f.Effect, parserContext);
            if (effect == null) {
                Debug.LogError( "EngineFeaturesController.InitFeatures : Effect parsing " +
                                $"error for EngineFeature (ID = {f.Id}). Ignoring \"{f.Effect}\".");
                continue;
            }

            // Store the WorldEvent and its computed metadata
            WorldEngineFeature worldFeature = new WorldEngineFeature(f, requirement, effect);
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
