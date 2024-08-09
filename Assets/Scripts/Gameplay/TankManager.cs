using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TankManager : SerializedMonoBehaviour
{
    public GameObject tankPrefab;
    public Transform tankSpawnPoint;

    [PropertySpace]
    public List<EnemyTankDesign> enemyTankDesigns = new List<EnemyTankDesign>();

    [PropertySpace]
    public List<TankId> tanks = new List<TankId>();
    
    [TabGroup("Runtime Actions")]
    [Button("Spawn New Tank", ButtonSizes.Small)]
    public TankController SpawnTank(int tier, bool spawnEnemy = true, bool spawnBuilt = false)
    {
        TankId newtank = new TankId();

        if (spawnEnemy)
        {
            TankNames nameType = Resources.Load<TankNames>("TankNames/PirateNames");
            newtank.TankName = new TankNameGenerator().GenerateRandomName(nameType);
            newtank.gameObject = Instantiate(tankPrefab, tankSpawnPoint, false);
            newtank.gameObject.name = newtank.TankName;
            newtank.tankType = TankId.TankType.ENEMY;

            //Determine tank design
            int random = Random.Range(0, 4);
            newtank.design = enemyTankDesigns[tier - 1].designs[random];

            if (spawnBuilt)
            {
                newtank.buildOnStart = true;
            }
        }

        //Assign Values
        TankController tankScript = newtank.gameObject.GetComponent<TankController>();
        tankScript.TankName = newtank.TankName;
        tankScript.tankType = newtank.tankType;
        newtank.gameObject.transform.parent = null;

        tanks.Add(newtank);
        return tankScript;
    }
    
    public void MoveSpawnPoint(Vector3 newPosition)
    {
        tankSpawnPoint.position = newPosition;
    }
}
