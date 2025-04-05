using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "New Inventory Item", menuName = "ScriptableObjects/Inventory Item")]
    public class InventoryItem : ScriptableObject
    {
        public new string name;
        public Sprite icon;
        public InventoryHUD.BarType barType;
    }
}
