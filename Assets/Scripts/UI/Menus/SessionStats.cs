using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatsMenu")]
public class SessionStats : ScriptableObject
{
    public int maxHeight;

    public int wavesCleared;
    public int normalTanksDefeated;
    public int drillTanksDefeated;
    public int mortarTanksDefeated;

    public int numberOfCannons;
    public int numberOfEngines;
    public int numberOfDumpsters;
    public int numberOfThrottles;
}
