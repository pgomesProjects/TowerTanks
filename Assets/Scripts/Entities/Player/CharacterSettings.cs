using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Character Settings")]
public class CharacterSettings : ScriptableObject
{
    [Header("Properties:")]
    [Tooltip("The maximum amount of health for a character.")] public float maxHealth = 100f;
    [Tooltip("The maximum amount of fuel for a character.")] public float fuelAmount = 100f;
    [Tooltip("The rate at which fuel depletes per second.")] public float fuelDepletionRate = 30f;
    [Tooltip("The rate at which fuel regenerates per second.")] public float fuelRegenerationRate = 20f;
}
