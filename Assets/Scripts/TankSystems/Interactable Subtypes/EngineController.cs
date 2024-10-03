using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineController : TankInteractable
{
    //Objects & Components
    [Tooltip("Transforms to spawn particles from when used."), SerializeField] private Transform[] particleSpots;

    //Settings:
    [Header("Engine Settings:")]
    public bool isPowered;
    
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

    [Header("Charge Settings:")]
    public float maxChargeTime;
    private float minChargeTime = 0f;
    public float chargeTimer = 0;
    private float targetChargeOffset = 0.25f;
    public float targetCharge;
    private float minTargetCharge = 1.5f;
    private float maxTargetCharge = 3f;
    private bool chargeStarted;

    TimingGauge currentGauge;

    [Header("UI:")]
    public SpriteRenderer[] boilerSprites;
    public Transform danger;
    public SpriteRenderer[] dangerSprites;
    private float targetValue = 1f;
    private float targetTimer = 0f;
    public Transform pressureBar;

    [Header("Explosion Settings:")]
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
        chargeStarted = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug settings:
        if (addPressure) { AddPressure(15, true, true); addPressure = false; }

        UpdateUI();
 
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

    private void FixedUpdate()
    {
        base.FixedUpdate();

        UpdatePressure();
        CheckForExplosion();

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
        if (enableSounds)
        {
            if (pressure >= 100)
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
        if (surgeSpeed)
        {
            StartCoroutine(tank.treadSystem.SpeedSurge(0.7f, 2));
        }
    }

    private void UpdatePressure()
    {
        float lowerSpeed = pressureReleaseSpeed * Time.deltaTime;
        float pressureDif = (50f + (pressure * 0.5f)) / 100f; //slows down the closer it gets to 0

        if (pressure > 0)
        {
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
            pressure = 0;
        }

    }

    public void StartCharge()
    {
        float random = Random.Range(minTargetCharge, maxTargetCharge);
        targetCharge = random;

        chargeTimer = 0;
        chargeStarted = true;

        float min = ((targetCharge - targetChargeOffset)) / maxChargeTime;
        float max = ((targetCharge + targetChargeOffset)) / maxChargeTime;

        currentGauge = GameManager.Instance.UIManager.AddTimingGauge(gameObject, maxChargeTime, min, max);
    }

    public void CheckCharge()
    {
        if (chargeTimer >= minChargeTime)
        {
            bool success = false;

            float min = targetCharge - targetChargeOffset;
            float max = targetCharge + targetChargeOffset;
            if (chargeTimer > min && chargeTimer < max) { success = true; }

            if (success) 
            { 
                AddPressure(30, true, true);
                GameManager.Instance.AudioManager.Play("JetpackRefuel"); //Got it!
            }
            else AddPressure(10, true, false);
        }

        if (currentGauge != null) Destroy(currentGauge.gameObject);
        chargeStarted = false;
        chargeTimer = 0;
    }

    private void CheckForExplosion()
    {
        if (pressure > dangerZoneThreshold)
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
