using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class ChunkWeight
{
    public GameObject chunkPrefab;

    [InlineButton("SubOne", "-1")]
    [InlineButton("SubFive", "-5")]
    [InlineButton("SubTen", "-10")]
    [InlineButton("AddTen", "+10")]
    [InlineButton("AddFive", "+5")]
    [InlineButton("AddOne", "+1")]
    [Tooltip("Weight of this chunk being chosen when rolling for a random chunk")] public int weight;
    [Tooltip("When spawning this chunk, the spawner will spawn this many copies of it in a row")] public int bias;
    [Tooltip("Spawner checks whether or not this is a multichunk preset group")] public bool isPreset;

    public void AddOne()
    {
        weight += 1;
    }

    public void AddFive()
    {
        weight += 5;
    }

    public void AddTen()
    {
        weight += 10;
    }

    public void SubOne()
    {
        weight -= 1;
        if (weight < 0) weight = 0;
    }

    public void SubFive()
    {
        weight -= 5;
        if (weight < 0) weight = 0;
    }

    public void SubTen()
    {
        weight -= 10;
        if (weight < 0) weight = 0;
    }
}
