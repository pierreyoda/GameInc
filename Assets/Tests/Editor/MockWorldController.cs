using System;
using System.Collections.Generic;
using Script;

public class MockWorldController : IScriptContext {
	private List<IFunction> functions = Function<bool>.DefaultFunctions();
	private List<LocalVariable> localVariables = new List<LocalVariable>();
	private List<GlobalVariable> globalVariables = new List<GlobalVariable>();

	private DateTime date = new DateTime(1980, 1, 1);
	private Employee currentEmployee = null;

	public int LoopsMaximumIterations() => 100;
	public List<IFunction> Functions() => functions;
	public List<LocalVariable> LocalVariables() => localVariables;
	public List<GlobalVariable> GlobalVariables() => globalVariables;

	public bool SetGlobalVariable(string name, ISymbol value) {
		throw new NotImplementedException();
	}

	public DateTime D() => date;
	public GameDevCompany C() => null;

	public Employee CurrentEmployee() => currentEmployee;
	public void SetCurrentEmployee(Employee employee) {
		currentEmployee = employee;
	}
}
