using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts 
{
    public class HopperHitbox : MonoBehaviour
    {
        public int itemsSold = 0;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Cargo"))
            {
                Cargo cargoItem = collision.gameObject.GetComponent<Cargo>();

                if (cargoItem != null)
                {
                    cargoItem.Sell(1f);
                    itemsSold += 1;
                    //GameManager.Instance.AudioManager.Play("JetpackRefuel");
                }
            }
        }
    }
}
