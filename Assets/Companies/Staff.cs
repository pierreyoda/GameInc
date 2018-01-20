using System;
using System.Collections.Generic;
using Database;
using NUnit.Framework;
using Script;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Provides staff management utilities for a GameDevCompany to use.
/// </summary>
public class Staff : MonoBehaviour {
    private class SkillType {
        [SerializeField] private string id;
        public string Id => id;

        [SerializeField] private Executable dailyProgress;
        public Executable DailyProgress => dailyProgress;

        [SerializeField] private Executable hiringCost;
        public Executable HiringCost => hiringCost;

        [SerializeField] private Executable salary;
        public Executable Salary => salary;

        public SkillType(string id, Executable dailyProgress,
            Executable hiringCost, Executable salary) {
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

    public void InitSkillsTypes(Database.Database.DatabaseCollection<Skill> skills,
        ParserContext parserContext) {
        skillTypes = new SkillType[skills.Collection.Count];
        for (int i = 0; i < skills.Collection.Count; i++) {
            Skill skill = skills.Collection[i];
            // daily progress
            var dailyProgress = ParseSkillScript(skill.DailyProgress,
                "daily progress", skill.Id, parserContext);
            if (dailyProgress == null) continue;
            // hiring cost
            var hiringCost = ParseSkillScript(skill.HiringCost,
                "hiring cost", skill.Id, parserContext);
            if (hiringCost == null) continue;
            // salary
            var salary = ParseSkillScript(skill.Salary,
                "salary", skill.Id, parserContext);
            if (salary == null) continue;

            skillTypes[i] = new SkillType(skill.Id, dailyProgress, hiringCost, salary);
        }
    }

    private static Executable ParseSkillScript(string script, string label,
        string id, ParserContext parserContext) {
        Executable skillScript = Executable.FromScript(script, parserContext);
        if (skillScript == null) {
            Debug.LogError(
                $"Staff.InitSkillsTypes : parsing error in {label} for Skill with ID = {id}. Ignoring.");
            return null;
        }
        return skillScript;
    }

    public Employee GenerateRandomEmployee(IScriptContext context,
        HiringMethod hiringMethod, Names commonNames, Database.Database.DatabaseCollection<Skill> skillsCollection,
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
                Debug.LogError($"Staff.GenerateRandomEmployee : invalid skill ID {skillId} in Hiring Method of ID {hiringMethod.Id}.");
                hiringCost = -1f;
                return null;
            }

            int proficiency = Random.Range(skillDistribution.Item2,
                skillDistribution.Item3); // upper bound is inclusive by convention
            skills[i] = new EmployeeSkill(skillId, skillInfo.Name, proficiency);
        }

        // Game Object Creation
        Employee employee = Instantiate(employeeModel);
        Employee generated = new Employee(firstName, lastName, 0, context.D(), skills); // TODO : find other way
        employee.CopyEmployee(generated);
        employee.name = $"Employee_{generated.Id}";
        context.SetCurrentEmployee(employee);

        // Hiring cost and salary
        hiringCost = ComputeEmployeeHiringCost(context);
        employee.Salary = ComputeEmployeeSalary(context);

        context.SetCurrentEmployee(null);
        employee.gameObject.SetActive(true);
        return employee;
    }

    private float ComputeEmployeeHiringCost(IScriptContext context) {
        Assert.IsTrue(ScriptContext.SetLocalVariableValue(context,
            "Employee.HiringCost", new FloatSymbol(0f)));
        foreach (SkillType skillType in skillTypes) {
            if (skillType == null) continue;
            if (skillType.HiringCost.Execute(context) == null) {
                Debug.LogError($"Staff : error while evaluating hiring cost for Skill \"{skillType.Id}\".");
                return 0f;
            }
        }
        Symbol<float> hiringCost = ScriptContext.GetLocalVariableValue(context,
            "Employee.HiringCost") as Symbol<float>;
        Assert.IsNotNull(hiringCost);
        return hiringCost.Value;
    }

    private float ComputeEmployeeSalary(IScriptContext context) {
        Assert.IsTrue(ScriptContext.SetLocalVariableValue(context,
            "Employee.Salary", new FloatSymbol(0f)));
        foreach (SkillType skillType in skillTypes) {
            if (skillType.Salary.Execute(context) == null) {
                Debug.LogError($"Staff : error while evaluating salary for Skill \"{skillType.Id}\".");
                return 0f;
            }
        }
        Symbol<float> salary = ScriptContext.GetLocalVariableValue(context,
            "Employee.HiringCost") as Symbol<float>;
        Assert.IsNotNull(salary);
        return salary.Value;
    }

    public void ApplyDayProgress(Employee employee, IScriptContext context) {
        context.SetCurrentEmployee(employee);
        foreach (EmployeeSkill employeeSkill in employee.EmployeeSkills) {
            SkillType skillType = Array.Find(skillTypes,
                type => type.Id == employeeSkill.Id);
            Assert.IsNotNull(skillType);
            if (skillType.DailyProgress.Execute(context) == null) {
                Debug.LogError($"Staff : error while evaluating daily progress for Employee \"{employee.Id}\".");
                context.SetCurrentEmployee(null);
                return;
            }
        }
        context.SetCurrentEmployee(null);
    }
}
