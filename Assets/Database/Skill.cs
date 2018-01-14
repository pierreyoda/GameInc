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
}

}
