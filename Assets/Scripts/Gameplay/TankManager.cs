using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TankManager : SerializedMonoBehaviour
{
    public List<TankId> tanks = new List<TankId>();

    [TabGroup("Runtime Actions")]
    [Button("Spawn New Tank", ButtonSizes.Small)]
    public void SpawnTank()
    {
        TankId newtank = new TankId();
        newtank.tankType = TankId.TankType.ENEMY;
        newtank.TankName = new TankNameGenerator().GenerateRandomName();

        tanks.Add(newtank);
    }
}
