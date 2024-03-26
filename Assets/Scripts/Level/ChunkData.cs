using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkData: MonoBehaviour
{
    public float chunkNumber;

    public enum ChunkType { FLAT, SLOPEUP, SLOPEDOWN, RAMPUP, RAMPDOWN, PRESET};
    public ChunkType chunkType;

    public bool IsActive { get; private set; }
    public bool isInitialized { get; private set; }

    public const float CHUNK_WIDTH = 30f;
    public float yOffset;

    [SerializeField, Tooltip("The chance for a trap to generate.")] private float chanceToGenerateTrap = 50f;
    [SerializeField, Tooltip("The trap GameObject.")] GameObject chunkTrap;
    [SerializeField, Tooltip("Flag that signals the end of the level.")] GameObject flag;
    private Transform flagSpawn; //place to spawn a flag if needed

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
            GenerateTrap();
            isInitialized = true;
        }
    }

    /// <summary>
    /// Generates a random trap.
    /// </summary>
    private void GenerateTrap()
    {
        if (Random.Range(0f, 100f) <= chanceToGenerateTrap)
        {
            chunkTrap.GetComponent<SpriteRenderer>().color = new Color(Random.value, Random.value, Random.value);
            chunkTrap.SetActive(true);
        }
        else
            chunkTrap.SetActive(false);
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
