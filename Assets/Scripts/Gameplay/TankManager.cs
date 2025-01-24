using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

namespace TowerTanks.Scripts
{
    public class TankManager : SerializedMonoBehaviour
    {
        public static TankManager instance;
        public GameObject tankPrefab;
        public Transform tankSpawnPoint;
        internal TankController playerTank;

        public static System.Action<TankController> OnPlayerTankAssigned;

        [PropertySpace]
        public List<EnemyTankDesign> enemyTankDesigns = new List<EnemyTankDesign>();
        public List<EnemyTankDesign> merchantTankDesigns = new List<EnemyTankDesign>();
        private List<TextAsset> spawnedThisMission = new List<TextAsset>();

        public List<Sprite> tankFlagSprites = new List<Sprite>();

        [PropertySpace]
        public List<TankId> tanks = new List<TankId>();

        [TabGroup("Runtime Actions")]
        [Button("Spawn New Tank", ButtonSizes.Small)]
        public TankController SpawnTank(int tier = 1, TankId.TankType typeToSpawn = TankId.TankType.NEUTRAL, bool spawnBuilt = true)
        {
            TankId newtank = new TankId();

            if (typeToSpawn == TankId.TankType.ENEMY)
            {
                //TankNames nameType = Resources.Load<TankNames>("TankNames/PirateNames");
                //newtank.TankName = new TankNameGenerator().GenerateRandomName(nameType);
                newtank.gameObject = Instantiate(tankPrefab, tankSpawnPoint, false);
                newtank.tankType = TankId.TankType.ENEMY;
                newtank.tankBrain = newtank.gameObject.GetComponent<TankAI>();
                
                var aiSettings = LoadTankAISettings(tier);
                if (aiSettings != null)
                {
                    newtank.tankBrain.aiSettings = aiSettings;
                }

                //Determine tank design
                TextAsset design = null;
                int counter = 100;
                while (design == null)
                {
                    //Roll for a design
                    int random = Random.Range(0, enemyTankDesigns[tier - 1].designs.Count);
                    
                    design = enemyTankDesigns[tier - 1].designs[random];

                    if (!spawnedThisMission.Contains(design)) //if we haven't spawned this design yet
                    {
                        spawnedThisMission.Add(design);
                        break;
                    }
                    else //we have already spawned this design
                    {
                        counter -= 1;
                        if (counter <= 0) //break potentially infinite loop
                        {
                            break;
                        }
                        design = null;
                        continue;
                    }
                }

                newtank.design = design;
                newtank.TankName = newtank.design.name;
                newtank.gameObject.name = newtank.TankName;

                if (spawnBuilt)
                {
                    newtank.buildOnStart = true;
                }
                newtank.tankBrain.enabled = true;
            }

            if (typeToSpawn == TankId.TankType.NEUTRAL)
            {
                //TankNames nameType = Resources.Load<TankNames>("TankNames/PirateNames");
                //newtank.TankName = new TankNameGenerator().GenerateRandomName(nameType);
                newtank.gameObject = Instantiate(tankPrefab, tankSpawnPoint, false);
                newtank.tankType = TankId.TankType.NEUTRAL;
                newtank.tankBrain = newtank.gameObject.GetComponent<TankAI>();

                //Determine tank design
                //int random = Random.Range(0, 4);
                newtank.design = merchantTankDesigns[0].designs[0];
                newtank.TankName = newtank.design.name;
                newtank.gameObject.name = newtank.TankName;

                if (spawnBuilt)
                {
                    newtank.buildOnStart = true;
                }
                newtank.tankBrain.enabled = true;
            }

            //Assign Values
            newtank.tankScript = newtank.gameObject.GetComponent<TankController>();
            newtank.tankScript.TankName = newtank.TankName;
            newtank.tankScript.tankType = newtank.tankType;
            newtank.gameObject.transform.parent = null;

            //Despawn Obstacles
            int baseChunk = ChunkLoader.Instance.GetChunkAtPosition(tankSpawnPoint.position).chunkNumber;
            ChunkLoader.Instance.DespawnObstacles(baseChunk, 2);

            tanks.Add(newtank);
            return newtank.tankScript;
        }
        
        private TankAISettings LoadTankAISettings(int tier)
        {
            return Resources.Load<TankAISettings>($"TankAISettings/Tier {tier} AI Settings");
        }

        //UNITY METHODS:
        private void Awake()
        {
            //Initialize:
            instance = this; //Singleton-ize this script
            if (tanks.Count > 0)
            {
                playerTank = tanks.FirstOrDefault(tank => tank.tankType == TankId.TankType.PLAYER)?.tankScript;
            }
        }

        private void OnEnable()
        {
            if (playerTank != null)
                OnPlayerTankAssigned?.Invoke(playerTank);
        }

        //UTILITY METHODS:
        public void MoveSpawnPoint(Vector3 newPosition)
        {
            tankSpawnPoint.position = newPosition;
        }
    }
}
