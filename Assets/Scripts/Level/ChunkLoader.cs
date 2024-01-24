using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    private const float RENDER_DISTANCE = 300f;

    [SerializeField, Tooltip("The parent to keep all ground pieces in when spawned.")] private Transform groundParentTransform;
    [SerializeField, Tooltip("The chunk that the level starts with.")] private ChunkData startingChunk;
    [SerializeField, Tooltip("The chunk prefab to use for spawning new chunks.")] private ChunkData chunkPrefab;

    private PlayerTankController playerTank;

    // The object pool for the ground chunks
    private List<ChunkData> groundPool = new List<ChunkData>();
    private int poolSize = 300;

    private void Awake()
    {
        playerTank = FindObjectOfType<PlayerTankController>();

        // Create and initialize the object pool
        InitializeChunks();
    }

    /// <summary>
    /// Creates an object pool of chunks, starting with the starting chunk, and then places them.
    /// </summary>
    private void InitializeChunks()
    {
        //Initialize starting chunk
        startingChunk.InitializeChunk(Vector3.zero);
        groundPool.Add(startingChunk);

        float direction = -1f;
        float chunkCounter = 0f;

        //Creates each chunk and alternates between placing them to the right and the left of the world
        for (int i = 1; i < poolSize; i++)
        {
            direction = -direction;

            if(i % 2 == 1)
                chunkCounter++;

            ChunkData chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, 0f, 0f));
            groundPool.Add(chunkData);
        }
    }

    /// <summary>
    /// Creates a new chunk based on the spawn position given.
    /// </summary>
    /// <param name="spawnPosition">The position for the chunk to spawn at.</param>
    /// <returns>The data of the newly spawned chunk.</returns>
    private ChunkData InstantiateChunk(Vector3 spawnPosition)
    {
        ChunkData newChunkTransform = Instantiate(chunkPrefab, spawnPosition, Quaternion.identity);
        newChunkTransform.transform.SetParent(groundParentTransform);
        newChunkTransform.InitializeChunk(spawnPosition);

        return newChunkTransform;
    }

    /// <summary>
    /// Updates the chunks by loading or unloading them based on the render distance.
    /// </summary>
    private void UpdateChunks()
    {
        foreach (ChunkData chunkData in groundPool)
        {
            float chunkDistance = Vector3.Distance(playerTank.transform.position, chunkData.transform.position);

            //If the chunk is within the render distance, load it. If not, unload it.
            if (chunkDistance <= RENDER_DISTANCE)
                chunkData.LoadChunk();
            else
                chunkData.UnloadChunk();
        }
    }

    private void Update()
    {
        if (playerTank != null)
            UpdateChunks();
    }
}




