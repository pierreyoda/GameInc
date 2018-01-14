using System;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField] private Expression<float> dailyProgress;
        public Expression<float> DailyProgress => dailyProgress;

        [SerializeField] private Expression<float> hiringCost;
        public Expression<float> HiringCost => hiringCost;

        [SerializeField] private Expression<float> salary;
        public Expression<float> Salary => salary;

        public SkillType(string id, Expression<float> dailyProgress,
            Expression<float> hiringCost, Expression<float> salary) {
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

    public void InitSkillsTypes(Database.Database.DatabaseCollection<Database.Skill> skills,
        List<LocalVariable> localVariables, List<GlobalVariable> globalVariables,
        List<IFunction> functions) {
        skillTypes = new SkillType[skills.Collection.Count];
        for (int i = 0; i < skills.Collection.Count; i++) {
            Database.Skill skill = skills.Collection[i];
            // daily progress
            var dailyProgress = ParseSkillExpression(localVariables, globalVariables,
                functions, skill.DailyProgress, "daily progress", skill.Id, false);
            if (dailyProgress == null) continue;
            // hiring cost
            var hiringCost = ParseSkillExpression(localVariables, globalVariables,
                functions, skill.HiringCost, "daily progress", skill.Id, true);
            if (hiringCost == null) continue;
            // salary
            var salary = ParseSkillExpression(localVariables, globalVariables,
                functions, skill.Salary, "daily progress", skill.Id, true);
            if (salary == null) continue;
            skillTypes[i] = new SkillType(skill.Id, dailyProgress, hiringCost, salary);
        }
    }

    private static Expression<float> ParseSkillExpression(List<LocalVariable> localVariables,
        List<GlobalVariable> globalVariables, List<IFunction> functions,
        string script, string label, string id, bool assignment) {
        IExpression expression = assignment ? Parser.ParseAssignment(
            script, localVariables, globalVariables, functions) : Parser.ParseExpression(
            script, localVariables, globalVariables, functions);
        if (expression == null) {
            Debug.LogError(
                $"Staff.InitSkillsTypes : parsing error in {label} for Skill with ID = {id}. Ignoring.");
            return null;
        }
        if (expression.Type() != SymbolType.Float) {
            Debug.LogError(
                $"Staff.InitSkillsTypes : {expression.Type()} {label} expression instead of {SymbolType.Float} for Skill with ID = {id}. Ignoring.");
            return null;
        }
        return expression as Expression<float>;
    }

    public Employee GenerateRandomEmployee(IScriptContext context,
        HiringMethod hiringMethod, Names commonNames,
        Database.Database.DatabaseCollection<Database.Skill> skillsCollection,
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
            Database.Skill skillInfo = skillsCollection.FindById(skillId);
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
            ISymbol result = skillType.HiringCost.EvaluateAsISymbol(context);
            if (result == null || result.Type() != SymbolType.Float) {
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
            ISymbol result = skillType.Salary.EvaluateAsISymbol(context);
            if (result == null || result.Type() != SymbolType.Float) {
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
            Symbol<float> result = skillType.DailyProgress.Evaluate(context);
            if (result == null) {
                Debug.LogError($"Staff : error while evaluating daily progress for Employee \"{employee.Id}\".");
            }
        }
        context.SetCurrentEmployee(null);
    }
}
