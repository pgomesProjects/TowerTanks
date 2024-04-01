using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class EventSpawnerManager : MonoBehaviour
{
    public enum EventType { 
        ENEMY, //Spawns an entity that can directly harm the tank (Tanks, Outposts, etc)
        OBSTACLE, //Gets in players way but doesn't directly harm the tank (Props, Terrain, etc)
        WEATHER, //Changes the weather (Rain, Thunderstorm, Wind, Fog, Sandstorm, etc)
        FRIENDLY, //Events that benefit the players, mostly used for recovery & helping players out of tough spots (Airdrops, Repair Stations, etc)
        TUTORIAL //Events used during tutorial missions
    }

    [Header("Events")]
    [SerializeField, Tooltip("All currently occuring events")] public List<EventType> currentEncounter = new List<EventType>();

    [Header("Spawner Variables")]
    [SerializeField, Tooltip("The minimum distance in chunks players need to travel before another event can trigger")] public float minTriggerDistance;
    [SerializeField, Tooltip("The maximum distance in chunks players need to travel before another event can trigger")] public float maxTriggerDistance;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
