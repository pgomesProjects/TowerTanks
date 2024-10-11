using UnityEngine;
using UnityEditor;
using TowerTanks.Scripts;

[CustomEditor(typeof(ButtonPromptSettings))]
public class ButtonPromptSettingsEditor : Editor
{
    private SerializedProperty buttonPromptsProp;

    private void OnEnable()
    {
        // Cache the serialized properties
        buttonPromptsProp = serializedObject.FindProperty("buttonPrompts");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the ButtonPrompt list
        for (int i = 0; i < buttonPromptsProp.arraySize; i++)
        {
            SerializedProperty buttonPromptProp = buttonPromptsProp.GetArrayElementAtIndex(i);
            SerializedProperty actionProp = buttonPromptProp.FindPropertyRelative("action");
            SerializedProperty promptsProp = buttonPromptProp.FindPropertyRelative("prompts");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Action", GUILayout.Width(100));
            EditorGUILayout.LabelField(actionProp.enumDisplayNames[actionProp.enumValueIndex], GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            // Draw the list of platform prompts
            for (int j = 0; j < promptsProp.arraySize; j++)
            {
                SerializedProperty platformPromptProp = promptsProp.GetArrayElementAtIndex(j);
                SerializedProperty platformProp = platformPromptProp.FindPropertyRelative("platform");
                SerializedProperty promptProp = platformPromptProp.FindPropertyRelative("prompt");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(platformProp.enumDisplayNames[platformProp.enumValueIndex], GUILayout.Width(100));
                promptProp.stringValue = EditorGUILayout.TextField(promptProp.stringValue, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }

        // Apply modified properties to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}