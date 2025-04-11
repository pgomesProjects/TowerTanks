using UnityEngine;
using UnityEditor;
using TowerTanks.Scripts;

[CustomEditor(typeof(SubObjectiveEvent))]
public class SubObjectiveEventEditor : Editor
{
    //Properties for LevelEvents ScriptableObjects
    private SerializedProperty objectiveNameProperty;
    private SerializedProperty objectiveTypeProperty;
    private SerializedProperty metersToTravelProperty;
    private SerializedProperty enemiesToDefeatProperty;
    private SerializedProperty secondsToSurviveForProperty;

    private void OnEnable()
    {
        //Assign serialized property to a part of the ScriptableObject
        objectiveNameProperty = serializedObject.FindProperty("objectiveName");
        objectiveTypeProperty = serializedObject.FindProperty("objectiveType");
        metersToTravelProperty = serializedObject.FindProperty("metersToTravel");
        enemiesToDefeatProperty = serializedObject.FindProperty("enemiesToDefeat");
        secondsToSurviveForProperty = serializedObject.FindProperty("secondsToSurviveFor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Sub-Objective Information", EditorStyles.boldLabel);
        //Objective description
        EditorGUILayout.PropertyField(objectiveNameProperty);

        EditorGUILayout.Space();

        //Objective types
        EditorGUILayout.LabelField("Objective", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(objectiveTypeProperty);

        EditorGUILayout.Space();

        ObjectiveType selectedObjectiveType = (ObjectiveType)objectiveTypeProperty.enumValueIndex;

        //Depending on the type of objective selected, show the options for that objective type
        switch (selectedObjectiveType)
        {
            case ObjectiveType.DefeatEnemies:
                DrawDefeatEnemiesOptions();
                break;
            case ObjectiveType.SurviveForAmountOfTime:
                DrawSurviveForAmountOfTimeOptions();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefeatEnemiesOptions()
    {
        EditorGUILayout.LabelField("Defeat Enemies Options", EditorStyles.boldLabel);
        enemiesToDefeatProperty.intValue = EditorGUILayout.IntField("Enemies to Defeat", enemiesToDefeatProperty.intValue);
    }

    private void DrawSurviveForAmountOfTimeOptions()
    {
        EditorGUILayout.LabelField("Survive For Amount Of Time Options", EditorStyles.boldLabel);
        secondsToSurviveForProperty.intValue = EditorGUILayout.IntField("Seconds To Survive For", secondsToSurviveForProperty.intValue);
    }
}