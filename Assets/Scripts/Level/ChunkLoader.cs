using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    private const float RENDER_DISTANCE = 600f;

    [SerializeField, Tooltip("The parent to keep all ground pieces in when spawned.")] private Transform groundParentTransform;
    [SerializeField, Tooltip("The chunk that the level starts with.")] private ChunkData startingChunk;
    [SerializeField, Tooltip("The chunk prefabs to use for spawning new chunks.")] private ChunkData[] chunkPrefabs;

    private PlayerTankController playerTank;

    // The object pool for the ground chunks
    private List<ChunkData> groundPool = new List<ChunkData>();
    private int poolSize = 300;

    private void Awake()
    {
        playerTank = FindObjectOfType<PlayerTankController>();

        // Create and initialize the object pool
        InitializeChunks(1);
    }

    /// <summary>
    /// Creates an object pool of chunks, starting with the starting chunk, and then places them. If directions is 1, chunks will only spawn to the right.
    /// </summary>
    private void InitializeChunks(int directions)
    {
        //Initialize starting chunk
        startingChunk.InitializeChunk(Vector3.zero);
        groundPool.Add(startingChunk);

        float direction = -1f;
        if (directions == 1) direction = 1f;
        float chunkCounter = 0f;
        float previousY = 0f;

        //Creates each chunk in the world
        for (int i = 1; i < poolSize; i++)
        {
            if (directions == 1) chunkCounter++; //if directions = 1, spawn only to the right

            if (directions == 2) //if going in both directions, alternate between left and right when spawning
            {
                direction = -direction;

                if (i % 2 == 1)
                    chunkCounter++;
            }

            ChunkData chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f));
            groundPool.Add(chunkData);

            if (chunkData.chunkType == ChunkData.ChunkType.SLOPEUP) previousY += 23f;
            else if (chunkData.chunkType == ChunkData.ChunkType.SLOPEDOWN) previousY -= 23f;
        }
    }

    /// <summary>
    /// Creates a new chunk based on the spawn position given.
    /// </summary>
    /// <param name="spawnPosition">The position for the chunk to spawn at.</param>
    /// <returns>The data of the newly spawned chunk.</returns>
    private ChunkData InstantiateChunk(Vector3 spawnPosition)
    {
        int random = Random.Range(0, chunkPrefabs.Length);
        ChunkData newChunkTransform = Instantiate(chunkPrefabs[random], spawnPosition, Quaternion.identity);
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




