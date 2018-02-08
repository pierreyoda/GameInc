using System;
using UnityEngine;

namespace Database {

/// <summary>
/// An event occuring in the game World.
/// Supports a simple syntax for the trigger and action properties.
/// </summary>
[Serializable]
public class Event : DatabaseElement {
    [SerializeField] [MultilineAttribute] private string onInit;
    public string OnInit => onInit;

    [SerializeField] [MultilineAttribute] private string triggerCondition;
    public string TriggerCondition => triggerCondition;

    [SerializeField] [MultilineAttribute] private string triggerAction;
    public string TriggerAction => triggerAction;

    [SerializeField] [MultilineAttribute] private string triggerLimit;
    public string TriggerLimit => triggerLimit;

    public string TitleEnglish => Name;

    [SerializeField] [MultilineAttribute] private string descriptionEnglish;
    public string DescriptionEnglish => descriptionEnglish;

    public Event(string id, string name, string onInit, string triggerCondition,
        string triggerAction, string triggerLimit, string descriptionEnglish)
        : base(id, name) {
        this.onInit = onInit;
        this.triggerCondition = triggerCondition;
        this.triggerAction = triggerAction;
        this.triggerLimit = triggerLimit;
        this.descriptionEnglish = descriptionEnglish;
    }
}

}
