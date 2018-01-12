using System;
using System.Linq;
using Database;
using NUnit.Framework;
using static ScriptParser;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Provides staff management utilities for a GameDevCompany to use.
/// </summary>
public class Staff : MonoBehaviour {
    private class SkillType {
        [SerializeField] private string id;
        public string Id => id;

        [SerializeField] private ScriptAction dailyProgress;
        public ScriptAction DailyProgress => dailyProgress;

        [SerializeField] private ExpressionFloat hiringCost;
        public ExpressionFloat HiringCost => hiringCost;

        [SerializeField] private ExpressionFloat salary;
        public ExpressionFloat Salary => salary;

        public SkillType(string id, ScriptAction dailyProgress, ExpressionFloat hiringCost, ExpressionFloat salary) {
            this.id = id;
            this.dailyProgress = dailyProgress;
            this.hiringCost = hiringCost;
            this.salary = salary;
        }
    }

    [SerializeField] private Employee employeeModel;
    [SerializeField] private SkillType[] skillTypes;

    private void Start() {
        Assert.IsNotNull(employeeModel);
        employeeModel.gameObject.SetActive(false);
    }

    public void InitSkillsTypes(Database.Database.DatabaseCollection<Skill> skills) {
        skillTypes = new SkillType[skills.Collection.Count];
        for (int i = 0; i < skills.Collection.Count; i++) {
            Skill skill = skills.Collection[i];
            ScriptAction dailyProgress = ParseAction(skill.DailyProgress);
            if (dailyProgress == null) {
                Debug.LogError(
                    $"Staff.InitSkillsTypes : parsing error in daily progress for Skill with ID = {skill.Id}.");
                continue;
            }
            ExpressionFloat hiringCost = ParseSkillExpression(skill.Id, skill.HiringCost, "hiring cost");
            ExpressionFloat salary = ParseSkillExpression(skill.Id, skill.Salary, "salary");
            if (hiringCost == null || salary == null) continue;
            skillTypes[i] = new SkillType(skill.Id, dailyProgress, hiringCost, salary);
        }
    }

    private static ExpressionFloat ParseSkillExpression(string skillId, string expression, string label) {
        string[] tokens = expression.Split(' ');
        Assert.IsTrue(tokens.Length >= 3);
        ExpressionFloat skillExpression = ParseExpressionFloat(tokens.Skip(2));
        if (skillExpression == null)
            Debug.LogError($"Staff.ParseSkillExpression : parsing error in {label} for Skill with ID = {skillId}.");
        return skillExpression;
    }

    public Employee GenerateRandomEmployee(EventsController ec, DateTime d,
        GameDevCompany c, HiringMethod hiringMethod, Names commonNames,
        Database.Database.DatabaseCollection<Skill> skillsCollection,
        out float hiringCost) {
        // Sex
        bool male = Random.value > 0.5f;

        // Name
        string firstName = commonNames.RandomFirstName(male);
        string lastName = commonNames.RandomLastName();

        // Skills
        EmployeeSkill[] skills = new EmployeeSkill[hiringMethod.SkillsDistribution.Length];
        for (int i = 0; i < hiringMethod.SkillsDistribution.Length; i++) {
            var skillDistribution = hiringMethod.SkillsDistribution[i];
            string skillId = skillDistribution.Item1;
            Skill skillInfo = skillsCollection.FindById(skillId);
            if (skillInfo == null) {
                Debug.LogError($"Staff.GenerateRandomEmployee : invalid skill ID {skillId} in Hiring Method of ID {hiringMethod.Id}");
                hiringCost = -1f;
                return null;
            }

            int proficiency = Random.Range(skillDistribution.Item2,
                skillDistribution.Item3); // upper bound is inclusive by convention
            skills[i] = new EmployeeSkill(skillId, skillInfo.Name, proficiency);
        }

        // Game Object Creation
        Employee employee = Instantiate(employeeModel);
        Employee generated = new Employee(firstName, lastName, 0, d, skills); // TODO : find other way
        employee.CopyEmployee(generated);
        employee.name = $"Employee_{generated.Id}";
        ec.SetCurrentEmployee(employee);

        // Hiring cost and salary
        hiringCost = ComputeEmployeeHiringCost(ec, d, c);
        employee.Salary = ComputeEmployeeSalary(ec, d, c);

        ec.SetCurrentEmployee(null);
        employee.gameObject.SetActive(true);
        return employee;
    }

    private float ComputeEmployeeHiringCost(EventsController ec, DateTime d,
        GameDevCompany c) {
        float hiringCost = 0f;
        foreach (SkillType skillType in skillTypes) {
            if (skillType == null) continue;
            hiringCost += skillType.HiringCost.Variable(ec, d, c);
        }
        return hiringCost;
    }

    public float ComputeEmployeeSalary(EventsController ec, DateTime d,
        GameDevCompany c) {
        float salary = 0f;
        foreach (SkillType skillType in skillTypes) {
            if (skillType == null) continue;
            salary += skillType.Salary.Variable(ec, d, c);
        }
        return salary;
    }
}
