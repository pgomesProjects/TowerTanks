using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TankAISettings", menuName = "ScriptableObjects/TankAISettings", order = 1)]
public class TankAISettings : ScriptableObject
{
    [Tooltip("The distance at which this tank can see other tanks. Once an opposing tank is closer than this value " +
             "in units, this tank goes into pursuit mode, latching that tank as it's target.")]
    public float viewRange;
    [Tooltip("Should be less than view range. Once an opposing tank is closer than this value in units, this tank " +
             "goes into engage mode, real fight begins, hell breaks loose")]
    public float engagementRange;
    
    [Tooltip("Once in engagement state, the tank will try and maintain this much distance from the player while in battle. Can be overridden for special moves like charging.")]
    public float defaultFightingDistance;

    [Tooltip("The max amount of tokens that this tank can have, and the amount of tokens that this tank starts with")]
    public int tankEconomy;

}
