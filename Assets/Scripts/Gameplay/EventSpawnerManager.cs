using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class EventSpawnerManager : MonoBehaviour
{
    private LevelManager levelManager;
    private ChunkLoader chunkLoader;

    public enum EventType { 
        ENEMY, //Spawns an entity that can directly harm the tank (Tanks, Outposts, etc)
        OBSTACLE, //Gets in players way but doesn't directly harm the tank (Props, Terrain, etc)
        WEATHER, //Changes the weather (Rain, Thunderstorm, Wind, Fog, Sandstorm, etc)
        FRIENDLY, //Events that benefit the players, mostly used for recovery & helping players out of tough spots (Airdrops, Repair Stations, etc)
        TUTORIAL //Events used during tutorial missions
    }

    [Header("Events")]
    [SerializeField, Tooltip("All currently occuring events")] public List<EventType> currentEncounter = new List<EventType>();
    [SerializeField, Tooltip("The maximum number of events that can be active at once in the current encounter")] public int maxEvents = 1;

    [Header("Spawner Variables")]
    [SerializeField, Tooltip("The current chunk the player tank is on")] public float currentChunk = 0;
    [SerializeField, Tooltip("The chunk players have to travel passed for it to be considered new territory")] private float lastChunk = 0;
    [SerializeField, Tooltip("The number of chunks the player tank has traveled since the last event marker")] public float chunksTraveled = 0;
    [SerializeField, Tooltip("The minimum distance in chunks players need to travel before another event can trigger")] public float minTriggerDistance;
    [SerializeField, Tooltip("The maximum distance in chunks players need to travel before another event can trigger")] public float maxTriggerDistance;
    private float triggerDistanceCheck = 0;

    [SerializeField, Tooltip("The time between when events can trigger")] public float eventSpawnCooldown;
    private float eventSpawnTimer = 0;

    private TankController playerTank;
    private List<TankController> enemies = new List<TankController>();

    private void Awake()
    {
        levelManager = GetComponent<LevelManager>();
        chunkLoader = GameObject.Find("ChunkLoader").GetComponent<ChunkLoader>();
    }

    // Start is called before the first frame update
    void Start()
    {
        playerTank = levelManager.playerTank;
        eventSpawnTimer = eventSpawnCooldown;
        triggerDistanceCheck = Random.Range(minTriggerDistance, maxTriggerDistance);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCurrentChunk();
        CheckForEvents();
    }

    public void UpdateCurrentChunk()
    {
        currentChunk = chunkLoader.currentChunk;
        if(currentChunk > lastChunk && !currentEncounter.Contains(EventType.ENEMY)) 
        {
            lastChunk = currentChunk;
            chunksTraveled += 1;
        }
    }

    public void CheckForEvents()
    {
        eventSpawnTimer -= Time.deltaTime;
        if (eventSpawnTimer <= 0)
        {
            //Trigger event
            if (currentEncounter.Count < maxEvents && chunksTraveled >= triggerDistanceCheck) 
            {
                TriggerEvent(EventType.ENEMY);
            }

            eventSpawnTimer = eventSpawnCooldown;
        }
    }

    public void TriggerEvent(EventType newEvent)
    {
        switch (newEvent)
        {
            case EventType.ENEMY: 
                { 
                    Debug.Log("Spawning Enemy!"); 
                    currentEncounter.Add(newEvent);
                    SpawnNewEnemy();
                }  
                break;

            case EventType.OBSTACLE: Debug.Log("Spawning Obstacle!"); break;
            case EventType.WEATHER: Debug.Log("Spawning Weather!"); break;
            case EventType.FRIENDLY: Debug.Log("Spawning Friendly!"); break;
            case EventType.TUTORIAL: Debug.Log("Spawning Tutorial Event!"); break;
        }
        chunksTraveled = 0;
    }

    public void SpawnNewEnemy()
    {
        //Finding new spawn point
        float chunkX = (currentChunk + 8) * ChunkData.CHUNK_WIDTH; //Find the chunk that's 8 chunks away from current chunk
        Vector3 findPos = new Vector3(chunkX, 0, 0);
        Vector3 newSpawnPoint = chunkLoader.GetChunkAtPosition(findPos).transform.position; //Get it's position

        //Set new spawn point
        levelManager.tankManager.MoveSpawnPoint(newSpawnPoint);
        levelManager.tankManager.tankSpawnPoint.position += new Vector3(0, 20, 0);

        //Determine Tier
        int tier = levelManager.GetEnemyTier();

        //Spawn new enemy tank
        TankController newtank = levelManager.tankManager.SpawnTank(tier, true, true);
        enemies.Add(newtank);
    }

    public void EnemyDestroyed(TankController tank)
    {
        if (enemies.Contains(tank))
        {
            enemies.Remove(tank);
            currentEncounter.Remove(EventType.ENEMY);
        }
        if (enemies.Count == 0) 
        {
            Vector3 destroyedPos = tank.treadSystem.transform.position;
            float newMarker = chunkLoader.GetChunkAtPosition(destroyedPos).chunkNumber;
            if (newMarker != -1) lastChunk = newMarker;
        }
    }
}