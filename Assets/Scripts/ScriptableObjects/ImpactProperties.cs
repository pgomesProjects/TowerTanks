using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ImpactProperties")]
    public class ImpactProperties : ScriptableObject
    {
        [Header("Base Values:")]
        [Tooltip("Duration of the impact event (setting to zero will result in an instantaneous force)."), Min(0)]                                          public float phase;
        [Tooltip("Standard impulse force applied by the event (when intensityCurve v = 1)."), Min(0)]                                                       public float baseAmplitude;
        [Tooltip("Maximum amplitude which can be added by multiplier factors, prevents really crazy stuff."), Min(0)]                                       public float maxAmplitudeGain = 0;
        [Tooltip("Multiplies base amplitude throughout duration of impact event."), ShowIf("phase")]                                                        public AnimationCurve intensityCurve;
        [Tooltip("Multiplier determining the effect of speed on magnitude of impact (a value of 1 means double the speed doubles the magnitude)."), Min(0)] public float speedFactor = 0;
        [Header("Advanced Properties:")]
        [Tooltip("Amount of force applied to physically push tank backward (reduces sticking power of treads proportionally to amplitude)."), ShowIf("phase")] public float pureKnockbackAmplitude = 0;
    }
}
