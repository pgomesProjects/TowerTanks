using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/RoomData", order = 1)]
public class RoomData : ScriptableObject
{
    //Data:
    [Header("Template Settings:")]
    [Tooltip("Width of couplers, used to check for where couplers can be placed between rooms.")] public float couplerWidth;

    [Header("Part Prefabs:")]
    [Tooltip("Reference to coupler object spawned when mounting rooms.")]                                             public GameObject couplerPrefab;
    [Tooltip("Reference to ladder object spawned when generating couplers.")]                                         public GameObject ladderPrefab;
    [Tooltip("Reference to short ladder object (for separators) spawned when generating couplers.")]                  public GameObject shortLadderPrefab;
    [Tooltip("Reference to platform object spawned when generating sections of tank which players need to walk on.")] public GameObject platformPrefab;
    [Tooltip("Reference to indicator object which shows up on rooms with open interactable slots.")]                  public GameObject slotIndicator;
    [Tooltip("Physics Material to use when making a DummyRoom Object.")]                                              public PhysicsMaterial2D dummyMaterial;
    [Space()]   
    [Tooltip("List of all interactables which can be installed in tank.")] public GameObject[] interactableList;

    [Header("Color Palettes:")]
    [Tooltip("Colors for room types (respective of room type listing).")] public Color[] roomTypeColors;
}