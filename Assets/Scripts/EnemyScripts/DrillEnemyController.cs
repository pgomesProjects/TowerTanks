using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillEnemyController : EnemyController
{
    protected override void CreateLayers()
    {
        float extraLayers = FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(1) * waveCounter;
        //Debug.Log("Extra Layers For Drill Tank #" + (FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(1) + 1).ToString() + ": " + extraLayers);
        totalEnemyLayers = 2 + Mathf.FloorToInt(extraLayers);
        totalEnemyLayers = Mathf.Clamp(totalEnemyLayers, 2, MAXLAYERS);
        LevelManager.instance.StartCombatMusic(totalEnemyLayers);

        bool specialLayerSpawned = false;

        for (int i = 0; i < totalEnemyLayers; i++)
        {
            int randomLayer;

            //Always spawn a drill on the first layer
            if (i == 0)
            {
                randomLayer = spawnableLayers.Length - 1;
                specialLayerSpawned = true;
                SpawnLayer(randomLayer, i);
                continue;
            }

            if (i % 2 == 1 && !specialLayerSpawned)
            {
                randomLayer = spawnableLayers.Length - 1;
            }
            else
            {
                if (i > 0 && i % 2 == 0)
                {
                    specialLayerSpawned = false;
                }
                if (specialLayerSpawned)
                    randomLayer = 0;
                else
                {
                    randomLayer = Random.Range(0, spawnableLayers.Length);
                    if (randomLayer != 0)
                        specialLayerSpawned = true;
                }
            }

            SpawnLayer(randomLayer, i);
        }
    }

    protected override void DetermineBehavior()
    {
        enemyTrait = ENEMYBEHAVIOR.AGGRESSIVE;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            Debug.Log("Enemy Is At Player!");
            enemyColliding = true;
        }
    }

    protected override void AddToList()
    {
        LevelManager.instance.currentSessionStats.drillTanksDefeated += 1;
    }
}
