using UnityEngine;
using UnityEditor;
using TowerTanks.Scripts;

[CustomEditor(typeof(LevelEvents))]
public class LevelEventObjectEditor : Editor
{
    //Properties for LevelEvents ScriptableObjects
    private SerializedProperty levelNameProperty;
    private SerializedProperty objectiveNameProperty;
    private SerializedProperty levelDescriptionProperty;
    private SerializedProperty startingInteractables;
    private SerializedProperty metersToTravelProperty;
    private SerializedProperty enemyFrequencyProperty;
    private SerializedProperty subObjectivesProperty;

    private void OnEnable()
    {
        //Assign serialized property to a part of the ScriptableObject
        levelNameProperty = serializedObject.FindProperty("levelName");
        objectiveNameProperty = serializedObject.FindProperty("objectiveName");
        levelDescriptionProperty = serializedObject.FindProperty("levelDescription");
        startingInteractables = serializedObject.FindProperty("startingInteractables");
        metersToTravelProperty = serializedObject.FindProperty("metersToTravel");
        enemyFrequencyProperty = serializedObject.FindProperty("enemyFrequency");
        subObjectivesProperty = serializedObject.FindProperty("subObjectives");

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //Level name
        EditorGUILayout.LabelField("Level Information", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(levelNameProperty);

        //Starting interactables
        EditorGUILayout.PropertyField(startingInteractables);

        //Level description
        EditorGUILayout.PropertyField(objectiveNameProperty);
        EditorGUILayout.LabelField("Description");
        levelDescriptionProperty.stringValue = EditorGUILayout.TextArea(levelDescriptionProperty.stringValue, EditorStyles.textArea, GUILayout.Height(80));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemy Frequency", EditorStyles.boldLabel);
        enemyFrequencyProperty.vector2Value = EditorGUILayout.Vector2Field("", enemyFrequencyProperty.vector2Value);
        Vector2 enemyFrequencyVal = enemyFrequencyProperty.vector2Value;
        enemyFrequencyVal.x = Mathf.Max(enemyFrequencyVal.x, 100f);
        enemyFrequencyVal.y = Mathf.Max(enemyFrequencyVal.y, enemyFrequencyVal.x);
        enemyFrequencyProperty.vector2Value = enemyFrequencyVal;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Travel Distance Options", EditorStyles.boldLabel);
        metersToTravelProperty.floatValue = EditorGUILayout.FloatField("Meters to Travel", metersToTravelProperty.floatValue);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(subObjectivesProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
