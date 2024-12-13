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

        [PropertySpace]
        public List<EnemyTankDesign> enemyTankDesigns = new List<EnemyTankDesign>();

        [PropertySpace]
        public List<TankId> tanks = new List<TankId>();

        [TabGroup("Runtime Actions")]
        [Button("Spawn New Tank", ButtonSizes.Small)]
        public TankController SpawnTank(int tier = 1, bool spawnEnemy = true, bool spawnBuilt = false)
        {
            TankId newtank = new TankId();

            if (spawnEnemy)
            {
                //TankNames nameType = Resources.Load<TankNames>("TankNames/PirateNames");
                //newtank.TankName = new TankNameGenerator().GenerateRandomName(nameType);
                newtank.gameObject = Instantiate(tankPrefab, tankSpawnPoint, false);
                newtank.tankType = TankId.TankType.ENEMY;

                //Determine tank design
                int random = Random.Range(0, 4);
                newtank.design = enemyTankDesigns[tier - 1].designs[random];
                newtank.TankName = newtank.design.name;
                newtank.gameObject.name = newtank.TankName;

                if (spawnBuilt)
                {
                    newtank.buildOnStart = true;
                }
            }

            //Assign Values
            newtank.tankScript = newtank.gameObject.GetComponent<TankController>();
            newtank.tankScript.TankName = newtank.TankName;
            newtank.tankScript.tankType = newtank.tankType;
            newtank.gameObject.transform.parent = null;

            tanks.Add(newtank);
            return newtank.tankScript;
        }

        //UNITY METHODS:
        private void Awake()
        {
            //Initialize:
            instance = this; //Singleton-ize this script
            if (tanks.Count > 0)
            {
                playerTank = tanks.Where(tank => tank.tankType == TankId.TankType.PLAYER).FirstOrDefault()?.tankScript;
            }
        }

        //UTILITY METHODS:
        public void MoveSpawnPoint(Vector3 newPosition)
        {
            tankSpawnPoint.position = newPosition;
        }
    }
}
