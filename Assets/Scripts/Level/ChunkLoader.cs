using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class ChunkLoader : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField, Tooltip("The parent to keep all ground pieces in when spawned.")] private Transform groundParentTransform;
    [SerializeField, Tooltip("The chunk that the level starts with.")] private ChunkData startingChunk;
    
    public Transform playerTank;
    public float currentChunk; //which chunk the player is currently on

    // The object pool for the ground chunks
    private List<ChunkData> groundPool = new List<ChunkData>();

    [Header("Chunk Pool")]
    [SerializeField, Tooltip("The chunk prefabs to use for spawning new chunks.")] private ChunkWeight[] chunkPrefabs;
    [SerializeField, Tooltip("How many chunks to spawn in the level.")] public int poolSize = 50;
    [SerializeField, Tooltip("How far away from the tank a chunk is allowed to render from.")] public float RENDER_DISTANCE = 100f;
    private int presetCount = 0;

    [Header("Procedural Variables")]
    [SerializeField, Tooltip("When false, the same bias can't happen twice in a row")] public bool biasesCanRepeat;
    private string[] spawnerWeights;
    private int currentBias = 0;
    private bool biasJustEnded = false;
    private float chunkCounter = 0f;
    private float previousY = 0f;
    private GameObject previousChunk = null;

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
                chunkData = InstantiatePreset(true, new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f));
            }
            else
            {
                chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f));
                groundPool.Add(chunkData);
                chunkData.chunkNumber = chunkCounter;
            }

            if (chunkData.yOffset != 0 && chunkData != null) previousY += chunkData.yOffset; //Offsets Y position for next chunk to follow

            //Biases
            if (currentBias > 0) //spawn additional chunks if there's a bias in place
            {
                for (int b = 0; b < currentBias; b++)
                {
                    if (previousChunk.GetComponent<ChunkData>() != null) //it's a normal chunk
                    {
                        chunkCounter++;
                        chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), previousChunk);
                        groundPool.Add(chunkData);
                        chunkData.chunkNumber = chunkCounter;
                    }
                    else
                    {
                        chunkCounter++;
                        chunkData = InstantiatePreset(false, new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), previousChunk);
                    }
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
    private ChunkData InstantiateChunk(Vector3 spawnPosition, GameObject chunk = null)
    {
        if (chunk == null) chunk = DetermineChunkType(); //gets a random chunk

        int _chunkCount = 0;
        foreach (ChunkWeight weight in chunkPrefabs) //determines if there's more than 1 non-preset chunk available
        {
            if (weight.isPreset == false) _chunkCount += 1;
        }

        if (biasJustEnded && !biasesCanRepeat && _chunkCount > 1) //guarentees the next chunk will be different than the previous
        {
            while (chunk == previousChunk)
            {
                int _presetCount = 0;
                foreach(ChunkWeight weight in chunkPrefabs)
                {
                    if (weight.isPreset) _presetCount += 1;
                }
                int random = Random.Range(0, chunkPrefabs.Length - _presetCount);
                chunk = chunkPrefabs[random].chunkPrefab;
            }
            biasJustEnded = false;
        }
        
        GameObject _newChunkTransform = Instantiate(chunk, spawnPosition, chunk.transform.rotation);
        ChunkData newChunkTransform = _newChunkTransform.GetComponent<ChunkData>();
        newChunkTransform.transform.SetParent(groundParentTransform);
        newChunkTransform.InitializeChunk(spawnPosition);
        previousChunk = chunk;

        //Check for bias
        foreach(ChunkWeight weight in chunkPrefabs)
        {
            if (weight.bias > 0 && (weight.chunkPrefab == previousChunk))
            {
                if (currentBias == 0)
                {
                    currentBias = weight.bias;
                }
            }
        }

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
            Vector3 playerTransform = new Vector3(playerTank.position.x, 0, 0);
            Vector3 chunkTransform = new Vector3(chunkData.transform.position.x, 0, 0);
            float chunkDistance = Vector3.Distance(playerTransform, chunkTransform);

            //Check if it's close enough to consider it the current chunk
            if (chunkDistance <= 1f) currentChunk = chunkData.chunkNumber;

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

    private ChunkData InstantiatePreset(bool randomized, Vector3 spawnPosition, GameObject preset = null)
    {
        ChunkData lastChunk = null; //chunkdata to return so that the spawner knows where to spawn the next chunk

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

        int _presetCount = 0;
        foreach (ChunkWeight weight in chunkPrefabs)
        {
            if (weight.isPreset && weight.weight > 0) _presetCount += 1;
        }

        if (biasJustEnded && !biasesCanRepeat && _presetCount > 1) //guarentees the next chunk will be different than the previous
        {
            while (preset == previousChunk)
            {
                int random = Random.Range(chunkPrefabs.Length - _presetCount, chunkPrefabs.Length);
                preset = chunkPrefabs[random].chunkPrefab;
            }
            biasJustEnded = false;
        }

        GameObject _preset = Instantiate(preset, spawnPosition, preset.transform.rotation); //spawn the preset
        _preset.transform.SetParent(groundParentTransform);
        previousChunk = preset;
        
        foreach (Transform child in _preset.transform) //initialize all the child chunks in the preset
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

        //Check for bias
        foreach (ChunkWeight weight in chunkPrefabs)
        {
            if (weight.bias > 0 && (weight.chunkPrefab == previousChunk))
            {
                if (currentBias == 0)
                {
                    currentBias = weight.bias;
                }
            }
        }

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
        //Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(playerTank.position, RENDER_DISTANCE);
    }

    #region LevelBuilder
    [BoxGroup("Level Builder")]
    [HorizontalGroup("Level Builder/Buttons")]
    [VerticalGroup("Level Builder/Buttons/Column 1")]
    [Button("Toggle"), Tooltip("Toggles the level builder & associated UI")] public void ToggleLevelBuilder() 
    {
        if (enableLevelBuilder) enableLevelBuilder = false;
        else enableLevelBuilder = true;
        levelBuilderUI.SetActive(enableLevelBuilder);
    }

    [BoxGroup("Level Builder")]
    [HorizontalGroup("Level Builder/Buttons")]
    [VerticalGroup("Level Builder/Buttons/Column 2")]
    [Button("Save"), Tooltip("Saves the current level as a new layout")]
    public void SaveLevel()
    {
        LevelLayout layout = new LevelLayout(); //Setup new layout

        int levelSize = 0;
        foreach (Transform chunk in groundParentTransform) //Establish the size of the layout array of the new asset
        {
            levelSize++;
        }
        layout.chunks = new string[levelSize];

        int counter = 0;
        foreach (Transform chunk in groundParentTransform) //Assign chunks to the new layout array
        {
            string chunkName = chunk.name;
            chunkName = chunkName.Replace("(Clone)", "");
            layout.chunks[counter] = chunkName;
            counter++;
        }

        string json = JsonUtility.ToJson(layout, true);

        if (File.Exists("Assets/Resources/LevelLayouts/LevelLayoutFile.json")) { Debug.LogError("File exists. Overwriting Existing File."); }
        File.WriteAllText("Assets/Resources/LevelLayouts/LevelLayoutFile.json", json);
        AssetDatabase.Refresh();
    }

    [BoxGroup("Level Builder")]
    [HorizontalGroup("Level Builder/Buttons")]
    [VerticalGroup("Level Builder/Buttons/Column 2")]
    [Button("Load"), Tooltip("Loads a level from the currently selected layout. If layout is null, loads a random layout.")]
    public void LoadLevel()
    {
        string json = selectedLayout.text;
        if (json != null)
        {
            LevelLayout layout = JsonUtility.FromJson<LevelLayout>(json);
            //Debug.Log("" + layout.chunks[0] + ", " + layout.chunks[1] + "...");
            InitializeChunksFromLayout(layout);
        }
    }

    [Tooltip("Level layout to load")]
    public TextAsset selectedLayout;

    public void InitializeChunksFromLayout(LevelLayout layout)
    {
        float direction = 1f;
        chunkCounter = 0f;
        previousY = 0f;

        int _poolSize = 0;
        foreach(string chunk in layout.chunks) //Determine level size
        {
            foreach (ChunkWeight weight in chunkPrefabs) //presets add multiple chunks
            {
                if (weight.chunkPrefab.name == chunk)
                {
                    if (weight.isPreset)
                    {
                        foreach(Transform child in weight.chunkPrefab.transform)
                        {
                            _poolSize++;
                        }
                        _poolSize -= 1;
                    }
                }
            }
            _poolSize++;
        }

        //Creates each chunk in the world
        foreach (string chunk in layout.chunks)
        {
            chunkCounter++;
            ChunkData chunkData;
            GameObject chunkToSpawn = null;
            bool spawnPreset = false;
            foreach(ChunkWeight weight in chunkPrefabs) //check to see if the chunk from the layout is in the current prefab palette
            {
                if (weight.chunkPrefab.name == chunk)
                {
                    chunkToSpawn = weight.chunkPrefab;
                    if (weight.isPreset) spawnPreset = true;
                }
            }

            if (spawnPreset)
            {
                chunkData = InstantiatePreset(false, new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), chunkToSpawn);
            }
            else
            {
                chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), chunkToSpawn);
                groundPool.Add(chunkData);
                chunkData.chunkNumber = chunkCounter;
            }

            if (chunkData.yOffset != 0 && chunkData != null) previousY += chunkData.yOffset; //Offsets Y position for next chunk to follow

            //Spawn a flag on the last chunk
            if (chunkCounter == _poolSize)
            {
                chunkData.SpawnFlag(Color.red);
            }

            if (chunkCounter == Mathf.Round(_poolSize * 0.5f)) //Spawn a flag at the halfway mark
            {
                chunkData.SpawnFlag(Color.blue);
            }
        }
    }

    public void SpawnChunk(InputAction.CallbackContext ctx, int chunkID)
    {
        if (ctx.started)
        {
            int direction = 1;
            chunkCounter++;

            GameObject chunk = chunkPrefabs[chunkID].chunkPrefab;
            if (chunk != null && alt && chunkID != 0)
            {
                if (chunkID == 1) chunk = chunkPrefabs[2].chunkPrefab;
                if (chunkID == 3) chunk = chunkPrefabs[4].chunkPrefab;
            }

            if (chunk != null)
            {
                ChunkData chunkData = InstantiateChunk(new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), chunk);
                groundPool.Add(chunkData);
                chunkData.chunkNumber = chunkCounter;

                if (chunkData.yOffset != 0) previousY += chunkData.yOffset; //Offsets Y position for next chunk to follow

                //Updates the UI's position
                levelBuilderUI.transform.position = chunkData.transform.position;
                Vector3 offsetPos = new Vector3(0, chunkData.yOffset, 0);
                levelBuilderUI.transform.position += offsetPos;
            }
        }
    }

    public void SpawnPreset(InputAction.CallbackContext ctx, int presetID)
    {
        if (ctx.started)
        {
            ChunkData lastChunk = null;
            GameObject preset = null;
            int direction = 1;

            if (presetID < chunkPrefabs.Length)
            {
                preset = chunkPrefabs[presetID].chunkPrefab;
            }

            if (preset != null)
            {
                chunkCounter++;
                GameObject _preset = Instantiate(preset, new Vector3(ChunkData.CHUNK_WIDTH * chunkCounter * direction, previousY, 0f), preset.transform.rotation); //spawn the preset
                _preset.transform.SetParent(groundParentTransform);
                previousChunk = preset;

                foreach (Transform child in _preset.transform) //initialize all the child chunks in the preset
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
                if (lastChunk.yOffset != 0) previousY += lastChunk.yOffset;

                //Updates the UI's position
                levelBuilderUI.transform.position = lastChunk.transform.position;
                Vector3 offsetPos = new Vector3(0, lastChunk.yOffset, 0);
                levelBuilderUI.transform.position += offsetPos;
            }
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

                //Updates the Ui's position
                levelBuilderUI.transform.position = chunkData.transform.position;
                Vector3 offsetPos = new Vector3(chunkData.transform.position.x, previousY + chunkData.yOffset, 0);
                levelBuilderUI.transform.position = offsetPos;
                previousY = offsetPos.y;
            }
        }
    }

    public ChunkData GetChunkAtPosition(Vector3 position)
    {
        ChunkData chunk = null;
        foreach(ChunkData _chunk in groundPool)
        {
            if (_chunk.transform.position.x >= position.x)
            {
                chunk = _chunk;
            }
        }

        return chunk;
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
            case "3": SpawnChunk(ctx, 2); break;
            case "4": SpawnChunk(ctx, 3); break;
            case "5": SpawnChunk(ctx, 4); break;
            case "6": SpawnPreset(ctx, 5); break;
            case "7": SpawnPreset(ctx, 6); break;
            case "8": SpawnPreset(ctx, 7); break;
            case "9": SpawnPreset(ctx, 8); break;
            case "0": SpawnChunk(ctx, 9); break;
            case "Cancel": DeletePreviousChunk(ctx); break;
            case "Cycle": SwapType(ctx); break;
        }
    }
    #endregion
}




