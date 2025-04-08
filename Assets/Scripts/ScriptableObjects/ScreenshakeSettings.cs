using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "New Screenshake Settings", menuName = "ScriptableObjects/Screenshake Settings")]
    public class ScreenshakeSettings : ScriptableObject
    {
        [Tooltip("The intensity of the screenshake.")] public float intensity;
        [Tooltip("The duration of the screenshake.")] public float duration;
    }
}
