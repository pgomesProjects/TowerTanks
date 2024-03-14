using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField, Tooltip("The parent to keep all ground pieces in when spawned.")] private Transform groundParentTransform;
    [SerializeField, Tooltip("The chunk that the level starts with.")] private ChunkData startingChunk;
    [SerializeField, Tooltip("The chunk prefabs to use for spawning new chunks.")] private ChunkData[] chunkPrefabs;

    public Transform playerTank;

    // The object pool for the ground chunks
    private List<ChunkData> groundPool = new List<ChunkData>();

    [Header("Chunk Pool")]
    [SerializeField, Tooltip("How many chunks to spawn in the level.")] public int poolSize = 50;
    [SerializeField, Tooltip("How far away from the tank a chunk is allowed to render from.")] public float RENDER_DISTANCE = 100f;

    [Header("Procedural Variables")]
    [SerializeField, Tooltip("Weight of flat terrain in chunk spawner. Higher = More flat chunks")] public int flatness = 10;
    [SerializeField, Tooltip("Weight of sloped terrain in chunk spawner. Higher = More sloped chunks")] public int hillyness = 10;
    [SerializeField, Tooltip("Weight of metal ramps in chunk spawner. Higher = More ramp chunks")] public int rampyness = 10;
    [SerializeField, Tooltip("When spawning flat chunks, how many it should spawn in a row. Higher = higher stretches of flat terrain")] public int flatBias = 1;
    [SerializeField, Tooltip("When spawning sloped chunks, how many it should spawn in a row. Higher = bigger hills")] public int hillBias = 1;
    [SerializeField, Tooltip("When true, guarantees that the same bias can't happen twice in a row")] public bool strictBiases;
    public int[] spawnerWeights;
    private int currentBias = 0;
    private bool biasJustEnded = false;

    private void Awake()
    {
        SetupSpawner();

        // Create and initialize the object pool
        InitializeChunks(1);
    }

    private void SetupSpawner()
    {
        int count = 0;
        spawnerWeights = new int[flatness + hillyness + rampyness]; //0-29
        for (int i = 0; i < flatness; i++) //add flat weights
        {
            spawnerWeights[count] = 0;
            count++;
        }
        for (int i = 0; i < hillyness; i++) //add flat weights
        {
            spawnerWeights[count] = 1;
            count++;
        }
        for (int i = 0; i < rampyness; i++) //add flat weights
        {
            spawnerWeights[count] = 2;
            count++;
        }
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
        int previousChunk = 0;

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

            ChunkData chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), previousChunk);
            groundPool.Add(chunkData);
            chunkData.chunkNumber = chunkCounter;

            if (chunkData.yOffset != 0) previousY += chunkData.yOffset; //Offsets Y position for next chunk to follow

            //Biases
            if (chunkData.chunkType == ChunkData.ChunkType.FLAT)
            {
                previousChunk = 0;
                if (flatBias > 0 && currentBias == 0) { currentBias = flatBias; }
            }
            else if (chunkData.chunkType == ChunkData.ChunkType.SLOPEUP)
            {
                previousChunk = 1;
                if (hillBias > 0 && currentBias == 0) { currentBias = hillBias; }
            }
            else if (chunkData.chunkType == ChunkData.ChunkType.SLOPEDOWN)
            {
                previousChunk = 2;
                if (hillBias > 0 && currentBias == 0) { currentBias = hillBias; }
            }
            else if (chunkData.chunkType == ChunkData.ChunkType.RAMPUP)
            {
                previousChunk = 3;
                //if (hillBias > 0 && currentBias == 0) { currentBias = hillBias; }
            }
            else if (chunkData.chunkType == ChunkData.ChunkType.RAMPDOWN)
            {
                previousChunk = 4;
                //if (hillBias > 0 && currentBias == 0) { currentBias = hillBias; }
            }

            if (currentBias > 0) //spawn additional chunks if there's a bias in place
            {
                for (int b = 0; b < currentBias; b++)
                {
                    chunkCounter++;
                    chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), previousChunk);
                    groundPool.Add(chunkData);
                    chunkData.chunkNumber = chunkCounter;

                    if (chunkData.yOffset != 0) previousY += chunkData.yOffset;
                }
                biasJustEnded = true;
                currentBias = 0;
            }

            //Spawn a flag on the last chunk
            if (i >= poolSize - 1)
            {
                chunkData.SpawnFlag(Color.red);
            }

            if (i == Mathf.Round(poolSize * 0.5f)) //Spawn a flag at the halfway mark
            {
                chunkData.SpawnFlag(Color.blue);
            }
        }
    }

    /// <summary>
    /// Creates a new chunk based on the spawn position given.
    /// </summary>
    /// <param name="spawnPosition">The position for the chunk to spawn at.</param>
    /// <returns>The data of the newly spawned chunk.</returns>
    private ChunkData InstantiateChunk(Vector3 spawnPosition, int previousChunk)
    {
        int chunk = DetermineChunkType();
        if (currentBias > 0) chunk = previousChunk;

        int newChunk = Random.Range(0, chunkPrefabs.Length);
        if (biasJustEnded && strictBiases) //guarentees the next chunk will be different than the previous
        {
            while (newChunk == previousChunk)
            {
                newChunk = Random.Range(0, chunkPrefabs.Length);
            }
            chunk = newChunk;
            biasJustEnded = false;
        }
        
        ChunkData newChunkTransform = Instantiate(chunkPrefabs[chunk], spawnPosition, chunkPrefabs[chunk].transform.rotation);
        newChunkTransform.transform.SetParent(groundParentTransform);
        newChunkTransform.InitializeChunk(spawnPosition);

        return newChunkTransform;
    }

    /// <summary>
    /// Determines what type of chunk to spawn next based on Spawner Variables & biases.
    /// </summary>
    private int DetermineChunkType()
    {
        int chunkToSpawn = 0;
        int random = Random.Range(0, spawnerWeights.Length);

        if (spawnerWeights[random] == 0) chunkToSpawn = 0;                  //Flat
        if (spawnerWeights[random] == 1) chunkToSpawn = Random.Range(1, 3); //Slope
        if (spawnerWeights[random] == 2) chunkToSpawn = Random.Range(3, 5); //Ramp

        return chunkToSpawn;

    }

    /// <summary>
    /// Updates the chunks by loading or unloading them based on the render distance.
    /// </summary>
    private void UpdateChunks()
    {
        foreach (ChunkData chunkData in groundPool)
        {
            float chunkDistance = Vector3.Distance(playerTank.position, chunkData.transform.position);

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTank.position, RENDER_DISTANCE);
    }
}




