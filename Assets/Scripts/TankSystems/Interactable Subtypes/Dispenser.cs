using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class Dispenser : TankInteractable
    {
        [Header("Dispenser Settings:")]
        [Tooltip("Position to spawn items.")]                                               public Transform spawnPoint;
        [Tooltip("The item to dispense.")]                                                  public GameObject item;
        [Tooltip("Dispense the indicated amount of the selected item before running out.")] public int amount = -1; //-1 is infinite supply
        [Tooltip("Time in seconds before another item can be dispensed.")]                  public float dispenseCooldown = 0;
                                                                                            private float dispenseTimer = 0;
        [Tooltip("If true, dispenser will not spawn items if too many already exist.")]     public int maxItems = 1;
        private List<GameObject> activeItems = new List<GameObject>();

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (dispenseTimer >= 0)
            {
                dispenseTimer -= Time.fixedDeltaTime;
                if (dispenseTimer <= 0)
                {
                    dispenseTimer = 0;
                    DispenseItem();
                }
            }
        }

        public void DispenseItem()
        {
            if (item == null) return;
            if (CheckItems())
            {
                if (amount > 0 || amount <= -1)
                {
                    GameObject _item = Instantiate(item, spawnPoint.position, Quaternion.identity, null);
                    Cargo cargoScript = _item.GetComponent<Cargo>();
                    if (cargoScript != null) cargoScript.ignoreInit = true;

                    activeItems.Add(_item);
                    dispenseTimer = dispenseCooldown;
                    //other effects
                    amount -= 1;
                }
            }
        }

        public bool CheckItems()
        {
            if (maxItems < 0) return true;

            //Check Count of Active Items
            int count = 0;

            List<GameObject> temp = new List<GameObject>();
            foreach(GameObject _item in activeItems)
            {
                if (_item != null)
                {
                    count++;
                }
                else temp.Add(_item);
            }

            //Cleanse List of empty items
            foreach(GameObject _item in temp)
            {
                activeItems.Remove(_item);
            }
            temp.Clear();

            if (count < maxItems) return true;
            else return false;
        }
    }
}
