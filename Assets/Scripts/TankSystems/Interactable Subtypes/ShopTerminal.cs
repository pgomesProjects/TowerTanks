using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ShopTerminal : TankInteractable
    {
        [Header("Shop Item Settings:")]
        public ShopManager.ShopItem item;
    }
}
