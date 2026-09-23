using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        return -EditorGUIUtility.standardVerticalSpacing;
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;
        
        // Find the conditional property relative to this field
        SerializedProperty conditionProperty = property.serializedObject.FindProperty(showIf.ConditionName);

        if (conditionProperty == null)
        {
            Debug.LogWarning($"ShowIf: Field '{showIf.ConditionName}' not found on {property.serializedObject.targetObject.name}.");
            return true;
        }

        switch (conditionProperty.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return conditionProperty.boolValue.Equals(showIf.ExpectedValue);

            case SerializedPropertyType.Enum:
                // Check if the enum value matches by integer value or string representation
                if (showIf.ExpectedValue is int intVal)
                {
                    return conditionProperty.enumValueIndex == intVal;
                }
                
                if (showIf.ExpectedValue != null)
                {
                    string enumName = conditionProperty.enumNames[conditionProperty.enumValueIndex];
                    return enumName.Equals(showIf.ExpectedValue.ToString());
                }
                return false;

            default:
                Debug.LogWarning($"ShowIf: Unsupported property type '{conditionProperty.propertyType}'.");
                return true;
        }
    }
}