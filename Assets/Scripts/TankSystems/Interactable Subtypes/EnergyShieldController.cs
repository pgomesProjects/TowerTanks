using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class EnergyShieldController : TankInteractable, IDamageable
    {
        [Header("Shield Settings")]
        [Tooltip("Current shield health value")] public float shieldHealth;
        private float shieldMaxHealth = 300;
        [Tooltip("If shield health drops below this value, the shield will be disabled")] public float shieldDisableThreshold;
        [Tooltip("If the shield is disabled, it won't recharge over time")] public bool shieldDisabled = false;
        private bool shieldStunned = false;
        private float shieldStunTime = 2f;
        private float shieldStunTimer = 0;
        [Tooltip("Rate per second of how fast the shield depletes when damaged")] public float shieldShrinkRate;
        [Tooltip("Rate per second of how fast the shield recharges over time")] public float shieldRechargeRate;

        public AOERenderer shieldRenderer;
        public CircleCollider2D shieldCollider;
        [Tooltip("Actual radius of the shield's AOE")] public float shieldRadius;
        private float shieldMaxRadius = 3;

        private void Awake()
        {
            base.Awake();
            shieldHealth = shieldMaxHealth;
            shieldRadius = shieldMaxRadius;
            //shieldRenderer.UpdateAOE(100, 3);
        }

        private void Update()
        {
            shieldRenderer.UpdateAOE(100, shieldRadius);
            shieldCollider.radius = shieldRadius * 0.5f;
        }

        private void FixedUpdate()
        {
            base.FixedUpdate();
            UpdateShieldValues();
        }

        public void UpdateShieldValues()
        {
            float shieldRatio = (shieldHealth / shieldMaxHealth);
            shieldRadius = Mathf.Lerp(0, shieldMaxRadius, (shieldRatio));
            if (shieldDisabled) shieldRadius = 0;

            //If we've been hit recently, don't recharge
            if (shieldStunTimer > 0)
            {
                shieldStunTimer -= Time.fixedDeltaTime;
                shieldStunned = true;
            }
            else
            {
                shieldStunTimer = 0;
                shieldStunned = false;
            }

            //Otherwise, Recharge Over Time
            if (!shieldStunned && (shieldHealth < shieldMaxHealth) && !shieldDisabled)
            {
                shieldHealth += shieldRechargeRate * Time.fixedDeltaTime;
                if (shieldHealth > shieldMaxHealth) shieldHealth = shieldMaxHealth;
            }
        }

        public void DisableShield(float duration)
        {
            GameManager.Instance.AudioManager.Play("EngineDyingSFX", this.gameObject);
            StartCoroutine(ShieldDown(duration));
        }

        public IEnumerator ShieldDown(float duration)
        {
            shieldDisabled = true;
            yield return new WaitForSeconds(duration);
            shieldDisabled = false;
        }

        public float Damage(Projectile projectile, Vector2 position)
        {
            float extraDamage = projectile.remainingDamage;

            shieldHealth -= extraDamage;
            shieldStunTimer = shieldStunTime;
            if (shieldHealth < shieldDisableThreshold)
            {
                shieldHealth = shieldDisableThreshold;
                if (!shieldDisabled) DisableShield(6f);
            }

            extraDamage -= shieldHealth;
            return Mathf.Max(0, extraDamage);
        }

        public void Damage(float damage)
        {
            shieldHealth -= damage;
            shieldStunTimer = shieldStunTime;
            if (shieldHealth < 0)
            {
                shieldHealth = 0;
                if (!shieldDisabled) DisableShield(6f);
            }
        }

        public override void Use(bool overrideConditions = false)
        {
            base.Use(overrideConditions);

            if (cooldown <= 0 && !shieldDisabled && !shieldStunned)
            {
                AddShieldCharge(15);
            }
        }

        public void AddShieldCharge(float amount)
        {
            shieldHealth += amount;
            if (shieldHealth > shieldMaxHealth) shieldHealth = shieldMaxHealth;
        }
    }
}
