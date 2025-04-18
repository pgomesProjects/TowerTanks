using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

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
        private Transform innerShield;
        public CircleCollider2D shieldCollider;
        public Animator shieldAnimator;
        [Tooltip("Actual radius of the shield's AOE")] public float shieldRadius;
        private float shieldMaxRadius = 3;

        protected override void Awake()
        {
            base.Awake();
            shieldHealth = shieldMaxHealth;
            shieldRadius = shieldMaxRadius;
            innerShield = shieldRenderer.transform.Find("InnerShield");
            //shieldRenderer.UpdateAOE(100, 3);
        }

        private void Update()
        {
            shieldRenderer.UpdateAOE(100, shieldRadius);
            shieldCollider.radius = shieldRadius * 0.5f;
            innerShield.transform.localScale = new Vector3(shieldRadius * 2, shieldRadius * 2, 1);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            UpdateShieldValues();
        }

        public override void LockIn(GameObject playerID)
        {
            base.LockIn(playerID);

            //Add the pump shield prompt to the HUD
            operatorID.GetCharacterHUD().SetButtonPrompt(GameAction.PumpShield, true);
        }

        public override void Exit(bool sameZone)
        {
            //Remove the pump shield prompt on the HUD
            operatorID.GetCharacterHUD().SetButtonPrompt(GameAction.PumpShield, false);

            base.Exit(sameZone);
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

        public void DisableShield(float duration = -1)
        {
            GameManager.Instance.AudioManager.Play("EngineDyingSFX", this.gameObject);
            HitEffects(transform.position, 0.5f, true);

            if (shieldHealth != 0) shieldHealth = 0;
            shieldDisabled = true;
            if (duration > 0) StartCoroutine(ShieldDown(duration));
        }

        public IEnumerator ShieldDown(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (!isBroken) shieldDisabled = false;
        }

        public float Damage(Projectile projectile, Vector2 position)
        {
            float extraDamage = projectile.remainingDamage;

            shieldHealth -= extraDamage;
            shieldStunTimer = shieldStunTime;

            //Scale Particle Size based on Damage
            float scale = Mathf.Lerp(0.6f, 0.2f, ((extraDamage + 5) / 100f));

            HitEffects(position, scale, false);
            if (extraDamage >= 75) { HitEffects(position, scale, true); }

            if (shieldHealth < shieldDisableThreshold)
            {
                shieldHealth = shieldDisableThreshold;
                if (!shieldDisabled) DisableShield(6f);
            }

            extraDamage -= shieldHealth;
            return Mathf.Max(0, extraDamage);
        }

        public float Damage(float damage, bool triggerHitEffects = false)
        {
            shieldHealth -= damage;
            shieldStunTimer = shieldStunTime;

            //Scale Particle Size based on Damage
            float scale = Mathf.Lerp(0.6f, 0.2f, ((damage + 5) / 100f));

            HitEffects(this.transform.position, scale, false);
            if (shieldHealth < 0)
            {
                shieldHealth = 0;
                if (!shieldDisabled) DisableShield(6f);
            }

            return damage;
        }

        public override void Use(bool overrideConditions = false)
        {
            base.Use(overrideConditions);

            if (isBroken) return;

            if (cooldown <= 0 && !shieldDisabled)
            {
                AddShieldCharge(15);
                HitEffects(transform.position, 0.3f, false);

                //Apply Haptics to Operator
                if (operatorID != null)
                {
                    HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("QuickJolt");
                    GameManager.Instance.SystemEffects.ApplyControllerHaptics(operatorID.GetPlayerData().playerInput, setting); //Apply haptics
                }
            }
        }

        public void AddShieldCharge(float amount)
        {
            shieldHealth += amount;
            if (shieldHealth > shieldMaxHealth) shieldHealth = shieldMaxHealth;
        }

        public void HitEffects(Vector2 position, float particleScale, bool outsideShield)
        {
            shieldAnimator.Play("Flash", 0, 0);

            //Particle FX
            GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(16, position, particleScale, shieldRenderer.gameObject.transform);

            if (outsideShield)
            {
                ParticleSystem particleSystem = particle.GetComponent<ParticleSystem>();
                particleSystem.GetComponent<ParticleSystemRenderer>().maskInteraction = SpriteMaskInteraction.None;
            }
        }

        public override void Break()
        {
            base.Break();

            DisableShield();
        }

        public override void Fix()
        {
            base.Fix();

            shieldDisabled = false;
        }
    }
}
