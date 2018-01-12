using System;
using UnityEngine;

namespace Database {

/// <summary>
/// An Employee Skill.
/// </summary>
[Serializable]
public class Skill : DatabaseElement {
	[SerializeField] private string dailyProgress;
	public string DailyProgress => dailyProgress;

	[SerializeField] private string hiringCost;
	public string HiringCost => hiringCost;

	[SerializeField] private string salary;
	public string Salary => salary;

	public Skill(string id, string name, string dailyProgress,
		string hiringCost, string salary) : base(id, name) {
		this.dailyProgress = dailyProgress;
		this.hiringCost = hiringCost;
		this.salary = salary;
	}

	public override bool IsValid() {
		return base.IsValid() &&
		       IsSkillScriptValid(dailyProgress, "Game.Scores.", "daily progress") &&
		       IsSkillScriptValid(hiringCost, "", "Employee.HiringCost") &&
		       IsSkillScriptValid(salary, "Employee.Salary", "salary");
	}

	private bool IsSkillScriptValid(string script, string variablePrefix,
		string label) {
		string[] progressTokens = script.Split(' ');
		if (progressTokens.Length < 3) {
			Debug.LogError($"Skill with ID = {Id} : invalid {label}.");
			return false;
		}
		if (!progressTokens[0].Trim().StartsWith($"${variablePrefix}")) {
			Debug.LogError($"Skill with ID = {Id} : invalid variable name in {label}.");
			return false;
		}
		if (progressTokens[1].Trim() != "+=") {
			Debug.LogError($"Skill with ID = {Id} : invalid operation in {label}.");
			return false;
		}
		return true;
	}
}

}
