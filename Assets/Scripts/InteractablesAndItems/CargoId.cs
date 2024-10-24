using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class CargoId
{
    public string id; //identifier for this item
    public GameObject cargoPrefab;

    [InlineButton("SubOne", "-1")]
    [InlineButton("SubFive", "-5")]
    [InlineButton("SubTen", "-10")]
    [InlineButton("AddTen", "+10")]
    [InlineButton("AddFive", "+5")]
    [InlineButton("AddOne", "+1")]
    [Tooltip("Weight of this item being chosen when rolling for a random cargo item")] public int weight;

    public void AddOne()
    {
        weight += 1;
    }

    public void AddFive()
    {
        weight += 5;
    }

    public void AddTen()
    {
        weight += 10;
    }

    public void SubOne()
    {
        weight -= 1;
        if (weight < 0) weight = 0;
    }

    public void SubFive()
    {
        weight -= 5;
        if (weight < 0) weight = 0;
    }

    public void SubTen()
    {
        weight -= 10;
        if (weight < 0) weight = 0;
    }
}
