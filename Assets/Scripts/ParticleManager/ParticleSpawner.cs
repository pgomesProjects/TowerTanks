using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSpawner : MonoBehaviour
{
    public GameObject[] particles;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnParticle(int id, Transform spawnPoint, float scale, Transform parent)
    {
        var particle = Instantiate(particles[id], spawnPoint.position, Quaternion.identity, parent); //set parent to null if you want it to spawn in world space
        particle.transform.localScale *= scale;
    }
}
