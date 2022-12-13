using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    private List<int> enemyTypeCounters;
    internal bool enemySpawnerActive = false;

    private void Start()
    {
        enemyTypeCounters = new List<int>();
        for(int i = 0; i < enemyPrefabs.Length; i++)
        {
            enemyTypeCounters.Add(0);
        }
    }

    private IEnumerator enemyEncounterAni;
    private void SpawnRandomEnemy()
    {
        //Pick a random enemy from the list of enemies and spawn it
        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject newEnemy = Instantiate(enemyPrefabs[enemyIndex], transform.position, transform.rotation);

        Vector3 cameraZoomPos = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().transform.position;

        if (enemyEncounterAni != null)
            StopCoroutine(enemyEncounterAni);

        enemyEncounterAni = CameraEventController.instance.ShowEnemyWithCamera(40, new Vector3(cameraZoomPos.x + 55, cameraZoomPos.y, cameraZoomPos.z), 4, newEnemy);

        StartCoroutine(enemyEncounterAni);

        LevelManager.instance.currentRound++;

        Debug.Log("===ENEMY #" + LevelManager.instance.currentRound + " HAS SPAWNED!===");
    }

    private void Update()
    {
        if (!enemySpawnerActive)
        {
            if (GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().SpawnDistanceReached())
            {
                GetReadyForEnemySpawn();
            }
        }
    }

    private void GetReadyForEnemySpawn()
    {
        Debug.Log("Enemy Spawn Has Begun!");
        enemySpawnerActive = true;
        int randomTime = Random.Range(5, 8);
        Invoke("SpawnRandomEnemy", randomTime);
        LevelManager.instance.HideGoPrompt();
    }

    public void AddToEnemyCounter(Component component)
    {
        //Add to the enemy list counter based on the type of enemy
        switch (component.GetType().ToString())
        {
            case "DrillEnemyController":
                enemyTypeCounters[1] += 1;
                break;
            default:
                enemyTypeCounters[0] += 1;
                break;
        }
    }

    public int GetEnemyCountAt(int i) => enemyTypeCounters[i];
}
