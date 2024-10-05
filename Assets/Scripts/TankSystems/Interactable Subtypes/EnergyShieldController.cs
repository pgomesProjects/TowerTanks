using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class EnergyShieldController : TankInteractable
    {
        public float shieldHealth;
        private float maxShieldHealth = 200;

        private void Awake()
        {
            shieldHealth = maxShieldHealth;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Damage(float amount)
        {
            shieldHealth -= amount;
        }
    }
}
