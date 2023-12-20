using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/RoomData", order = 1)]
public class RoomData : ScriptableObject
{
    [Header("Part Prefabs:")]
    [Tooltip("Reference to coupler object spawned when mounting rooms.")]                            public GameObject couplerPrefab;
    [Tooltip("Reference to ladder object spawned when generating couplers.")]                        public GameObject ladderPrefab;
    [Tooltip("Reference to short ladder object (for separators) spawned when generating couplers.")] public GameObject shortLadderPrefab;
}