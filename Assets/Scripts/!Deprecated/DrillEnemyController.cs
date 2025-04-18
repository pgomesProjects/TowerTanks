using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
    public class DrillEnemyController : EnemyController
    {
        [SerializeField, Tooltip("The number of layers needed before a cannon can spawn.")] private int layersNeededBeforeCannonSpawn;
        [SerializeField, Tooltip("The percent chance that a layer will spawn a cannon instead of a drill.")] private float chanceOfSpawningCannon;

        private float currentLayerSpawned;

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

            LevelManager.Instance.StartCombatMusic(totalEnemyLayers);

            bool specialLayerSpawned = false;

            for (int i = 0; i < totalEnemyLayers; i++)
            {
                currentLayerSpawned = i + 1;
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
                    randomLayer = 1;
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

            foreach (var cannon in GetComponentsInChildren<EnemyCannonController>())
            {
                StartCoroutine(cannon.FireAtDelay());
            }

            //If the enemy is to the left of the player, reverse the direction variable
            if (enemyDirection == COMBATDIRECTION.Left)
                combatDirectionMultiplier = -1f;
            else
                combatDirectionMultiplier = 1f;
        }

        public override void OnLayerDestroyed()
        {
            base.OnLayerDestroyed();

            //If the enemy is dead or self destructing, do nothing
            if (totalEnemyLayers <= 0 || selfDestructMode)
                return;

            if (!canMove && !AnyActiveDrillsLeft())
                DetermineCollisionForce();
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
                    if (currentLayerSpawned >= layersNeededBeforeCannonSpawn)
                    {
                        Random.InitState(System.DateTime.Now.Millisecond);  //Seeds the randomizer
                        float currentChanceOfSpawningCannon = Random.Range(0, 100);

                        //If the current chance of spawning a cannon is met, put a cannon instead of a drill
                        if (currentChanceOfSpawningCannon < chanceOfSpawningCannon)
                            currentLayerManager.GetCannons().GetChild(1).gameObject.SetActive(true);
                        else
                            currentLayerManager.GetDrills().GetChild(1).gameObject.SetActive(true);
                    }
                    else
                        currentLayerManager.GetDrills().GetChild(1).gameObject.SetActive(true);
                    break;
                case COMBATDIRECTION.Right:
                    if (currentLayerSpawned >= layersNeededBeforeCannonSpawn)
                    {
                        Random.InitState(System.DateTime.Now.Millisecond);  //Seeds the randomizer
                        float currentChanceOfSpawningCannon = Random.Range(0, 100);

                        //If the current chance of spawning a cannon is met, put a cannon instead of a drill
                        if (currentChanceOfSpawningCannon < chanceOfSpawningCannon)
                            currentLayerManager.GetCannons().GetChild(0).gameObject.SetActive(true);
                        else
                            currentLayerManager.GetDrills().GetChild(0).gameObject.SetActive(true);
                    }
                    else
                        currentLayerManager.GetDrills().GetChild(0).gameObject.SetActive(true);
                    break;
            }
        }

        protected override void DetermineBehavior()
        {
            enemyTrait = ENEMYBEHAVIOR.AGGRESSIVE;
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("PlayerTankCollider"))
            {
                Debug.Log("Enemy Is At Player!");
                enemyColliding = true;
                canMove = false;
                if (!AnyActiveDrillsLeft())
                    DetermineCollisionForce();
            }
        }

        protected override void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("PlayerTankCollider"))
            {
                Debug.Log("Enemy Is No Longer At Player!");
                enemyColliding = false;
                canMove = true;
            }
        }

        protected override void AddToList()
        {
            //LevelManager.Instance.currentSessionStats.drillTanksDefeated += 1;
        }
    }
}
