using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class ChunkLoader : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField, Tooltip("The parent to keep all ground pieces in when spawned.")] private Transform groundParentTransform;
    [SerializeField, Tooltip("The chunk that the level starts with.")] private ChunkData startingChunk;
    

    public Transform playerTank;

    // The object pool for the ground chunks
    private List<ChunkData> groundPool = new List<ChunkData>();

    [Header("Chunk Pool")]
    [SerializeField, Tooltip("The chunk prefabs to use for spawning new chunks.")] private ChunkWeight[] chunkPrefabs;
    [SerializeField, Tooltip("How many chunks to spawn in the level.")] public int poolSize = 50;
    [SerializeField, Tooltip("How far away from the tank a chunk is allowed to render from.")] public float RENDER_DISTANCE = 100f;
    private int presetCount = 0;

    [Header("Procedural Variables")]
    [SerializeField, Tooltip("When spawning flat chunks, how many it should spawn in a row. Higher = higher stretches of flat terrain")] public int flatBias = 1;
    [SerializeField, Tooltip("When spawning sloped chunks, how many it should spawn in a row. Higher = bigger hills")] public int hillBias = 1;
    [SerializeField, Tooltip("When true, guarantees that the same bias can't happen twice in a row")] public bool strictBiases;
    private string[] spawnerWeights;
    private int currentBias = 0;
    private bool biasJustEnded = false;
    private float chunkCounter = 0f;
    private float previousY = 0f;
    private int previousChunk = 0;

    //Input
    private PlayerInput playerInputComponent;
    InputActionMap inputMap;

    [PropertySpace]
    [BoxGroup("Level Builder")]
    [SerializeField, Tooltip("Allows the use of debug controls to create a custom level during runtime. Enables a helper Canvas UI. Disables procedural level spawning.")]
    private bool enableLevelBuilder;
    private bool alt = false;
    public GameObject levelBuilderUI;

    private void Awake()
    {
        playerInputComponent = GetComponent<PlayerInput>();
        if (playerInputComponent != null) LinkPlayerInput(playerInputComponent);
       
        // Create and initialize the object pool
        if (!enableLevelBuilder)
        {
            SetupSpawner(); //determine spawner weights
            InitializeChunks(1); //Spawn the level randomly
            if (levelBuilderUI != null) levelBuilderUI.SetActive(false);
        }
        else
        {
            InitializeStarterChunk();
        }
    }

    private void SetupSpawner()
    {
        int count = 0;
        int length = 0;
        foreach (ChunkWeight weight in chunkPrefabs) //Get weights from every available chunk in the spawner
        {
            length += weight.weight;
            if (weight.isPreset) presetCount += weight.weight;
        }

        spawnerWeights = new string[length]; //sets up total weight values

        foreach (ChunkWeight weight in chunkPrefabs) //assigns weights to spawner array
        {
            if (weight.weight > 0)
            {
                for (int i = 0; i < weight.weight; i++)
                {
                    spawnerWeights[count] = weight.chunkPrefab.name;
                    count++;
                }
            }
        }
    }

    private void InitializeStarterChunk()
    {
        //Initialize starting chunk
        startingChunk.InitializeChunk(Vector3.zero);
        groundPool.Add(startingChunk);
    }
    /// <summary>
    /// Creates an object pool of chunks, starting with the starting chunk, and then places them. If directions is 1, chunks will only spawn to the right.
    /// </summary>
    private void InitializeChunks(int directions)
    {
        InitializeStarterChunk();

        float direction = -1f;
        if (directions == 1) direction = 1f;
        chunkCounter = 0f;
        previousY = 0f;
        previousChunk = 0;

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

            ChunkData chunkData = null;

            bool presetCheck = CheckForPreset();

            if (presetCheck)
            {
                chunkData = SpawnPreset(true, new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f));
            }
            else
            {
                chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), previousChunk);
                groundPool.Add(chunkData);
                chunkData.chunkNumber = chunkCounter;
            }

            if (chunkData.yOffset != 0 && chunkData != null) previousY += chunkData.yOffset; //Offsets Y position for next chunk to follow

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
    private ChunkData InstantiateChunk(Vector3 spawnPosition, int previousChunk, GameObject chunk = null)
    {
        if (chunk == null) chunk = DetermineChunkType(); //gets a random chunk
        /*if (currentBias > 0) chunk = previousChunk;

        int newChunk = Random.Range(0, chunkPrefabs.Length);
        if (biasJustEnded && strictBiases) //guarentees the next chunk will be different than the previous
        {
            while (newChunk == previousChunk)
            {
                newChunk = Random.Range(0, chunkPrefabs.Length);
            }
            chunk = newChunk;
            biasJustEnded = false;
        }*/
        
        GameObject _newChunkTransform = Instantiate(chunk, spawnPosition, chunk.transform.rotation);
        ChunkData newChunkTransform = _newChunkTransform.GetComponent<ChunkData>();
        newChunkTransform.transform.SetParent(groundParentTransform);
        newChunkTransform.InitializeChunk(spawnPosition);

        return newChunkTransform;
    }

    /// <summary>
    /// Determines what type of chunk to spawn next based on Spawner Variables & biases.
    /// </summary>
    private GameObject DetermineChunkType()
    {
        int random = Random.Range(0, spawnerWeights.Length - presetCount);
        GameObject chunkToSpawn = null;

        foreach(ChunkWeight weight in chunkPrefabs)
        {
            if (weight.chunkPrefab.name == spawnerWeights[random])
            {
                chunkToSpawn = weight.chunkPrefab;
            }
        }
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

    #region Presets
    private bool CheckForPreset()
    {
        int random = Random.Range(0, spawnerWeights.Length);
        string choice = spawnerWeights[random];
        foreach(ChunkWeight weight in chunkPrefabs)
        {
            if (weight.chunkPrefab.name == choice)
            {
                if (weight.isPreset)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private ChunkData SpawnPreset(bool randomized, Vector3 spawnPosition)
    {
        ChunkData lastChunk = null; //chunkdata to return so that the spawner knows where to spawn the next chunk
        GameObject preset = null; //preset we're gonna spawn

        if (randomized) //Chooses a random preset of the possible presets in the spawner
        {
            int count = 0; 
            foreach (ChunkWeight weight in chunkPrefabs) //determine local array size
            {
                if (weight.weight > 0 && weight.isPreset) count += weight.weight;
            }
            GameObject[] presets = new GameObject[count];

            count = 0;
            foreach (ChunkWeight weight in chunkPrefabs) //setup local array weights
            {
                if (weight.weight > 0 && weight.isPreset)
                {
                    for (int i = 0; i < weight.weight; i++)
                    {
                        presets[count] = weight.chunkPrefab;
                        count++;
                    }
                }
            }

            int random = Random.Range(0, presets.Length); //Roll for a random preset from the array
            preset = presets[random];
        }

        GameObject _preset = Instantiate(preset, spawnPosition, preset.transform.rotation);
        _preset.transform.SetParent(groundParentTransform);
        
        foreach (Transform child in _preset.transform)
        {
            if (child.name == "Chunk")
            {
                ChunkData chunkData = child.GetComponent<ChunkData>();
                groundPool.Add(chunkData);
                chunkData.chunkNumber = chunkCounter;
                chunkData.InitializeChunk(chunkData.transform.localPosition);

                chunkCounter++;

                lastChunk = chunkData;
            }
        }
        chunkCounter -= 1;
        return lastChunk;
    }
    #endregion

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

    #region LevelBuilder
    [BoxGroup("Level Builder")]
    [HorizontalGroup("Level Builder/Buttons")]
    [VerticalGroup("Level Builder/Buttons/Column 1")]
    [Button("Toggle")] public void ToggleLevelBuilder() 
    {
        if (enableLevelBuilder) enableLevelBuilder = false;
        else enableLevelBuilder = true;
        levelBuilderUI.SetActive(enableLevelBuilder);
    }
    [BoxGroup("Level Builder")]
    [HorizontalGroup("Level Builder/Buttons")]
    [VerticalGroup("Level Builder/Buttons/Column 2")]
    [Button("Save")]
    public void SaveLevel()
    {
        //Save the level layout to a scriptable object?
    }

    public void SpawnChunk(InputAction.CallbackContext ctx, int chunk)
    {
        if (ctx.started)
        {
            int direction = 1;
            chunkCounter++;

            if (chunk != 0 && alt) chunk += 1;

            ChunkData chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), previousChunk); //last argument needs to be what gameobject you're spawning
            groundPool.Add(chunkData);
            chunkData.chunkNumber = chunkCounter;

            if (chunkData.yOffset != 0) previousY += chunkData.yOffset; //Offsets Y position for next chunk to follow

            levelBuilderUI.transform.position = chunkData.transform.position;
            Vector3 offsetPos = new Vector3(0, chunkData.yOffset, 0);
            levelBuilderUI.transform.position += offsetPos;
        }
    }

    public void SwapType(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (alt) alt = false;
            else if (!alt) alt = true;

            foreach (Transform child in levelBuilderUI.transform)
            {
                Transform _child = child.Find("Visual");
                if (_child != null) _child.Rotate(0, 180, 0);
            }
        }
    }

    public void DeletePreviousChunk(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            int index = groundPool.Count - 1;
            GameObject chunk = groundPool[index].gameObject;

            if (index > 0)
            {
                chunkCounter -= 1;
                groundPool.RemoveAt(index);
                Destroy(chunk);

                ChunkData chunkData = groundPool[index - 1];
                previousY = chunkData.transform.position.y;

                levelBuilderUI.transform.position = chunkData.transform.position;
                Vector3 offsetPos = new Vector3(chunkData.transform.position.x, previousY + chunkData.yOffset, 0);
                levelBuilderUI.transform.position = offsetPos;
            }
        }
    }

    #endregion

    #region Input
    public void LinkPlayerInput(PlayerInput newInput)
    {
        playerInputComponent = newInput;

        //Gets the player input action map so that events can be subscribed to it
        inputMap = playerInputComponent.actions.FindActionMap("Debug");
        inputMap.actionTriggered += OnPlayerInput;
    }

    private void OnPlayerInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "1": SpawnChunk(ctx, 0); break;
            case "2": SpawnChunk(ctx, 1); break;
            case "3": SpawnChunk(ctx, 3); break;
            case "4": DeletePreviousChunk(ctx); break;
            case "Cycle": SwapType(ctx); break;
        }
    }
    #endregion
}




