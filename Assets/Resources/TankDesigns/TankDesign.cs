using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "TankDesign", menuName = "ScriptableObjects/TankDesign", order = 1)]
public class TankDesign : ScriptableObject
{
    [Header("Tank Info")]
    [InlineButton("UpdateName")]
    public string TankName;

    [Header("Instructions:"), Tooltip("The order in which to execute the following build steps in order to construct this tank during runtime. Order is from top to bottom.")]
    public BuildStep[] buildingSteps;

    public void UpdateName()
    {
        TankName = name;
    }

}
