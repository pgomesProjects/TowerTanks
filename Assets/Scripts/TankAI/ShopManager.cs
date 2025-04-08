using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ShopManager : MonoBehaviour
    {
        public enum ShopItemType 
        { 
            Stack_Random, 
            Stack_RandomWeapon, 
            Stack_RandomNonWeapon, 
            Stack_RandomDefense,
            Stack_RandomEngineering,
            Stack_RandomLogistics,
            Stack_RandomConsumable,
            Cargo_RandomSetOfThree
        }

        [System.Serializable]
        public class ShopItem
        {
            public ShopItemType type;
            public int cost;
        }

        public ShopItem[] defaultStock; //default shop lineup
        public ShopTerminal[] terminals; //list of active shop terminals on the Shop Tank

        private LevelManager levelMan;

        private void OnEnable()
        {
            //InitializeShop();
            levelMan = GameObject.Find("LevelManager")?.GetComponent<LevelManager>();
        }

        public void InitializeShop()
        {
            terminals = GetComponentsInChildren<ShopTerminal>();
            if (terminals.Length == 0) return;

            //Assign Items to Terminals
            for(int t = 0; t < defaultStock.Length; t++)
            {
                terminals[t].item = defaultStock[t];

                //Assign Items to each Terminal
                if(defaultStock[t].type != ShopItemType.Cargo_RandomSetOfThree)
                {
                    TankInteractable interactable = GetStackItem(defaultStock[t]);
                    terminals[t].AssignItem(interactableItem: interactable);
                }
                else
                {
                    CargoId cargo = GetCargoItem(defaultStock[t]);
                    terminals[t].AssignItem(cargoItem: cargo);
                }

                //Update Prices
                terminals[t].UpdatePrice(defaultStock[t].cost);

                //Assign ShopID
                terminals[t].AssignShop(this);
            }
        }

        public TankInteractable GetStackItem(ShopItem item)
        {
            TankInteractable interactable = null;

            TankInteractable[] validInteractables = null;

            //Find Valid Items based on Desired Type
            switch (item.type)
            {
                case ShopItemType.Stack_Random:
                    validInteractables = GameManager.Instance.GetInteractableList();
                    break;
                case ShopItemType.Stack_RandomWeapon:
                    validInteractables = GameManager.Instance.GetInteractablesOfType(TankInteractable.InteractableType.WEAPONS);
                    break;
                case ShopItemType.Stack_RandomNonWeapon:
                    validInteractables = GameManager.Instance.GetInteractablesNotOfType(TankInteractable.InteractableType.WEAPONS);
                    break;
                case ShopItemType.Stack_RandomDefense:
                    validInteractables = GameManager.Instance.GetInteractablesOfType(TankInteractable.InteractableType.DEFENSE);
                    break;
                case ShopItemType.Stack_RandomEngineering:
                    validInteractables = GameManager.Instance.GetInteractablesOfType(TankInteractable.InteractableType.ENGINEERING);
                    break;
                case ShopItemType.Stack_RandomLogistics:
                    validInteractables = GameManager.Instance.GetInteractablesOfType(TankInteractable.InteractableType.LOGISTICS);
                    break;
                case ShopItemType.Stack_RandomConsumable:
                    validInteractables = GameManager.Instance.GetInteractablesOfType(TankInteractable.InteractableType.CONSUMABLE);
                    break;
            }

            //Roll for Item from that List
            if (validInteractables.Length > 0)
            {
                int random = Random.Range(0, validInteractables.Length);
                interactable = validInteractables[random];
            }

            return interactable;
        }

        public CargoId GetCargoItem(ShopItem item)
        {
            CargoId cargo = null;

            //Get a random Cargo Item, excluding the basic cargo crate
            int i = GameManager.Instance.CargoManager.cargoList.Count;

            int random = Random.Range(1, i);
            cargo = GameManager.Instance.CargoManager.cargoList[random];

            return cargo;
        }

        public bool PurchaseItem(int price)
        {
            bool purchased = false;

            if (levelMan != null)
            {
                if (levelMan.CanPlayerAfford(price))
                {
                    //Spend the Resources
                    levelMan.UpdateResources(-price);

                    purchased = true;
                }
                else
                {
                    purchased = false;
                }
            }

            return purchased;
        }
    }
}
