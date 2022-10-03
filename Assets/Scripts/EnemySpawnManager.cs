using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;

    // Start is called before the first frame update
    void Start()
    {
        //Spawn an enemy initially
        Invoke("SpawnRandomEnemy", 5);
    }

    private void SpawnRandomEnemy()
    {
        //Pick a random enemy from the list of enemies and spawn it
        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject newEnemy = Instantiate(enemyPrefabs[enemyIndex], transform.position, transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
