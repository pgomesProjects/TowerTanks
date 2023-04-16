using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum COMBATDIRECTION { Left, Right };

public class EnemySpawnManager : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField, Tooltip("The prefabs for the enemies.")] private GameObject[] enemyPrefabs;
    [SerializeField, Tooltip("The enemy spawners.")] private Transform [] spawnTransforms;
    [Space(10)]

    [Header("Debug Options")]
    [SerializeField, Tooltip("Spawns an enemy.")] private bool debugSpawnEnemy;
    [SerializeField, Tooltip("Spawns an enemy on the left of the tank.")] private bool debugSpawnEnemyLeft;
    [SerializeField, Tooltip("Spawns an enemy on the right of the tank.")] private bool debugSpawnEnemyRight;
    [SerializeField, Tooltip("If true, the debug spawn enemy layer number will override the current layer system.")] private bool debugOverrideEnemyLayers;
    [SerializeField, Range(2, 8), Tooltip("If override is on, the number of enemy layers to spawn.")] private int debugSpawnEnemyLayers = 2;

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
        //Pick a random enemy from the list of enemies and spawn it at a random spawner
        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        int spawnerIndex = Random.Range(0, spawnTransforms.Length);

        if (debugSpawnEnemyLeft)
            spawnerIndex = 0;
        else if (debugSpawnEnemyRight)
            spawnerIndex = 1;

        GameObject newEnemy = Instantiate(enemyPrefabs[enemyIndex], spawnTransforms[spawnerIndex].position, spawnTransforms[spawnerIndex].rotation);

        if((debugSpawnEnemy || debugSpawnEnemyLeft || debugSpawnEnemyRight) && debugOverrideEnemyLayers)
            newEnemy.GetComponent<EnemyController>().CreateLayers((COMBATDIRECTION)spawnerIndex, debugSpawnEnemyLayers);
        else
            newEnemy.GetComponent<EnemyController>().CreateLayers((COMBATDIRECTION)spawnerIndex);

        Vector3 cameraZoomPos = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().transform.position;

        switch ((COMBATDIRECTION)spawnerIndex)
        {
            case COMBATDIRECTION.Left:
                cameraZoomPos.x -= 55;
                break;
            case COMBATDIRECTION.Right:
                cameraZoomPos.x += 55;
                break;
        }

        if (enemyEncounterAni != null)
            StopCoroutine(enemyEncounterAni);

        enemyEncounterAni = CameraEventController.instance.ShowEnemyWithCamera(newEnemy);

        StartCoroutine(enemyEncounterAni);

        LevelManager.instance.currentRound++;

        Debug.Log("===ENEMY #" + LevelManager.instance.currentRound + " HAS SPAWNED!===");
    }

    private void Update()
    {
        if (!enemySpawnerActive)
        {
            if(GameObject.FindGameObjectWithTag("PlayerTank") != null)
            {
                if (GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().SpawnDistanceReached())
                {
                    GetReadyForEnemySpawn();
                }
            }
        }

        //Debug functions
        if (Application.isEditor)
        {
            if (debugSpawnEnemy)
            {
                SpawnRandomEnemy();
                debugSpawnEnemy = false;
            }

            if (debugSpawnEnemyLeft)
            {
                SpawnRandomEnemy();
                debugSpawnEnemyLeft = false;
            }

            if (debugSpawnEnemyRight)
            {
                SpawnRandomEnemy();
                debugSpawnEnemyRight = false;
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
