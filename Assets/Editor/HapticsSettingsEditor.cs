using UnityEngine;
using UnityEditor;
using TowerTanks.Scripts;

[CustomEditor(typeof(HapticsSettings))]
public class HapticsSettingsEditor : Editor
{
    private SerializedProperty hapticsTypeProp;

    private SerializedProperty leftMotorIntensityProp;
    private SerializedProperty rightMotorIntensityProp;
    private SerializedProperty durationProp;

    private SerializedProperty leftStartIntensityProp;
    private SerializedProperty leftEndIntensityProp;
    private SerializedProperty rightStartIntensityProp;
    private SerializedProperty rightEndIntensityProp;
    private SerializedProperty rampUpDurationProp;
    private SerializedProperty holdDurationProp;
    private SerializedProperty rampDownDurationProp;

    private void OnEnable()
    {
        // Cache the serialized properties
        hapticsTypeProp = serializedObject.FindProperty("hapticsType");

        leftMotorIntensityProp = serializedObject.FindProperty("leftMotorIntensity");
        rightMotorIntensityProp = serializedObject.FindProperty("rightMotorIntensity");
        durationProp = serializedObject.FindProperty("duration");

        leftStartIntensityProp = serializedObject.FindProperty("leftStartIntensity");
        leftEndIntensityProp = serializedObject.FindProperty("leftEndIntensity");
        rightStartIntensityProp = serializedObject.FindProperty("rightStartIntensity");
        rightEndIntensityProp = serializedObject.FindProperty("rightEndIntensity");
        rampUpDurationProp = serializedObject.FindProperty("rampUpDuration");
        holdDurationProp = serializedObject.FindProperty("holdDuration");
        rampDownDurationProp = serializedObject.FindProperty("rampDownDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(20);
        EditorGUILayout.PropertyField(hapticsTypeProp);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        HapticsType currentType = (HapticsType)hapticsTypeProp.enumValueIndex;

        //Show different settings based on the type of haptics event
        switch (currentType)
        {
            case HapticsType.STANDARD:
                EditorGUILayout.LabelField("Standard Haptics Settings", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                leftMotorIntensityProp.floatValue = EditorGUILayout.Slider("Left Motor Intensity", leftMotorIntensityProp.floatValue, 0f, 1f);
                rightMotorIntensityProp.floatValue = EditorGUILayout.Slider("Right Motor Intensity", rightMotorIntensityProp.floatValue, 0f, 1f);
                durationProp.floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Duration", durationProp.floatValue));
                break;

            case HapticsType.RAMPED:
                EditorGUILayout.LabelField("Ramped Haptics Settings", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Starting Intensity", EditorStyles.boldLabel);
                leftStartIntensityProp.floatValue = EditorGUILayout.Slider("Left Motor Intensity", leftMotorIntensityProp.floatValue, 0f, 1f);
                rightStartIntensityProp.floatValue = EditorGUILayout.Slider("Right Motor Intensity", rightStartIntensityProp.floatValue, 0f, 1f);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Ending Intensity", EditorStyles.boldLabel);
                leftEndIntensityProp.floatValue = EditorGUILayout.Slider("Left Motor Intensity", leftEndIntensityProp.floatValue, 0f, 1f);
                rightEndIntensityProp.floatValue = EditorGUILayout.Slider("Right Motor Intensity", rightEndIntensityProp.floatValue, 0f, 1f);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Ramp Settings", EditorStyles.boldLabel);
                rampUpDurationProp.floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Ramp Up Duration", rampUpDurationProp.floatValue));
                holdDurationProp.floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Ramp Hold Duration", holdDurationProp.floatValue));
                rampDownDurationProp.floatValue = Mathf.Max(0, EditorGUILayout.FloatField("Ramp Down Duration", rampDownDurationProp.floatValue));
                break;
        }

        // Apply modified properties to the serialized object
        serializedObject.ApplyModifiedProperties();
    }
}
