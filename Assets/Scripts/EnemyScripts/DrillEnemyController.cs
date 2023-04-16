using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillEnemyController : EnemyController
{

    public override void CreateLayers(COMBATDIRECTION enemyDirection, int debugEnemyLayers = 0)
    {
        if (debugEnemyLayers <= 0)
        {
            float extraLayers = FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(1) * waveCounter;
            //Debug.Log("Extra Layers For Drill Tank #" + (FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(1) + 1).ToString() + ": " + extraLayers);
            totalEnemyLayers = 2 + Mathf.FloorToInt(extraLayers);
        }
        else
            totalEnemyLayers = debugEnemyLayers;

        totalEnemyLayers = Mathf.Clamp(totalEnemyLayers, 2, MAXLAYERS);

        //If the game is on debug mode, override the enemy layers
        if(GameSettings.debugMode)
            LayerSpawnDebugMode();

        LevelManager.instance.StartCombatMusic(totalEnemyLayers);

        bool specialLayerSpawned = false;

        for (int i = 0; i < totalEnemyLayers; i++)
        {
            int randomLayer;

            //Always spawn a drill on the first layer
            if (i == 0)
            {
                randomLayer = 1;
                specialLayerSpawned = true;
                SpawnLayer(randomLayer, i, enemyDirection);
                continue;
            }

            if (i % 2 == 1 && !specialLayerSpawned)
            {
                randomLayer = 11;
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
                    randomLayer = Random.Range(0, 1);
                    if (randomLayer != 0)
                        specialLayerSpawned = true;
                }
            }

            SpawnLayer(randomLayer, i, enemyDirection);
        }

        //If the enemy is to the left of the player, reverse the direction variable
        if (enemyDirection == COMBATDIRECTION.Left)
            combatDirectionMultiplier = -1f;
        else
            combatDirectionMultiplier = 1f;
    }

    /// <summary>
    /// Spawns a weapon on the left or right of the enemy.
    /// </summary>
    protected override void SpawnWeapon(LayerManager currentLayerManager, COMBATDIRECTION enemyDirection)
    {
        Debug.Log("Spawn Drill!");

        switch (enemyDirection)
        {
            case COMBATDIRECTION.Left:
                currentLayerManager.GetDrills().GetChild(1).gameObject.SetActive(true);
                break;
            case COMBATDIRECTION.Right:
                currentLayerManager.GetDrills().GetChild(0).gameObject.SetActive(true);
                break;
        }
    }

    protected override void DetermineBehavior()
    {
        enemyTrait = ENEMYBEHAVIOR.AGGRESSIVE;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTankCollider"))
        {
            Debug.Log("Enemy Is At Player!");
            enemyColliding = true;
            canMove = false;
        }
    }
    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTankCollider"))
        {
            Debug.Log("Enemy Is No Longer At Player!");
            enemyColliding = false;
            canMove = true;
        }
    }

    protected override void AddToList()
    {
        LevelManager.instance.currentSessionStats.drillTanksDefeated += 1;
    }
}
