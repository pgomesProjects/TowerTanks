using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class ChunkData : MonoBehaviour
    {
        public int chunkNumber;

        public enum ChunkType { FLAT, SLOPEUP, SLOPEDOWN, RAMPUP, RAMPDOWN, PRESET };
        public ChunkType chunkType;

        public bool IsActive { get; private set; }
        public bool isInitialized { get; private set; }

        public const float CHUNK_WIDTH = 30f;
        public float yOffset;

        [SerializeField, Tooltip("Flag that signals the end of the level.")] private GameObject flag;
        [SerializeField, Tooltip("Current flag on this chunk")] public Transform currentFlag;
        [SerializeField, Tooltip("Position on this chunk to spawn a flag")] private Transform flagSpawn; //place to spawn a flag if needed
        [SerializeField, Tooltip("Positions on this chunk to spawn obstacles")] private Transform[] obstacleSpawns; //place to spawn a flag if needed
        [SerializeField, Tooltip("Currently spawned obstacle on this chunk.")] public GameObject currentObstacle;

        public bool canSpawnLandmarks;

        private void Awake()
        {
            flagSpawn = transform.Find("FlagSpawn");
            UnloadChunk();
        }

        /// <summary>
        /// Initializes the chunk by placing it and adding any information that needs to be stored.
        /// </summary>
        /// <param name="position">The position that the chunk will be placed at within the parent.</param>
        public void InitializeChunk(Vector3 position)
        {
            if (!isInitialized)
            {
                if (position != null) transform.localPosition = position;
                isInitialized = true;
            }
        }

        /// <summary>
        /// Generates a random obstacle at a random spawn point on the chunk.
        /// </summary>
        public void GenerateObstacle(GameObject obstacle, float spawnChance)
        {
            if (Random.Range(0f, 100f) <= spawnChance && obstacleSpawns.Length > 0)
            {
                int random = Random.Range(0, obstacleSpawns.Length);
                Transform randomSpawn = obstacleSpawns[random];
                DestructibleObject newObstacle = Instantiate(obstacle, randomSpawn.position, randomSpawn.rotation, transform).GetComponent<DestructibleObject>();
                currentObstacle = newObstacle.gameObject;
            }
        }

        public void GenerateLandmark(GameObject landmark)
        {
            if (canSpawnLandmarks && obstacleSpawns.Length > 0)
            {
                int random = Random.Range(0, obstacleSpawns.Length);
                Transform randomSpawn = obstacleSpawns[random];
                GameObject newLandmark = Instantiate(landmark, randomSpawn.position, randomSpawn.rotation, transform);
            }
        }

        public void SpawnFlag(Color color, int flagSpriteIndex = 2)
        {
            var newflag = Instantiate(flag, flagSpawn);
            newflag.transform.Rotate(new Vector3(0, 180, 0));
            SpriteRenderer flagSprite = newflag.transform.Find("Visuals").Find("FlagSprite").GetComponent<SpriteRenderer>();
            flagSprite.color = color;

            if (color == Color.red) { newflag.name = "Flag (End)"; }

            FlagSettings settings = newflag.GetComponent<FlagSettings>();
            Sprite _sprite = TankManager.instance.tankFlagSprites[flagSpriteIndex];
            settings.flagSprite = _sprite;

            currentFlag = newflag.transform;
        }

        /// <summary>
        /// Loads the chunk by setting it active.
        /// </summary>
        public void LoadChunk()
        {
            IsActive = true;
            transform.gameObject.SetActive(true);
        }

        /// <summary>
        /// Unloads the chunk by setting it inactive.
        /// </summary>
        public void UnloadChunk()
        {
            IsActive = false;
            transform.gameObject.SetActive(false);
        }
    }
}
