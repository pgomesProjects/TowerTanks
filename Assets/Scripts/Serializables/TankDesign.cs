using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class TankDesign
    {
        [Header("Tank Info:")]
        public string TankName;
        public string aiSettings;

        [Header("Instructions:"), Tooltip("The order in which to execute the following build steps in order to construct this tank during runtime. Order is from top to bottom.")]
        public BuildStep.CellInterAssignment[] coreInteractables = { };
        public BuildStep[] buildingSteps;

        [Header("Cargo Nodes:"), Tooltip("List of positions where cargo can be spawned inside this tank. (Not used for player tank)")]
        public Vector2[] cargoNodes;
    }
}