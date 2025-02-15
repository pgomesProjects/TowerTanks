using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class EventSpawnerManager : MonoBehaviour
    {
        private LevelManager levelManager;
        private ChunkLoader chunkLoader;

        public enum EventType
        {
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
        [SerializeField, Tooltip("The current chunk the player tank is on")] public int currentChunk = 0;
        [SerializeField, Tooltip("The chunk players have to travel passed for it to be considered new territory")] private int lastChunk = 0;
        [SerializeField, Tooltip("The number of chunks the player tank has traveled since the last event marker")] public int chunksTraveled = 0;
        [SerializeField, Tooltip("The minimum distance in chunks players need to travel before another event can trigger")] public float minTriggerDistance;
        [SerializeField, Tooltip("The maximum distance in chunks players need to travel before another event can trigger")] public float maxTriggerDistance;
        private float triggerDistanceCheck = 0;

        [SerializeField, Tooltip("The time between when events can trigger")] public float eventSpawnCooldown;
        private float eventSpawnTimer = 0;

        [Tooltip("Current chance a Merchant will spawn in place of an enemy")] public float shopChance;

        private TankController playerTank;
        private List<TankController> enemies = new List<TankController>();

        public bool endFlagExists;
        private bool levelEnded;

        private void Awake()
        {
            levelManager = GetComponent<LevelManager>();
            chunkLoader = GameObject.Find("ChunkLoader").GetComponent<ChunkLoader>();

            //Inherit Shop Chance
            shopChance = GameManager.shopChance;
        }

        // Start is called before the first frame update
        void Start()
        {
            UpdatePlayerTank();
            eventSpawnTimer = eventSpawnCooldown;
            triggerDistanceCheck = Random.Range(minTriggerDistance, maxTriggerDistance);
            //shopChance = 0;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateCurrentChunk();
            if (!endFlagExists) CheckForEvents();
        }

        public void UpdateCurrentChunk()
        {
            currentChunk = chunkLoader.currentChunk;
            if (currentChunk > lastChunk && !currentEncounter.Contains(EventType.ENEMY))
            {
                lastChunk = currentChunk;
                chunksTraveled += 1;
            }

            if (endFlagExists)
            {
                bool endLevel = chunkLoader.CheckEndFlag();
                if (endLevel == true && levelEnded == false)
                {
                    Debug.Log("Level Finished!");
                    playerTank.Transition();
                    levelEnded = true;
                }
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
                    int random = Random.Range(0, 100);

                    if (random < shopChance) 
                    { 
                        TriggerEvent(EventType.FRIENDLY);
                        shopChance = 0;
                    }
                    else 
                    { 
                        TriggerEvent(EventType.ENEMY);
                        shopChance += 20;
                    }
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

                case EventType.FRIENDLY:
                    {
                        Debug.Log("Spawning Merchant!");
                        currentEncounter.Add(newEvent);
                        SpawnNewMerchant();
                    }
                    break;

                case EventType.TUTORIAL: Debug.Log("Spawning Tutorial Event!"); break;
            }
            chunksTraveled = 0;
        }

        public void SpawnNewMerchant()
        {
            //Finding new spawn point
            int offset = currentChunk + 6; //Find the chunk that's [x] chunks away from current chunk
            float chunkX = chunkLoader.GetChunkAtIndex(offset).transform.position.x; //get it's x position
            Vector3 findPos = new Vector3(chunkX, 0, 0);
            Vector3 newSpawnPoint = chunkLoader.GetChunkAtPosition(findPos).transform.position; //Get it's position

            //Set new spawn point
            levelManager.tankManager.MoveSpawnPoint(newSpawnPoint);
            levelManager.tankManager.tankSpawnPoint.position += new Vector3(0, 20, 0);

            //Determine Tier
            int tier = 1;

            //Spawn new enemy tank
            TankController newtank = levelManager.tankManager.SpawnTank(tier, TankId.TankType.NEUTRAL, true, false);
            //enemies.Add(newtank);
        }

        public void SpawnNewEnemy()
        {
            //Finding new spawn point
            int offset = currentChunk + 8; //Find the chunk that's [x] chunks away from current chunk
            float chunkX = chunkLoader.GetChunkAtIndex(offset).transform.position.x; //get it's x position
            Vector3 findPos = new Vector3(chunkX, 0, 0);
            Vector3 newSpawnPoint = chunkLoader.GetChunkAtPosition(findPos).transform.position; //Get it's position

            //Set new spawn point
            levelManager.tankManager.MoveSpawnPoint(newSpawnPoint);
            levelManager.tankManager.tankSpawnPoint.position += new Vector3(0, 20, 0);

            //Determine Tier
            int tier = levelManager.GetEnemyTier();

            //Spawn new enemy tank
            TankController newtank = levelManager.tankManager.SpawnTank(tier, TankId.TankType.ENEMY, true, false);
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
                int newMarker = chunkLoader.GetChunkAtPosition(destroyedPos).chunkNumber;
                if (newMarker != -1) lastChunk = newMarker;
                GameManager.Instance.AudioManager.StopCombatMusic();
            }
        }

        public void EncounterEnded(EventType type)
        {
            if (currentEncounter.Contains(type)) currentEncounter.Remove(type);

            if (type == EventType.ENEMY && enemies.Count == 0)
            {
                Vector3 endPos = playerTank.treadSystem.transform.position;
                int newMarker = chunkLoader.GetChunkAtPosition(endPos).chunkNumber;
                if (newMarker != -1) lastChunk = newMarker;
            }
        }

        public void SpawnEndOfLevel()
        {
            Debug.Log("Spawned Flag!");
            int flagOffset = lastChunk + 3;
            chunkLoader.SpawnEndFlag(flagOffset);
        }

        public void UpdatePlayerTank()
        {
            playerTank = LevelManager.Instance.playerTank;
            chunksTraveled = 0;
        }
    }
}
