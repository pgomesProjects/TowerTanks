using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public enum ObjectiveType
    {
        DefeatEnemies,
        SurviveForAmountOfTime
    }

    [CreateAssetMenu(fileName = "New SubObjective Event", menuName = "Level Data / Sub-Objective Event")]
    public class SubObjectiveEvent : ScriptableObject
    {
        [Tooltip("The name of the sub-objective.")] public string objectiveName;
        [Tooltip("The type of objective.")] public ObjectiveType objectiveType;

        //Defeat Enemies options
        [Tooltip("The number of enemies to defeat.")] public int enemiesToDefeat;
        //Survive For Amount Of Time options
        [Tooltip("The amount of time to survive for (in seconds).")] public int secondsToSurviveFor;
    }
}
