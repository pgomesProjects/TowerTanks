using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts 
{
    public class HopperHitbox : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Cargo"))
            {
                Cargo cargoItem = collision.gameObject.GetComponent<Cargo>();

                if (cargoItem != null)
                {
                    cargoItem.Sell(1f);
                    //GameManager.Instance.AudioManager.Play("JetpackRefuel");
                }
            }
        }
    }
}
