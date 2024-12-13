using UnityEngine;
using UnityEditor;
using TowerTanks.Scripts;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Linq;

[CustomEditor(typeof(ButtonPromptSettings))]
public class ButtonPromptSettingsEditor : Editor
{
    private SerializedProperty buttonPromptsProp;
    private bool newActionPromptsActive;
    private int selectedUnusedActionIndex;

    private void OnEnable()
    {
        // Cache the serialized properties
        buttonPromptsProp = serializedObject.FindProperty("buttonPrompts");
        newActionPromptsActive = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw the ButtonPrompt list
        for (int i = 0; i < buttonPromptsProp.arraySize; i++)
        {
            SerializedProperty buttonPromptProp = buttonPromptsProp.GetArrayElementAtIndex(i);
            SerializedProperty actionProp = buttonPromptProp.FindPropertyRelative("action");
            SerializedProperty actionTypeProp = buttonPromptProp.FindPropertyRelative("actionType");
            SerializedProperty promptsProp = buttonPromptProp.FindPropertyRelative("prompts");

            GUIStyle boldItalicStyle = new GUIStyle(EditorStyles.label);
            boldItalicStyle.fontStyle = FontStyle.BoldAndItalic;
            GUIStyle italicStyle = new GUIStyle(EditorStyles.label);
            italicStyle.fontStyle = FontStyle.Italic;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField(actionProp.enumDisplayNames[actionProp.enumValueIndex], boldItalicStyle, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type", EditorStyles.label, GUILayout.Width(100));
            actionTypeProp.intValue = (int)(ActionType)EditorGUILayout.EnumPopup((ActionType)actionTypeProp.intValue, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("---------------------------------------------------------------------");

            // Draw the list of platform prompts
            for (int j = 0; j < promptsProp.arraySize; j++)
            {
                SerializedProperty platformPromptProp = promptsProp.GetArrayElementAtIndex(j);
                SerializedProperty platformProp = platformPromptProp.FindPropertyRelative("platform");
                SerializedProperty promptInfoProp = platformPromptProp.FindPropertyRelative("promptInfo");

                EditorGUILayout.BeginHorizontal();
                string platformName = platformProp.enumDisplayNames[platformProp.enumValueIndex];
                EditorGUILayout.LabelField(platformName.Replace(" ", ""), italicStyle, GUILayout.Width(100));
                promptInfoProp.objectReferenceValue = (PromptInfo)EditorGUILayout.ObjectField(promptInfoProp.objectReferenceValue, typeof(PromptInfo), false, GUILayout.Width(400));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Remove Action", GUILayout.Width(600)))
                buttonPromptsProp.DeleteArrayElementAtIndex(i);

            EditorGUILayout.Space();

            //Draw a line at the end of every section except for the last
            if (i < buttonPromptsProp.arraySize - 1)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        if(buttonPromptsProp.arraySize != Enum.GetValues(typeof(GameAction)).Length)
        {
            if (!newActionPromptsActive)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();

                if (GUILayout.Button("Add New Action"))
                {
                    newActionPromptsActive = true;
                }
            }
            else
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();

                List<GameAction> unusedActions = new List<GameAction>();

                GameAction[] allGameActions = (GameAction[])Enum.GetValues(typeof(GameAction));
                List<ButtonPrompt> allPrompts = new List<ButtonPrompt>();

                //Recreate all existing prompts
                for (int i = 0; i < buttonPromptsProp.arraySize; i++)
                {
                    SerializedProperty buttonPromptProp = buttonPromptsProp.GetArrayElementAtIndex(i);
                    SerializedProperty actionProp = buttonPromptProp.FindPropertyRelative("action");

                    ButtonPrompt buttonPrompt = new ButtonPrompt((GameAction)actionProp.enumValueIndex);
                    allPrompts.Add(buttonPrompt);
                }

                //Get a hashset that has all used game actions
                HashSet<GameAction> usedGameActions = new HashSet<GameAction>(allPrompts.Select(bp => bp.action));

                //If there are game actions not found, add them to a list
                foreach (GameAction action in allGameActions)
                    if (!usedGameActions.Contains(action))
                        unusedActions.Add(action);

                string[] unusedActionNames = unusedActions.Select(a => a.ToString()).ToArray();
                selectedUnusedActionIndex = EditorGUILayout.Popup("Add New Action", selectedUnusedActionIndex, unusedActionNames);

                EditorGUILayout.Space();

                if (GUILayout.Button("Add Selected Action"))
                {
                    buttonPromptsProp.arraySize++;

                    SerializedProperty newAction = buttonPromptsProp.GetArrayElementAtIndex(buttonPromptsProp.arraySize - 1);

                    newAction.FindPropertyRelative("action").enumValueIndex = (int)unusedActions[selectedUnusedActionIndex];
                    newAction.FindPropertyRelative("actionType").enumValueIndex = 0;

                    SerializedProperty promptsProp = newAction.FindPropertyRelative("prompts");
                    for (int i = 0; i < promptsProp.arraySize; i++)
                    {
                        SerializedProperty platformPromptProp = promptsProp.GetArrayElementAtIndex(i);
                        //platformPromptProp.FindPropertyRelative("spriteID").intValue = 0;
                        //platformPromptProp.FindPropertyRelative("promptSprite").objectReferenceValue = null;
                        //platformPromptProp.FindPropertyRelative("promptText").stringValue = "";
                    }

                    selectedUnusedActionIndex = 0;
                    newActionPromptsActive = false;
                }
            }
        }

        // Apply modified properties to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}