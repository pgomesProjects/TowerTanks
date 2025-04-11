using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "New Level Event", menuName = "Level Data/Level Event")]
    public class LevelEvents : ScriptableObject
    {
        [Tooltip("The name of the level.")] public string levelName;
        [Tooltip("The name of the objective.")] public string objectiveName;
        [Tooltip("The description of the level.")] public string levelDescription;
        [Tooltip("The interactables given at the start.")] public INTERACTABLE[] startingInteractables;

        [Tooltip("The frequency of enemies spawning.")] public Vector2 enemyFrequency;
        [Tooltip("The sub-objectives for the level.")] public SubObjectiveEvent[] subObjectives;

        [Tooltip("The amount of meters to travel.")] public float metersToTravel;
    }
}
