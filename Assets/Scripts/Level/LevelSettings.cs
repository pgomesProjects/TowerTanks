using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Internal;
#if UNITY_EDITOR 
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "LevelSettings", menuName = "ScriptableObjects/LevelSettings", order = 1)]
    public class LevelSettings : SerializedScriptableObject
    {
        [Header("Chunk Pool:")]
        [SerializeField, Tooltip("The chunk prefabs to use for spawning new chunks in the level.")] public ChunkWeight[] chunkPrefabs;
        [SerializeField, Tooltip("How many chunks to spawn in the level.")] public int poolSize = 50;

        [Header("Obstacles:")]
        [SerializeField, Tooltip("Obstacles, hazards, and traps used throughout the level.")] public ObstacleWeight[] obstacles;
        [SerializeField, Tooltip("Chance on any given chunk that an obstacle can spawn on a valid node.")] public float obstacleChance;

        [Header("Landmarks:")]
        [SerializeField, Tooltip("Visual elements added to the back of the ground layer in the level.")] public GameObject[] landmarks;
        [SerializeField, Tooltip("The chance that a landmark will spawn on any valid chunk.")] public float landmarkChance;
        [SerializeField, Tooltip("How many chunks must spawn (minimum) between each landmark.")] public int landMarkOffset;

        [Header("Procedural Variables")]
        [SerializeField, Tooltip("When false, the same bias can't happen twice in a row")] public bool biasesCanRepeat;
    }
}
