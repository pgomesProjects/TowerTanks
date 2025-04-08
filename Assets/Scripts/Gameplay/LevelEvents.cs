using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public enum ObjectiveType
    {
        TravelDistance,
        DefeatEnemies,
        SurviveForAmountOfTime
    }

    [CreateAssetMenu(fileName = "New Level Event", menuName = "Level Data / Level Event")]
    public class LevelEvents : ScriptableObject
    {
        public string levelName;
        public string levelDescription;
        public ObjectiveType objectiveType;
        [Tooltip("The number of rounds. Enter 0 for infinite rounds.")] public int numberOfRounds;
        public INTERACTABLE[] startingInteractables;

        //Travel Distance options
        public float metersToTravel;

        //Defeat Enemies options
        public int enemiesToDefeat;

        //Survive For Amount Of Time options
        public int secondsToSurviveFor;

        public string GetObjectiveName()
        {
            switch (objectiveType)
            {
                case ObjectiveType.DefeatEnemies:
                    return "Defeat Enemies!";

                case ObjectiveType.SurviveForAmountOfTime:
                    return "Keep Your Tank Alive!";

                case ObjectiveType.TravelDistance:
                    return "Reach The Objective!";
            }

            return "No Title Found";
        }
    }
}
