using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CargoManager : MonoBehaviour
{
    public List<CargoId> cargoList = new List<CargoId>();

    public string[] cargoWeights;

    private void Awake()
    {
        SetupWeights();
    }

    public void SetupWeights()
    {
        int count = 0;
        int length = 0;
        foreach (CargoId cargo in cargoList) //Get weights from every available chunk in the spawner
        {
            length += cargo.weight;
        }

        cargoWeights = new string[length]; //sets up total weight values

        foreach (CargoId cargo in cargoList) //assigns weights to spawner array
        {
            if (cargo.weight > 0)
            {
                for (int i = 0; i < cargo.weight; i++)
                {
                    cargoWeights[count] = cargo.cargoPrefab.name;
                    count++;
                }
            }
        }
    }

    public GameObject GetRandomCargo()
    {
        GameObject cargo = null;

        int random = Random.Range(0, cargoWeights.Length);

        foreach (CargoId weight in cargoList)
        {
            if (weight.cargoPrefab.name == cargoWeights[random])
            {
                cargo = weight.cargoPrefab;
            }
        }

        return cargo;
    }
}
