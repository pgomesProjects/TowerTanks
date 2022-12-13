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

    public int numberOfCannons;
    public int numberOfAmmoCrates;
    public int numberOfEngines;
    public int numberOfThrottles;
}
