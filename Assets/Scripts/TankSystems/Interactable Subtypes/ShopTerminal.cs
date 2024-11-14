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
        public TankInteractable interactableForSale;
        public CargoId cargoForSale;

        public void AssignItem(TankInteractable interactableItem = null, CargoId cargoItem = null)
        {
            interactableForSale = interactableItem;
            cargoForSale = cargoItem;
        }
    }
}
