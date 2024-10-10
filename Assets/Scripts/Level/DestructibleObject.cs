using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class DestructibleObject : MonoBehaviour
    {
        public float maxHealth;
        public float health;
        private SpriteMask[] damageMasks;

        public void Awake()
        {
            health = maxHealth;
            damageMasks = GetComponentsInChildren<SpriteMask>();
            if (damageMasks.Length > 0)
            {
                foreach (SpriteMask mask in damageMasks)
                {
                    mask.enabled = false;
                }
            }
        }

        public float Damage(float amount)
        {
            float tempHealth = health;
            float healthLost = 0;

            health -= amount;
            if (health < 0) health = 0;
            healthLost = tempHealth - health;

            if (health <= 0)
            {
                Projectile projectile = GetComponent<Projectile>();
                if (projectile != null)
                {
                    if (projectile.type == Projectile.ProjectileType.OTHER)
                    {
                        projectile.Hit(null);
                    }
                }
                else
                {
                    //Debug.Log("No projectile found.");
                    Destroy(gameObject);
                }
            }

            EvaluateDamage();
            return healthLost;
        }

        private void EvaluateDamage()
        {
            float damageThresholdSegment = maxHealth / damageMasks.Length; //67
            for (int i = 0; i < damageMasks.Length; i++)
            {
                if ((i + 1) * Mathf.RoundToInt(damageThresholdSegment) > health)
                {
                    damageMasks[i].enabled = true;
                }
            }
        }
    }
}
