using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyTankDesign
{
    [Tooltip("Category of difficulty / firepower / size this tank is classified in"), SerializeField] public int tier;

    public List<TextAsset> designs = new List<TextAsset>();
}
