using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class ParticleSpawner : MonoBehaviour
{
    //Objects & Components:
    [SerializeField, Tooltip("Current list of all implemented particle effects.")] private GameObject[] particles;

    //Settings:
    [SerializeField, Tooltip("Path in game files to folder containing particle effect prefabs.")] private string particleFolderPath;
    [Button("Refresh List")] public void RefreshList()
    {

    }

    //UTILITY METHODS:
    /// <summary>
    /// Spawns particle system of given name at given position.
    /// </summary>
    /// <param name="effectName">Name of particle effect, must be in particle list.</param>
    public void SpawnParticle(string effectName, Vector2 position, float scale = 1)
    {

    }
    /// <summary>
    /// Spawns given particle system from array.
    /// </summary>
    /// <param name="id">Array ID of target particle system.</param>
    public void SpawnParticle(int id, Transform spawnPoint, float scale, Transform parent)
    {
        var particle = Instantiate(particles[id], spawnPoint.position, Quaternion.identity, parent); //set parent to null if you want it to spawn in world space
        particle.transform.localScale *= scale;
    }
}
