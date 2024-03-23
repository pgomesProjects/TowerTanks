using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TankManager : SerializedMonoBehaviour
{
    public GameObject tankPrefab;
    public Transform tankSpawnPoint;

    [PropertySpace]
    public List<TankId> tanks = new List<TankId>();
    
    [TabGroup("Runtime Actions")]
    [Button("Spawn New Tank", ButtonSizes.Small)]
    public void SpawnTank()
    {
        TankId newtank = new TankId();
        newtank.tankType = TankId.TankType.ENEMY;
        newtank.TankName = new TankNameGenerator().GenerateRandomName();
        newtank.gameObject = Instantiate(tankPrefab, tankSpawnPoint, false);
        newtank.gameObject.name = newtank.TankName;

        //Assign Name
        newtank.gameObject.GetComponent<TankController>().TankName = newtank.TankName;
        newtank.gameObject.transform.parent = null;

        tanks.Add(newtank);
    }
    [TabGroup("Runtime Actions")]
    [Button("Move Spawn Point")]
    public void MoveSpawnPoint()
    {
        tankSpawnPoint.position += new Vector3(15, 0, 0);
    }

    public void ConstructTank(Transform parentTank)
    {

    }
}
