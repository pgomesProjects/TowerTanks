using UnityEngine;

namespace TowerTanks.Scripts
{
    public enum HapticsType { STANDARD, RAMPED }

    [CreateAssetMenu(fileName = "New Haptics Settings", menuName = "ScriptableObjects/Haptics Settings")]
    public class HapticsSettings : ScriptableObject
    {
        [Tooltip("The type of haptics to use.")] public HapticsType hapticsType;

        //Standard haptics settings
        [Tooltip("The left motor haptics intensity.")] public float leftMotorIntensity;
        [Tooltip("The right motor haptics intensity.")] public float rightMotorIntensity;
        [Tooltip("The duration of the haptics event.")] public float duration;

        //Ramped haptics settings
        [Tooltip("The starting left motor haptics intensity.")] public float leftStartIntensity;
        [Tooltip("The ending left motor haptics intensity.")] public float leftEndIntensity;
        [Tooltip("The starting right motor haptics intensity.")] public float rightStartIntensity;
        [Tooltip("The ending right motor haptics intensity.")] public float rightEndIntensity;
        [Tooltip("The duration it takes for the motors to go from the starting intensity to the ending intensity.")] public float rampUpDuration;
        [Tooltip("The duration it takes to keep the ending intensity for.")] public float holdDuration;
        [Tooltip("The duration it takes for the intensity to reset to zero.")] public float rampDownDuration;
    }
}
