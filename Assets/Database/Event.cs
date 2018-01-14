using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Database {

/// <summary>
/// An event occuring in the game World.
/// Supports a simple syntax for the trigger and action properties.
/// </summary>
[Serializable]
public class Event : DatabaseElement {
    [SerializeField] private string[] triggerConditions;
    public string[] TriggerConditions => triggerConditions;

    [SerializeField] private string[] triggerActions;
    public string[] TriggerActions => triggerActions;

    [SerializeField] private string triggerLimit;
    public string TriggerLimit => triggerLimit;

    public string TitleEnglish => Name;

    [SerializeField] [MultilineAttribute] private string descriptionEnglish;
    public string DescriptionEnglish => descriptionEnglish;

    [SerializeField] private string[] variables;
    public string[] VariablesDeclarations => variables;

    public Event(string id, string name, string[] triggerConditions,
        string[] triggerActions, string triggerLimit, string[] variables,
        string descriptionEnglish) : base(id, name) {
        this.triggerConditions = triggerConditions;
        this.triggerActions = triggerActions;
        this.triggerLimit = triggerLimit;
        this.descriptionEnglish = descriptionEnglish;
        this.variables = variables;
    }
}

}
