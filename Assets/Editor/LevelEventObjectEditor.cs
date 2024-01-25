using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(LevelEvents))]
public class LevelEventObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //Get property values for the ScriptableObject
        SerializedProperty levelNameProperty = serializedObject.FindProperty("levelName");
        SerializedProperty levelDescriptionProperty = serializedObject.FindProperty("levelDescription");

        //Label
        EditorGUILayout.LabelField("Level Information", EditorStyles.boldLabel);

        //Level information
        EditorGUILayout.PropertyField(levelNameProperty);
        EditorGUILayout.LabelField("Description");
        levelDescriptionProperty.stringValue = EditorGUILayout.TextArea(levelDescriptionProperty.stringValue, EditorStyles.textArea, GUILayout.Height(80));

        serializedObject.ApplyModifiedProperties();
    }
}
