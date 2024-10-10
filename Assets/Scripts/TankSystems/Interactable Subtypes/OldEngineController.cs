using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class OldEngineController : TankInteractable
    {
        //Objects & Components
        [Tooltip("Transforms to spawn particles from when used."), SerializeField] private Transform[] particleSpots;

        //Settings:
        public enum EngineType { A, B }; //A = Temperature Mechanics, B = no Temperature Mechanic (purely visual)
        [Header("Engine Settings:")]
        public EngineType engineType;
        public bool isPowered;

        private float smokePuffRate = 1f;
        private float smokePuffTimer = 0;

        [Header("Coal Settings:")]
        public float coal = 0;
        [Tooltip("Maximum coal the firebox can hold"), SerializeField] public float maxCoal; //maximum coal allowed in Firebox
        public float coalBurnSpeed; //how fast coal burns
        public float coalBump; //bump to temp & pressure when adding coal
        private float currentCoalBurnValue = 0;

        [Header("Temperature:")]
        public float temperature = 0; //current temp
        public float targetTemperature = 0; //for Engine B - what temperature the current temp is lerping towards
        public float temperatureRiseSpeed; //how fast temperature rises due to coal
        public float lowTempThreshold; //threshold temp needs to be above for pressure to begin

        public Color temperatureLowColor;
        public Color temperatureHighColor;

        [Header("Pressure:")]
        public float pressure = 0;
        public float pressureRiseSpeed; //how fast pressure rises due to temperature
        public float pressureReleaseSpeed; //how fast pressure drops when holding release valve
        public float dangerZoneThreshold; //threshold pressure needs to be above for overdrive
        private bool overdriveActive = false;
        public float overDriveOffset = 1f; //multiplier on engine rates while overdrive is active
        private float pressureReleaseCd = 0;

        [Header("Explosion Settings:")]
        public float explosionTime; //how long it takes to trigger an explosion when conditions are met
        private bool canExplode = false;
        private float explosionTimeOriginal;
        public float explosionTimer = 0;

        [Header("UI:")]
        public SpriteRenderer[] boilerSprites;
        public Transform danger;
        public SpriteRenderer[] dangerSprites;
        private float targetValue = 1f;
        private float targetTimer = 0f;
        public Transform pressureBar;

        [Header("Debug Controls:")]
        public bool loadCoal;

        //Input
        public bool repairInputHeld;

        private void Start()
        {
            explosionTimeOriginal = explosionTime;
        }

        // Update is called once per frame
        void Update()
        {
            //Debug settings:
            if (loadCoal) { loadCoal = false; LoadCoal(1); }

            if (coal > 0) BurnCoal();
            UpdateTemperature();
            UpdatePressure();
            CheckForExplosion();
            UpdateUI();

            if (hasOperator == false) repairInputHeld = false;

            //Add to Tank Engine Count
            if (pressure > 0)
            {
                if (!isPowered)
                {
                    isPowered = true;
                    tank.treadSystem.currentEngines += 1;
                }
            }
            else
            {
                if (isPowered)
                {
                    isPowered = false;
                    tank.treadSystem.currentEngines -= 1;
                }
            }
        }

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Loads (amount) coal into the engine.
        /// </summary>
        public void LoadCoal(int amount, bool enableSounds = true, bool surgeSpeed = false)
        {
            //Increase coal total:
            coal += amount;
            if (enableSounds)
            {
                if (coal > maxCoal)
                {
                    GameManager.Instance.AudioManager.Play("InvalidAlert"); //Can't do that, sir
                }
                else
                {
                    //Other effects:
                    GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[0].position, 0.15f, null);
                    GameManager.Instance.AudioManager.Play("CoalLoad", this.gameObject); //Play loading clip
                    GameManager.Instance.SystemEffects.ApplyRampedControllerHaptics(operatorID.GetPlayerData().playerInput, 0f, 0.5f, 0.25f, 0.5f, 0.25f); //Apply haptics
                }
            }

            //Small Speed Boost
            if ((coal < maxCoal) && surgeSpeed)
            {
                StartCoroutine(tank.treadSystem.SpeedSurge(0.7f, 2));
            }
        }

        private void BurnCoal() //Depletes coal over time based on coalBurnSpeed
        {
            currentCoalBurnValue += 1f * coalBurnSpeed * Time.deltaTime;
            if (currentCoalBurnValue >= 10f)
            {
                coal -= 1;
                currentCoalBurnValue = 0;
            }
            if (engineType == EngineType.B)
            {
                targetTemperature = coal * (100f / maxCoal);
                if (overdriveActive) coalBurnSpeed = 0.5f + (temperature * 0.005f) + (overDriveOffset * 10f);
                else coalBurnSpeed = 0.5f + (temperature * 0.005f);
            }

            smokePuffTimer += coalBurnSpeed * Time.deltaTime;
            if (smokePuffTimer >= smokePuffRate)
            {
                smokePuffTimer = 0;
                GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[0].position, 0.1f, null);
            }
        }

        private void UpdateTemperature() //Increases Temperature over time while coal burning
        {
            float riseSpeed = 1f * temperatureRiseSpeed * Time.deltaTime; //how fast temperature fills over time
            float lowerSpeed = -pressureReleaseSpeed * Time.deltaTime * 0.4f; //how fast temperature drains when holding release
            float heatDif = (100f - (temperature * 0.75f)) / 100f; //slows it down the closer it gets to 100

            if (engineType == EngineType.A)
            {
                if (coal > 0)
                {
                    riseSpeed = riseSpeed * heatDif;
                    temperature += riseSpeed;
                    if (temperature > 100f) temperature = 100f;
                }
                else if (temperature > 0)
                {
                    temperature -= riseSpeed * 5f;
                    if (temperature < 0) temperature = 0;
                }

                if (repairInputHeld && temperature > 0)
                {
                    temperature += lowerSpeed;
                    if (temperature < 0) temperature = 0;
                }
            }

            if (engineType == EngineType.B)
            {
                if (coal > 0)
                {
                    temperature = Mathf.Lerp(temperature, targetTemperature, riseSpeed * heatDif);
                }
                else if (temperature > 0)
                {
                    temperature -= riseSpeed * 5f;
                    if (temperature < 0) temperature = 0;
                }
            }
        }

        private void UpdatePressure()
        {
            float riseSpeed = 1f * pressureRiseSpeed * Time.deltaTime;
            float lowerSpeed = -pressureReleaseSpeed * Time.deltaTime;
            float pressureDif = (100f - (pressure * 0.25f)) / 100f; //slows down the closer it gets to 100

            if (engineType == EngineType.A)
            {
                if (temperature > lowTempThreshold)
                {
                    riseSpeed = riseSpeed * (temperature * 15f) * pressureDif;
                    pressure += riseSpeed;
                    if (pressure > 100f) pressure = 100f;
                }
                else if (pressure > 0)
                {
                    pressure += lowerSpeed;
                    if (pressure < 0) pressure = 0;
                }

                if (repairInputHeld && pressure > 2f)
                {
                    pressure += lowerSpeed;
                    if (pressure < 0) pressure = 0;

                    if (pressure >= dangerZoneThreshold)
                    {
                        overdriveActive = true;
                    }

                    if (!GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop"))
                    {
                        GameManager.Instance.AudioManager.Play("SteamExhaustLoop");
                    }
                }
                else if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop"))
                {
                    GameManager.Instance.AudioManager.Stop("SteamExhaustLoop");
                    overdriveActive = false;
                }
            }

            if (engineType == EngineType.B)
            {
                if (temperature > 0)
                {
                    //if (temperature < pressure) riseSpeed *= 0.5f;
                    pressure = Mathf.Lerp(pressure, temperature, riseSpeed * pressureDif); //Lerps pressure towards current temperature, slowed slightly by pressure dif
                    if (pressure > 100f) pressure = 100f;
                }
                else if (pressure > 0)
                {
                    pressure += lowerSpeed;
                    if (pressure < 0) pressure = 0;
                }

                if (repairInputHeld && pressure > 0f && pressureReleaseCd <= 0)
                {
                    if (overdriveActive) pressure += lowerSpeed * overDriveOffset;
                    else pressure += lowerSpeed;

                    if (pressure < 0) pressure = 0;

                    if (pressure >= dangerZoneThreshold)
                    {
                        overdriveActive = true;
                    }

                    if (!GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", gameObject))
                    {
                        GameManager.Instance.AudioManager.Play("SteamExhaustLoop", gameObject);
                    }
                }
                else
                {
                    if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", gameObject))
                    {
                        GameManager.Instance.AudioManager.Stop("SteamExhaustLoop", gameObject);
                    }
                    overdriveActive = false;
                }

                if (pressure <= 0)
                {
                    if (overdriveActive)
                    {
                        pressureReleaseCd = 2.0f;
                        GameManager.Instance.AudioManager.Play("SteamExhaust", gameObject);
                    }
                    overdriveActive = false;
                }
            }

            if (pressureReleaseCd > 0) pressureReleaseCd -= Time.deltaTime;

        }

        private void CheckForExplosion()
        {
            if (temperature > dangerZoneThreshold && pressure > dangerZoneThreshold)
            {
                if (!canExplode)
                {
                    explosionTimer = 0;
                    float randomOffset = Random.Range(-1f, 5f);
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
                    Explode();
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
            for (int i = 0; i < boilerSprites.Length; i++)
            {
                boilerSprites[i].color = Color.Lerp(temperatureLowColor, temperatureHighColor, temperature / 100f);
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

            if (pressureBar != null)
            {
                pressureBar.localScale = new Vector3(0.1f, 0.5f * (pressure / 100f));
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (isPowered) tank.treadSystem.currentEngines -= 1;
        }
    }
}
