using System;
using UnityEngine;
using UnityEngine.Assertions;

public class Employee : MonoBehaviour {
    private static int INSTANCES_COUNT = 0;

    [SerializeField] private int id;
    public int Id => id;

    [SerializeField] private string firstName;
    public string FirstName => firstName;

    [SerializeField] private string lastName;
    public string LastName => lastName;

    [SerializeField] private float salary;
    public float Salary => salary;

    [SerializeField] private DateTime hireDate;
    public DateTime HireDate => hireDate;

    [SerializeField] private Skill[] skills;
    public Skill[] Skills => skills;

    [SerializeField] private Need vacationNeed = new Need("Vacations", 0);
    public Need VacationNeed => vacationNeed;

    [SerializeField] private Need relaxationNeed = new Need("Relaxation", 0);
    public Need RelaxationNeed => relaxationNeed;

    public Employee(string firstName, string lastName, float salary,
        DateTime hireDate, Skill[] skills) {
        id = INSTANCES_COUNT++;
        this.firstName = firstName;
        this.lastName = lastName;
        this.salary = salary;
        this.hireDate = hireDate;
        this.skills = skills;
    }

    private void Start() {
        Assert.IsTrue(salary >= 0);
    }
}
