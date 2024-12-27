using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class EngineController : TankInteractable
    {
        //Objects & Components
        [Tooltip("Transforms to spawn particles from when used."), SerializeField] private Transform[] particleSpots;

        //Settings:
        [Header("Engine Settings:")]
        public bool isPowered;
        public float power; //Total horsepower being output by this engine
        public bool isSurging;
        public float boostMultiplier; //Multiplier on speed when boosting/surging

        private float smokePuffRate = 1f;
        private float smokePuffTimer = 0;

        public Color temperatureLowColor;
        public Color temperatureHighColor;

        [Header("Pressure:")]
        public float pressure = 0;
        public float pressureReleaseSpeed; //how fast pressure drops over time
        public float dangerZoneThreshold; //threshold pressure needs to be above for overdrive
        private bool overdriveActive = false;
        public float overDriveOffset = 1f; //multiplier on engine rates while overdrive is active
        private float pressureReleaseCd = 0;
        [SerializeField, Tooltip("The settings for the adding pressure haptics.")] private HapticsSettings enginePressureHaptics;

        [Header("Charge Settings:")]
        public float maxChargeTime;
        public float minChargeTime = 0f;
        public float chargeTimer = 0;
        private float targetChargeOffset = 0.2f;
        public float targetCharge;
        private float minTargetCharge = 0;
        private float maxTargetCharge = 0;
        public bool chargeStarted { get; private set; }

        TimingGauge currentGauge;

        [Header("UI:")]
        public SpriteRenderer[] boilerSprites;
        public Transform danger;
        public SpriteRenderer[] dangerSprites;
        private float targetValue = 1f;
        private float targetTimer = 0f;
        public Transform pressureBar;

        [Header("Explosion Settings:")]
        public float explosionChance; //chance out of 100 that an explosion will happen when conditions are met
        private float explosionChanceOriginal;
        public float explosionTime; //how long it takes to trigger an explosion when conditions are met
        private bool canExplode = false;
        private float explosionTimeOriginal;
        public float explosionTimer = 0;

        [Header("Debug Controls:")]
        public bool addPressure;

        //Input
        public bool repairInputHeld;

        private void Start()
        {
            explosionTimeOriginal = explosionTime;
            explosionChanceOriginal = explosionChance;
            chargeStarted = false;

            maxTargetCharge = maxChargeTime - (targetChargeOffset * 2f);
            minTargetCharge = (targetChargeOffset * 2f);
        }

        // Update is called once per frame
        void Update()
        {
            //Debug settings:
            if (addPressure) { AddPressure(15, true, true); addPressure = false; }

            UpdateUI();
            UpdatePowerOutput();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            UpdatePressure();
            CheckForFire();

            //Input
            if (hasOperator)
            {
                if (operatorID.interactInputHeld && chargeStarted)
                {
                    chargeTimer += Time.deltaTime;
                }

                if (chargeTimer >= maxChargeTime)
                {
                    CheckCharge();
                }
            }
        }

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Loads (amount) coal into the engine.
        /// </summary>
        public void AddPressure(int amount, bool enableSounds = true, bool surgeSpeed = false)
        {
            //Increase coal total:
            pressure += amount;

            if (pressure > 100)
            {
                pressure = 100;
                float random = Random.Range(0f, 100f);
                float chance = explosionChance;
                //if (parentCell.isOnFire) chance += explosionChance;

                if (random < chance && parentCell.isOnFire == false) parentCell.Ignite();

                explosionChance += 5f;
            }

            if (enableSounds)
            { 
                //Other effects:
                GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[0].position, 0.15f, null);
                GameManager.Instance.AudioManager.Play("CoalLoad", this.gameObject); //Play loading clip
                if (operatorID != null) GameManager.Instance.SystemEffects.ApplyControllerHaptics(operatorID.GetPlayerData().playerInput, enginePressureHaptics); //Apply haptics
            }

            //Small Speed Boost
            if (!isSurging && surgeSpeed)
            {
                float duration = 0.5f;
                float force = boostMultiplier * 2f;

                if (pressure >= 50)
                {
                    duration = 0.8f;
                    force += 5;
                }

                if (pressure >= dangerZoneThreshold)
                {
                    duration = 1.1f;
                    force += 5;
                }

                StartCoroutine(SpeedSurge(duration, force));
            }
        }

        private void UpdatePressure()
        {
            float lowerSpeed = pressureReleaseSpeed * Time.deltaTime;
            float pressureDif = (50f + (pressure * 0.5f)) / 100f; //slows down the closer it gets to 0

            if (!chargeStarted && repairInputHeld && isPowered)
            {
                lowerSpeed *= 10f;
                if (!GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", this.gameObject)) GameManager.Instance.AudioManager.Play("SteamExhaustLoop", this.gameObject);
            }
            else if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", this.gameObject)) GameManager.Instance.AudioManager.Stop("SteamExhaustLoop", this.gameObject);

            if (pressure > 0)
            {
                if (!isPowered) isPowered = true;
                pressure -= lowerSpeed * pressureDif;

                //Puff Smoke
                smokePuffTimer += Time.deltaTime;
                if (smokePuffTimer >= smokePuffRate)
                {
                    smokePuffTimer = 0;
                    GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[0].position, 0.1f, null);
                }
            }
            else
            {
                if (isPowered)
                {
                    if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", this.gameObject)) GameManager.Instance.AudioManager.Stop("SteamExhaustLoop", this.gameObject);
                    GameManager.Instance.AudioManager.Play("SteamExhaust");
                    isPowered = false;
                }
                pressure = 0;
            }

            if (pressure < dangerZoneThreshold)
            {
                if (explosionChance != explosionChanceOriginal) { explosionChance = explosionChanceOriginal; }
            }

        }

        public IEnumerator SpeedSurge(float duration, float force)
        {
            isSurging = true;

            /*force = force * Mathf.Sign(tank.treadSystem.gear);
            tank.treadSystem.ApplyForce(transform.position, force, duration);*/

            yield return new WaitForSeconds(duration);
            isSurging = false;
        }

        public override void Use(bool overrideConditions = false)
        {
            base.Use(overrideConditions);

            if (overrideConditions)
                AddPressure(30, false, false);
            else if (cooldown <= 0)
                StartCharge();
        }

        public override void LockIn(GameObject playerID)
        {
            base.LockIn(playerID);

            operatorID.GetCharacterHUD().SetButtonPrompt(GameAction.AddFuel, true);
            operatorID.GetCharacterHUD().SetButtonPrompt(GameAction.ReleaseSteam, true);
        }

        public override void CancelUse()
        {
            base.CancelUse();

            CheckCharge();
        }

        public override void Exit(bool sameZone)
        {
            RemoveTimingGauge();
            operatorID.GetCharacterHUD().SetButtonPrompt(GameAction.AddFuel, false);
            operatorID.GetCharacterHUD().SetButtonPrompt(GameAction.ReleaseSteam, false);

            if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", this.gameObject)) GameManager.Instance.AudioManager.Stop("SteamExhaustLoop", this.gameObject);
            base.Exit(sameZone);
        }

        public void StartCharge()
        {
            //If there is no operator, just add pressure
            if(operatorID == null)
            {
                AddPressure(25, false, false);
                return;
            }

            float random = Random.Range(minTargetCharge, maxTargetCharge);
            targetCharge = random;

            chargeTimer = 0;
            chargeStarted = true;

            float min = ((targetCharge - targetChargeOffset)) / maxChargeTime;
            float max = ((targetCharge + targetChargeOffset)) / maxChargeTime;

            currentGauge = GameManager.Instance.UIManager.AddTimingGauge(gameObject, new Vector2(0f, -0.56f), maxChargeTime, min, max, true);
            operatorID?.GetCharacterHUD()?.SetButtonPrompt(GameAction.ReleaseSteam, false);
        }

        public void CheckCharge()
        {
            if (!chargeStarted)
                return;

            if (chargeTimer >= minChargeTime)
            {
                //If the gauge was pressed in the zone
                if (currentGauge.PressGauge())
                {
                    AddPressure(30, true, true);
                    GameManager.Instance.AudioManager.Play("JetpackRefuel"); //Got it!
                }
                else
                {
                    //AddPressure(5, true, false);
                    GameManager.Instance.AudioManager.Play("InvalidAlert"); //Missed it!
                }
            }

            RemoveTimingGauge();

            chargeStarted = false;
            chargeTimer = 0;
        }

        private void RemoveTimingGauge()
        {
            if(currentGauge != null)
            {
                //Ends the timing gauge and destroys it
                currentGauge.EndTimingGauge();
                currentGauge = null;
                operatorID?.GetCharacterHUD()?.SetButtonPrompt(GameAction.ReleaseSteam, true);
            }
        }

        private void UpdatePowerOutput()
        {
            if (isPowered)
            {
                power = 0.5f;
                if (pressure >= 50) power = 1f;
                if (pressure >= dangerZoneThreshold) power = 1.5f;

                //if (isSurging) power *= boostMultiplier;

                //SFX
                if (!GameManager.Instance.AudioManager.IsPlaying("EngineRunning", this.gameObject)) GameManager.Instance.AudioManager.Play("EngineRunning", this.gameObject);
            }
            else
            {
                if (GameManager.Instance.AudioManager.IsPlaying("EngineRunning", this.gameObject)) GameManager.Instance.AudioManager.Stop("EngineRunning", this.gameObject);
                power = 0;
            }
        }

        private void CheckForFire()
        {
            if (pressure > dangerZoneThreshold)
            {
                if (!canExplode)
                {
                    explosionTimer = 0;
                    float randomOffset = Random.Range(-1f, 3f);
                    explosionTime += randomOffset;
                }
                canExplode = true;
            }

            if (pressure < dangerZoneThreshold)
            {
                canExplode = false;
                explosionTime = explosionTimeOriginal;
            }

            if (canExplode)
            {
                if (explosionTimer < explosionTime) explosionTimer += Time.deltaTime;
                if (explosionTimer > explosionTime)
                {
                    explosionTimer = 0;
                    if (parentCell.isOnFire != true) parentCell.Ignite();
                }
            }
        }

        public void Explode()
        {
            GameManager.Instance.ParticleSpawner.SpawnParticle(4, particleSpots[1].position, 0.1f, null);
            GameManager.Instance.AudioManager.Play("LargeExplosionSFX", gameObject);
            parentCell.Damage(100);
        }

        public void UpdateUI()
        {
            /*
            for (int i = 0; i < boilerSprites.Length; i++) {
                boilerSprites[i].color = Color.Lerp(temperatureLowColor, temperatureHighColor, pressure / 100f);
            }

            if (canExplode)
            {
                for (int i = 0; i < dangerSprites.Length; i++)
                {
                    Color tempColor = dangerSprites[i].color;
                    float alpha = Mathf.Lerp(1f, 0f, (targetTimer / targetValue));
                    tempColor.a = alpha;
                    dangerSprites[i].color = tempColor;

                    float scale = Mathf.Lerp(1.2f, 1f, (targetTimer / targetValue));
                    danger.localScale = new Vector3(scale, scale, 1f);
                }
                targetTimer += Time.deltaTime;
                if (targetTimer >= targetValue)
                {
                    targetTimer = 0;
                }

            }
            else
            {
                for (int i = 0; i < dangerSprites.Length; i++)
                {
                    Color tempColor = dangerSprites[i].color;
                    float alpha = 0;
                    tempColor.a = alpha;
                    dangerSprites[i].color = tempColor;
                }
                danger.localScale = new Vector3(1f, 1f, 1f);
            }
            */

            if (pressureBar != null)
            {
                pressureBar.localScale = new Vector3(0.1f, 0.5f * (pressure / 100f));
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            //if (isPowered) tank.treadSystem.horsePower -= power;
        }
    }
}
