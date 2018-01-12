using System;
using UnityEngine;

namespace Database {

/// <summary>
/// Description of an Employee hiring method.
/// </summary>
[Serializable]
public class HiringMethod : DatabaseElement {
    [SerializeField] private float cost;
    public float Cost => cost;

    [SerializeField] private int durationInDays;
    public int DurationInDays => durationInDays;

    [SerializeField] private string candidatesDistribution;
    private Tuple<int, int> candidatesDistributionInt;
    public Tuple<int, int> CandidatesDistribution => candidatesDistributionInt;

    [SerializeField] private string[] skillsDistribution;
    private Tuple<string, int, int>[] skillsDistributionInt;
    public Tuple<string, int, int>[] SkillsDistribution => skillsDistributionInt;

    public HiringMethod(string id, string name) : base(id, name) { }

    public override bool IsValid() {
        // Cost & duration check
        if (cost < 0) {
            Debug.LogError($"HiringMethod with ID = {Id} : cost cannot be negative.");
            return false;
        }
        if (durationInDays < 1) {
            Debug.LogError($"HiringMethod with ID = {Id} : duration must be strictly positive.");
            return false;
        }

        // Candidates number check & parsing
        string[] tokens = candidatesDistribution.Split('-');
        if (tokens.Length != 2) {
            Debug.LogError($"HiringMethod with ID = {Id} : invalid candidates number distribution.");
            return false;
        }
        int lowerBound, higherBound;
        if (!int.TryParse(tokens[0].Trim(), out lowerBound) ||
            !int.TryParse(tokens[1].Trim(), out higherBound)) {
            Debug.LogError($"HiringMethod with ID = {Id} : cannot parse candidates number bound as an integer.");
            return false;
        }
        candidatesDistributionInt = new Tuple<int, int>(lowerBound, higherBound);

        // Distribution check & parsing
        skillsDistributionInt = new Tuple<string, int, int>[skillsDistribution.Length];
        for (int i = 0; i < skillsDistribution.Length; i++) {
            tokens = skillsDistribution[i].Split('=', '-');
            if (tokens.Length != 3) {
                Debug.LogError($"HiringMethod with ID = {Id} : invalid skill distribution.");
                return false;
            }

            string name = tokens[0].Trim();
            if (!int.TryParse(tokens[1].Trim(), out lowerBound) ||
                !int.TryParse(tokens[2].Trim(), out higherBound)) {
                Debug.LogError($"HiringMethod with ID = {Id} : cannot parse skill bound as an integer.");
                return false;
            }
            if (higherBound < 0 || lowerBound < 0 || lowerBound > higherBound) {
                Debug.LogError($"HiringMethod with ID = {Id} : invalid skill bounds.");
                return false;
            }

            skillsDistributionInt[i] = new Tuple<string, int, int>(name, lowerBound, higherBound);
        }

        return base.IsValid();
    }
}

}
