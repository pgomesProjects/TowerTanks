using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    //Travel Distance options
    public float metersToTravel;

    //Defeat Enemies options
    public int enemiesToDefeat;

    //Survive For Amount Of Time options
    public int secondsToSurviveFor;
}
