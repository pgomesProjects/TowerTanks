using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TowerTanks.Scripts;

[CustomEditor(typeof(ScreenshakeSettings))]
public class ScreenshakeSettingsEditor : Editor
{
    private SerializedProperty intensityProp;
    private SerializedProperty durationProp;

    private void OnEnable()
    {
        // Cache the serialized properties
        intensityProp = serializedObject.FindProperty("intensity");
        durationProp = serializedObject.FindProperty("duration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //Display the screenshake settings (includes tooltips)
        intensityProp.floatValue = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Intensity", "The intensity of the screenshake."), intensityProp.floatValue));
        durationProp.floatValue = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Duration", "The duration of the screenshake."), durationProp.floatValue));

        serializedObject.ApplyModifiedProperties();
    }
}
