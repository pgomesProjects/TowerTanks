using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class EnergyShieldController : TankInteractable, IDamageable
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
        public float Damage(Projectile projectile, Vector2 position)
        {
            float extraDamage = projectile.remainingDamage;
            shieldHealth -= extraDamage;
            extraDamage -= shieldHealth;
            return Mathf.Max(0, extraDamage);
        }
        public void Damage(float damage)
        {
            shieldHealth -= damage;
        }
    }
}
