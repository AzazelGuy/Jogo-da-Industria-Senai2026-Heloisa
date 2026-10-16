using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ShowIfAttribute : PropertyAttribute
{
    public string ConditionName { get; }
    public object ExpectedValue { get; }

    // Overload for booleans
    public ShowIfAttribute(string conditionName, bool expectedValue = true)
    {
        ConditionName = conditionName;
        ExpectedValue = expectedValue;
    }

    // Overload for enums or other values
    public ShowIfAttribute(string conditionName, object expectedValue)
    {
        ConditionName = conditionName;
        ExpectedValue = expectedValue;
    }
}