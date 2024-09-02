using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(LevelEvents))]
public class LevelEventObjectEditor : Editor
{
    //Properties for LevelEvents ScriptableObjects
    private SerializedProperty levelNameProperty;
    private SerializedProperty levelDescriptionProperty;
    private SerializedProperty objectiveTypeProperty;
    private SerializedProperty numberOfRoundsProperty;
    private SerializedProperty startingInteractables;
    private SerializedProperty metersToTravelProperty;
    private SerializedProperty enemiesToDefeatProperty;
    private SerializedProperty secondsToSurviveForProperty;

    private void OnEnable()
    {
        //Assign serialized property to a part of the ScriptableObject
        levelNameProperty = serializedObject.FindProperty("levelName");
        levelDescriptionProperty = serializedObject.FindProperty("levelDescription");
        objectiveTypeProperty = serializedObject.FindProperty("objectiveType");
        numberOfRoundsProperty = serializedObject.FindProperty("numberOfRounds");
        startingInteractables = serializedObject.FindProperty("startingInteractables");
        metersToTravelProperty = serializedObject.FindProperty("metersToTravel");
        enemiesToDefeatProperty = serializedObject.FindProperty("enemiesToDefeat");
        secondsToSurviveForProperty = serializedObject.FindProperty("secondsToSurviveFor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //Level name
        EditorGUILayout.LabelField("Level Information", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(levelNameProperty);

        //Number of rounds in the campaign
        EditorGUILayout.PropertyField(numberOfRoundsProperty);

        //Starting interactables
        EditorGUILayout.PropertyField(startingInteractables);

        //Level description (for campaign mode, not in-game)
        EditorGUILayout.LabelField("Description");
        levelDescriptionProperty.stringValue = EditorGUILayout.TextArea(levelDescriptionProperty.stringValue, EditorStyles.textArea, GUILayout.Height(80));

        EditorGUILayout.Space();

        //Objective types
        EditorGUILayout.LabelField("Objective", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(objectiveTypeProperty);

        EditorGUILayout.Space();

        ObjectiveType selectedObjectiveType = (ObjectiveType)objectiveTypeProperty.enumValueIndex;

        //Depending on the type of objective selected, show the options for that objective type
        switch (selectedObjectiveType)
        {
            case ObjectiveType.TravelDistance:
                DrawTravelDistanceOptions();
                break;
            case ObjectiveType.DefeatEnemies:
                DrawDefeatEnemiesOptions();
                break;
            case ObjectiveType.SurviveForAmountOfTime:
                DrawSurviveForAmountOfTimeOptions();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTravelDistanceOptions()
    {
        EditorGUILayout.LabelField("Travel Distance Options", EditorStyles.boldLabel);
        metersToTravelProperty.floatValue = EditorGUILayout.FloatField("Meters to Travel", metersToTravelProperty.floatValue);
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
