using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class ChunkData : MonoBehaviour
    {
        public float chunkNumber;

        public enum ChunkType { FLAT, SLOPEUP, SLOPEDOWN, RAMPUP, RAMPDOWN, PRESET };
        public ChunkType chunkType;

        public bool IsActive { get; private set; }
        public bool isInitialized { get; private set; }

        public const float CHUNK_WIDTH = 30f;
        public float yOffset;

        [SerializeField, Tooltip("Flag that signals the end of the level.")] GameObject flag;
        [SerializeField, Tooltip("Position on this chunk to spawn a flag")] private Transform flagSpawn; //place to spawn a flag if needed
        [SerializeField, Tooltip("Positions on this chunk to spawn obstacles")] private Transform[] obstacleSpawns; //place to spawn a flag if needed

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
                transform.localPosition = position;
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
            }
        }

        public void SpawnFlag(Color color)
        {
            var newflag = Instantiate(flag, flagSpawn);
            SpriteRenderer flagSprite = newflag.transform.Find("Visuals").Find("FlagSprite").GetComponent<SpriteRenderer>();
            flagSprite.color = color;
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
