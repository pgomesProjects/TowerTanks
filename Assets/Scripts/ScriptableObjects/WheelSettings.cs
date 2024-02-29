using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/WheelSettings")]
public class WheelSettings : ScriptableObject
{
    [Header("Properties:")]
    [Tooltip("Maximum upward distance wheel can travel before it hits a hard stop."), Min(0)]                  public float maxSuspensionDepth;
    [Tooltip("Maximum speed at which wheels can spring to target position when uncompressed."), Min(0)]        public float maxSpringSpeed;
    [Tooltip("Maximum speed at which wheels can spring to target position when compressed."), Min(0)]          public float maxSqueezeSpeed;
    [Tooltip("Extra radius around wheel used to maintain ground status when wheel is decompressing."), Min(0)] public float groundDetectBuffer;
    [Space()]
    [Tooltip("How much force wheel suspension exerts to support tank."), Min(0)]            public float stiffness;
    [Tooltip("Curve representing suspension stiffness based on wheel compression amount.")] public AnimationCurve stiffnessCurve;
    [Header("Other Settings:")]
    [Tooltip("Causes wheel to generate a collider which prevents tank from squishing it into the ground once it's reached its compression limit.")] public bool generateWheelGuard = true;
    [Tooltip("Hides debug visualization meshes on wheels.")]                                                                                        public bool hideDebugs;
}
