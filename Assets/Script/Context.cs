using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script {

public interface IScriptContext {
    List<IFunction> Functions();
    List<LocalVariable> LocalVariables();
    bool SetGlobalVariable(string name, ISymbol value);

    DateTime D();
    GameDevCompany C();

    Employee CurrentEmployee();
    void SetCurrentEmployee(Employee employee);
}

public abstract class ScriptContext {
    public static bool AddLocalVariable(IScriptContext context, string name,
        ISymbol value, bool mutable) {
        List<LocalVariable> localVariables = context.LocalVariables();
        if (localVariables.Find(lv => lv.Name == name) != null) {
            Debug.LogError(
                $"ScriptContext.AddVariable : \"{name}\" already exists.");
            return false;
        }
        LocalVariable localVariable = new LocalVariable(name, value, mutable);
        localVariables.Add(localVariable);
        return true;
    }

    public static bool SetLocalVariableValue(IScriptContext context, string name,
        ISymbol value) {
        LocalVariable localVariable = context.LocalVariables().Find(lv => lv.Name == name);
        if (localVariable == null) {
            Debug.LogError(
                $"ScriptContext.SetLocalVariableValue(\"{name}\", {value.ValueString()}) : local variable not found.");
            return false;
        }
        if (localVariable.Type != value.Type()) {
            Debug.LogError(
                $"ScriptContext.SetLocalVariableValue(\"{name}\", {value.ValueString()}) : wrong type ({value.Type()} instead of {localVariable.Type}).");
            return false;
        }
        localVariable.Value = value;
        return true;
    }

    public static ISymbol GetLocalVariableValue(IScriptContext context, string name) {
        LocalVariable localVariable = context.LocalVariables().Find(lv => lv.Name == name);
        if (localVariable == null) {
            Debug.LogError(
                $"ScriptContext.GetLocalVariableValue(\"{name}\") : local variable not found.");
            return null;
        }
        return localVariable.Value;
    }
}

public class ParserContext {
    public Grammar Grammar;
    public List<LocalVariable> LocalVariables;
    public List<GlobalVariable> GlobalVariables;
    public List<IFunction> Functions;
}

}