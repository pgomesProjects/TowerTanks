using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ShopTerminal : TankInteractable
    {
        [Header("Shop Item Settings:")]
        private ShopManager shopMan;
        public ShopManager.ShopItem item;
        public TankInteractable interactableForSale;
        public CargoId cargoForSale;

        public GameObject interactableUI;
        public TextMeshProUGUI priceText;
        private Image interactableIcon;

        public Transform[] cargoSpots; //spots to spawn cargo at

        private LevelManager levelMan;

        public void AssignShop(ShopManager shop) //Assign a Shop Manager to this Terminal (which shop does it belong to?)
        {
            shopMan = shop;
        }

        public void AssignItem(TankInteractable interactableItem = null, CargoId cargoItem = null)
        {
            interactableForSale = interactableItem;
            cargoForSale = cargoItem;

            if (interactableForSale != null)
            {
                interactableIcon.sprite = interactableItem.uiImage;
            }

            if (cargoForSale != null)
            {
                switch (item.type)
                {
                    case ShopManager.ShopItemType.Cargo_RandomSetOfThree:
                        SpawnCargoSet();
                        break;
                }
            }
        }

        public void UpdatePrice(int price)
        {
            priceText.text = "" + price;
        }

        //RUNTIME VARIABLES
        public void Awake()
        {
            base.Awake();

            interactableIcon = interactableUI.transform.Find("InteractableIcon").GetComponent<Image>();
        }

        private void Update()
        {
            UpdateUI();
        }

        public override void Use(bool overrideConditions = false)
        {
            base.Use(overrideConditions);

            if (cooldown <= 0)
            {
                bool hasItem = false;
                if (interactableForSale != null) hasItem = true;
                if (cargoForSale != null) hasItem = true;

                //Try and Purchase the Item
                if (hasItem)
                {
                    if (shopMan.PurchaseItem(item.cost)) //Purchased!
                    {
                        //If it's an Interactable
                        if (interactableForSale != null)
                        {
                            INTERACTABLE interactable = GameManager.Instance.TankInteractableToEnum(interactableForSale);
                            StackManager.AddToStack(interactable);

                            interactableForSale = null;
                        }

                        //If it's Cargo
                        if (cargoForSale != null)
                        {
                            //Allow Players to safely take the Sold Cargo

                            cargoForSale = null;
                        }
                    }
                }
            }
        }

        public void UpdateUI()
        {
            if (interactableForSale != null)
            {
                interactableUI.SetActive(true);
            }
            else
            {
                interactableUI.SetActive(false);

                if (cargoForSale == null)
                {
                    priceText.gameObject.SetActive(false);
                }
            }
        }

        public void SpawnCargoSet()
        {
            foreach (Transform position in cargoSpots) //go through each transform
            {
                GameObject prefab = null;
                foreach (CargoId cargoItem in GameManager.Instance.CargoManager.cargoList)
                {
                    if (cargoItem.id == cargoForSale.id) //if the sold item id matches an object in the cargomanager
                    {
                        prefab = cargoItem.cargoPrefab; //get the object we need to spawn from the list
                    }
                }

                GameObject _item = Instantiate(prefab, position, false); //spawn the object at that transform
                _item.GetComponent<Cargo>().ignoreInit = true; //ignoring the initial spawn settings
            }
        }
    }
}
