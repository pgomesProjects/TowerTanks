using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/RoomData", order = 1)]
public class RoomData : ScriptableObject
{
    [Header("Part Prefabs:")]
    [Tooltip("Reference to coupler object spawned when mounting rooms.")]                                             public GameObject couplerPrefab;
    [Tooltip("Reference to ladder object spawned when generating couplers.")]                                         public GameObject ladderPrefab;
    [Tooltip("Reference to short ladder object (for separators) spawned when generating couplers.")]                  public GameObject shortLadderPrefab;
    [Tooltip("Reference to platform object spawned when generating sections of tank which players need to walk on.")] public GameObject platformPrefab;
    [Tooltip("Reference to indicator object which shows up on rooms with open interactable slots.")]                  public GameObject slotIndicator;

    [Header("Color Palettes:")]
    [Tooltip("Colors for room types (respective of room type listing).")] public Color[] roomTypeColors;
}