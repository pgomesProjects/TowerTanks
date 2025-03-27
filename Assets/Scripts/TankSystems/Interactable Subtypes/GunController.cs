using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class GunController : TankInteractable
    {
        //Objects & Components:
        [Header("Weapon Components:")]
        [Tooltip("Default projectile which will be fired by this weapon")]                                      public GameObject projectilePrefab;
        [Tooltip("List containing special Ammo loaded into this weapon"), SerializeField]                       public List<GameObject> specialAmmo = new List<GameObject>();
        [Tooltip("Transform indicating direction and position in which projectiles are fired"), SerializeField] internal Transform barrel;
        [Tooltip("Joint around which moving cannon assembly rotates."), SerializeField]                         private Transform pivot;
        [Tooltip("Transforms to spawn particles from when used."), SerializeField]                              private Transform[] particleSpots;
        [Tooltip("Scale particles are multiplied by when used by this weapon"), SerializeField]                 private float particleScale;
        [Tooltip("Line Renderer used for trajectories"), SerializeField]                                        private LineRenderer trajectoryLine;
        [Tooltip("Moving barrel assembly."), SerializeField]                                                    private Transform reciprocatingBarrel;

        //Settings:
        public enum GunType { CANNON, MACHINEGUN, MORTAR };

        [Header("Gun Settings:")]
        [Tooltip("What type of weapon this is"), SerializeField] public GunType gunType;
        [Tooltip("Velocity of projectile upon exiting the barrel."), SerializeField, Min(0)]  internal float muzzleVelocity;
        [Tooltip("Force exerted on tank each time weapon is fired."), SerializeField, Min(0)] private float recoil;
        [Tooltip("Speed at which the cannon barrel rotates"), SerializeField]                 private float rotateSpeed;
        [Tooltip("Max angle (up or down) weapon joint can be rotated to."), SerializeField]   private float gimbalRange;
        [Tooltip("Cooldown in seconds between when the weapon can fire"),  Min(0)]            public float rateOfFire;
        [Header("Barrel Reciprocation:")]
        [SerializeField, Tooltip("How far the barrel reciprocates when firing."), Min(0)]          private float reciprocationDistance;
        [SerializeField, Tooltip("How long barrel reciprocation phase is."), Min(0)]               private float reciprocationTime;
        [SerializeField, Tooltip("Curve describing motion of barrel during reciprocation phase.")] private AnimationCurve reciprocationCurve;
        [Space(), HideInInspector]
        public float fireCooldownTimer;
        private bool isCooldownActive;
        [Tooltip("Radius of Degrees of the Cone of Fire for this weapon's projectiles"), SerializeField, Min(0)] private float spread;

        //Gun Specific Settings

        //Cannon

        //Machine Gun
        private float spinupTime = 0.1f; //while fire is held down, how much time it takes before it starts shooting
        private float spinupTimer = 0;
        private float spinTime = 0.4f; //how long the barrel will keep spinning for after shooting before it starts to slow down again
        private float spinTimer = 0;

        private float overheatTime = 6.5f; //how long the operator can keep shooting for before the weapon overheats
        private float overheatCooldownMultiplier = 0.5f; //what percentage of time does it take for the weapon to cooldown
        private float overheatTimer = 0f;
        public bool isOverheating { get; private set; }
        private float smokePuffRate = 0.3f; //how much the gun should smoke when overheating (lower = more smoke)
        private float smokePuffTimer = 0;

        private SpriteRenderer heatRenderer;
        private SymbolDisplay currentSpecialAmmo;

        //Mortar
        private float maxVelocity;
        //private float minVelocity = 20f;
        //private float maxChargeTime = 2.4f; //maximum duration weapon can be charged before firing
        //private float minChargeTime = 0.4f; //minimum duration the weapon needs to be charged before it can fire
        //[HideInInspector]public float chargeTimer = 0;

        [Header("Debug Controls:")]
        public bool fire;
        [Range(0, 1)] public float moveGimbal = 0.5f;
        private Vector3 currentRotation = new Vector3(0, 0, 0);


        [Button(ButtonSizes.Medium)]
        private void TestAddSpecialAmmo()
        {
            GameObject ammo = null;
            int amount = 3;
            
            switch (gunType) //Determine Ammo Type & Quantity
            {
                case GunType.MACHINEGUN:
                    ammo = GameManager.Instance.CargoManager.projectileList[0].ammoTypes[1];
                    amount *= 20;
                    break;

                case GunType.CANNON:
                    ammo = GameManager.Instance.CargoManager.projectileList[1].ammoTypes[1];
                    break;

                case GunType.MORTAR:
                    ammo = GameManager.Instance.CargoManager.projectileList[1].ammoTypes[3];
                    break;
            }

            if (ammo != null) AddSpecialAmmo(ammo, amount);
        }

        //Runtime Variables:
        private Vector2 barrelBasePos;
        private float reciproTimeLeft;
        [HideInInspector] public bool usingAIbrain = false;

        //RUNTIME METHODS:

        private void Start()
        {
            if (gunType == GunType.MACHINEGUN) { heatRenderer = transform.Find("Visuals/JointParent/MachineGun_Heat").GetComponent<SpriteRenderer>(); }
            if (gunType == GunType.MORTAR) {
                trajectoryLine = GetComponentInChildren<LineRenderer>();
                trajectoryLine.positionCount = 100;
                
                trajectoryLine.enabled = false;
                maxVelocity = muzzleVelocity; 
            }

            //Initialize runtime variables:
            if (reciprocatingBarrel != null) barrelBasePos = reciprocatingBarrel.localPosition;
            isCooldownActive = false;
        }

        public override void LockIn(GameObject playerID)
        {
            base.LockIn(playerID);
            operatorID.GetCharacterHUD()?.InventoryHUD.InitializeBar(1f);
        }

        public override void Use(bool overrideConditions = false)
        {
            base.Use(overrideConditions);

            if (isBroken) return;
            if (cooldown <= 0)
                Fire(overrideConditions, tank.tankType);
        }

        public override void Exit(bool sameZone)
        {
            if (GameManager.Instance.AudioManager.IsPlaying("CannonRotate", gameObject)) GameManager.Instance.AudioManager.Stop("CannonRotate", gameObject);
            base.Exit(sameZone);
        }

        private void Update()
        {
            //Barrel reciprocation:
            if (reciprocatingBarrel != null && reciproTimeLeft > 0)
            {
                reciproTimeLeft = Mathf.Max(0, reciproTimeLeft - Time.deltaTime);                                  //Decrement time tracker
                float interpolant = 1 - (reciprocationTime == 0 ? 0 : reciproTimeLeft / reciprocationTime);        //Use ternary to prevent possible division by zero
                Vector2 targetPos = barrelBasePos + (Vector2.left * reciprocationDistance);                        //Get farthest position barrel will reciprocate to
                Vector2 newPos = Vector2.Lerp(barrelBasePos, targetPos, reciprocationCurve.Evaluate(interpolant)); //Get new position by interpolating it between base and target positions and adjusting using animation curve
                reciprocatingBarrel.localPosition = newPos;                                                        //Apply new position
            }

            //Debug settings:
            if (fire) { fire = false; Fire(true, tank.tankType); }

            pivot.localEulerAngles = currentRotation;

            if (gunType == GunType.MORTAR)
            {
                if (operatorID != null && fireCooldownTimer <= 0 && tank.tankType == TankId.TankType.PLAYER && !isBroken)
                {
                    ToggleTrajectory(true);
                }
                else
                {
                    ToggleTrajectory(false);
                }
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            //Cooldown
            if (fireCooldownTimer > 0)
            {
                fireCooldownTimer -= Time.fixedDeltaTime;

                //If there is an operator and the gun isn't a machine gun, update the item bar
                if(operatorID != null && gunType != GunType.MACHINEGUN)
                    operatorID.GetCharacterHUD()?.InventoryHUD.UpdateItemBar(1f - (fireCooldownTimer / rateOfFire));
            }
            else if (isCooldownActive && !(gunType == GunType.MACHINEGUN))
            {
                isCooldownActive = false;
            }

            //Gun Specific
            if (gunType == GunType.CANNON) { };

            if (gunType == GunType.MACHINEGUN)
            {

                if (spinupTimer > 0) //Calculate barrel spin timings
                {
                    if ((operatorID != null && operatorID.interactInputHeld == false) || operatorID == null || isOverheating)
                    {
                        if (spinTimer > 0)
                        {
                            spinTimer -= Time.fixedDeltaTime;
                        }
                        else
                        {
                            spinupTimer -= Time.fixedDeltaTime;
                        }
                    }
                }
                if (!usingAIbrain) // if the operator is not an AI
                {
                    if (overheatTimer > 0 && spinTimer <= 0) //Track overheat timer
                    {
                        overheatTimer -= (Time.fixedDeltaTime * (1 / overheatCooldownMultiplier));

                        if (operatorID != null && isOverheating)
                            operatorID.GetCharacterHUD()?.InventoryHUD.UpdateItemBar(1f - (overheatTimer / overheatTime));
                    }
                    else
                    {
                        if (overheatTimer <= 0)
                        {
                            isOverheating = false;
                            if (isCooldownActive)
                            {
                                isCooldownActive = false;
                            }
                        }
                    }
                }
                else // if the operator is the ai, the overheat timer is handled slightly differently
                {
                    if (overheatTimer > 0 && isOverheating)
                    {
                        overheatTimer -= (Time.fixedDeltaTime * (1 / overheatCooldownMultiplier));
                    }
                    else
                    {
                        if (overheatTimer <= 0)
                        {
                            isOverheating = false;
                            if (isCooldownActive)
                            {
                                isCooldownActive = false;
                            }
                        }
                    }
                }


                if (isOverheating)
                {
                    smokePuffTimer += Time.fixedDeltaTime;
                    if (smokePuffTimer >= smokePuffRate)
                    {
                        smokePuffTimer = 0;
                        GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[1].position, 0.1f, this.transform);
                    }
                }

                if (heatRenderer != null) //update heat sprite renderer
                {
                    float alpha = Mathf.Lerp(0, 150f, (overheatTimer / overheatTime) * Time.fixedDeltaTime);
                    Color newColor = heatRenderer.color;
                    newColor.a = alpha;
                    heatRenderer.color = newColor;
                }

            };
        }

        /*
        /// <summary>
        /// Should be called in update, will charge or discharge the mortar based on the parameter.
        /// </summary>
        /// <param name="charging">
        /// "False" will cooldown it's charge, "True" will increment it.
        /// </param>
        public void ChargeMortar(bool charging = true, float chargeMultiplier = 1)
        {
            if (charging)
            {
                if (chargeTimer < maxChargeTime)
                {
                    chargeTimer += Time.deltaTime * chargeMultiplier;
                }

                if (chargeTimer >= minChargeTime)
                {
                    List<Vector3> trajectoryPoints = Trajectory.GetTrajectory(barrel.position, barrel.up * muzzleVelocity, 30, 100);

                    if (operatorID)
                    {
                        Color playerColor = operatorID.GetCharacterColor();
                        trajectoryLine.startColor = playerColor;
                        trajectoryLine.endColor = playerColor;

                        trajectoryLine.enabled = true;
                        for (int i = 0; i < trajectoryPoints.Count; i++)
                        {
                            trajectoryLine.SetPosition(i, trajectoryPoints[i]);
                        }
                    }
                }
            } else
            {
                trajectoryLine.enabled = false;
                if (chargeTimer > 0)
                {
                    chargeTimer -= Time.deltaTime * chargeMultiplier;
                }
            }
            
            
            float newVelocity = Mathf.Lerp(minVelocity, maxVelocity, (chargeTimer / maxChargeTime));
            muzzleVelocity = newVelocity;
        }
        */

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Fires the weapon, once.
        /// </summary>
        public void Fire(bool overrideConditions, TankId.TankType inheritance = TankId.TankType.PLAYER, bool bypassSpinup = false)
        {
            if (isBroken) return;
            bool canFire = true;
            if (tank == null) tank = GetComponentInParent<TankController>();

            //Gun Specific
            if (gunType == GunType.CANNON) { };

            if (gunType == GunType.MACHINEGUN)
            {
                if (isOverheating == false)
                {
                    if (spinupTimer < spinupTime && !bypassSpinup)
                    {
                        canFire = false;
                        spinupTimer += Time.deltaTime;
                    }
                    else
                    {
                        if (!usingAIbrain) spinTimer = spinTime;
                        overheatTimer += Time.deltaTime;
                    }

                    if (overrideConditions)
                    {
                        canFire = true;
                    }
                }

                if (overheatTimer > overheatTime && !isOverheating)
                {
                    overheatTimer = overheatTime;
                    isOverheating = true;
                    if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaust", gameObject) == false) {
                        GameManager.Instance.AudioManager.Play("SteamExhaust", gameObject);
                    }

                    GameManager.Instance.UIManager.AddTaskBar(gameObject, new Vector2(0f, -45f), overheatTime / (1 / overheatCooldownMultiplier), true);
                    isCooldownActive = true;
                }

                if (isOverheating) canFire = false;
            };

            if (gunType == GunType.MORTAR)
            {
                /*
                canFire = false;

                if ((chargeTimer >= minChargeTime) || overrideConditions)
                {
                    canFire = true;
                    if (!usingAIbrain) chargeTimer = 0;
                }*/
            };

            if ((fireCooldownTimer <= 0 || overrideConditions) && canFire)
            {
                Vector3 tempRotation = barrel.localEulerAngles;

                //Adjust for Spread
                float randomSpread = Random.Range(-spread, spread);
                barrel.localEulerAngles += new Vector3(0, 0, randomSpread);

                //Check for Special Ammo
                GameObject projectile = projectilePrefab; //Default projectile
                if (specialAmmo.Count > 0) { projectile = specialAmmo[0]; } //Special Ammo

                //Adjust velocity:
                Vector2 fireVelocity = barrel.up * muzzleVelocity;
                //fireVelocity += tank.treadSystem.r.GetPointVelocity(barrel.position); This line of code is what adds the tank's velocity to the projectile. Commented out for now, will probably be useful later.

                //Fire projectile:
                Projectile newProjectile = Instantiate(projectile).GetComponent<Projectile>();
                if (gunType != GunType.MORTAR) newProjectile.Fire(barrel.position, barrel.right * muzzleVelocity);
                else newProjectile.Fire(barrel.position, fireVelocity);
                newProjectile.factionId = inheritance;

                if (!tank.overrideWeaponRecoil)
                {
                    //Handle knockback:
                    Vector2 knockbackForce = newProjectile.hitProperties.mass * muzzleVelocity * -barrel.right; //Calculate knockback force based on mass and muzzle velocity of projectile
                                                                                                                //if (parentCell.room.targetTank.treadSystem.ramming) knockbackForce *= 0.5f;
                    parentCell.room.targetTank.treadSystem.HandleImpact(knockbackForce, barrel.position);       //Apply knockback to own treadsystem at barrel position in reverse direction of projectile
                }

                //If Special, Remove from List
                if (projectile != projectilePrefab)
                {
                    specialAmmo.RemoveAt(0);
                    currentSpecialAmmo?.UpdateDisplay(specialAmmo.Count.ToString());

                    //If there is no more special ammo, destroy the display
                    if (specialAmmo.Count <= 0)
                        currentSpecialAmmo?.DestroyDisplay();
                }

                /*
                if (newProjectile.factionId == TankId.TankType.ENEMY) {
                    newProjectile.gameObject.layer = 23;
                    newProjectile.layerMask |= (LayerMask.NameToLayer("Projectiles"));
                    newProjectile.layerMask &= (LayerMask.NameToLayer("EnemyProjectiles"));
                }*/

                //Revert from Spread
                barrel.localEulerAngles = tempRotation;

                //Other effects:
                reciproTimeLeft = reciprocationTime;
                if (gunType == GunType.CANNON)
                {
                    int random = Random.Range(0, 2);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(random, particleSpots[0].position, particleScale, null);
                    GameManager.Instance.AudioManager.Play("CannonFire", gameObject);
                    GameManager.Instance.AudioManager.Play("CannonThunk", gameObject); //Play firing audioclips
                    CameraManipulator.main?.ShakeTankCamera(tank, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Jolt"));
                    GameManager.Instance.UIManager.AddTaskBar(gameObject, new Vector2(-15f, -50f), rateOfFire, true);
                    isCooldownActive = true;

                    //Haptics
                    if (operatorID != null)
                    {
                        HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("QuickRumble");
                        GameManager.Instance.SystemEffects.ApplyControllerHaptics(operatorID.GetPlayerData().playerInput, setting); //Apply haptics
                    }
                }

                if (gunType == GunType.MACHINEGUN)
                {
                    int random = Random.Range(9, 12);
                    float randomScale = Random.Range(0.25f, 0.35f);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(8, particleSpots[0].position, 0.85f, null); //Flash
                    GameObject part = GameManager.Instance.ParticleSpawner.SpawnParticle(random, particleSpots[0].position, randomScale, null); //Flare
                    part.transform.rotation = barrel.rotation;
                    GameManager.Instance.ParticleSpawner.SpawnParticle(12, particleSpots[2].position, 0.1f, null); //Bullet Casing
                    
                    //Gunshots
                    if (GameManager.Instance.AudioManager.IsPlaying("MachineGunFire", gameObject))
                    {
                        GameManager.Instance.AudioManager.Stop("MachineGunFire", gameObject);
                    }
                    GameManager.Instance.AudioManager.PlayRandomPitch("MachineGunFire", 0.9f, 1.1f, gameObject);

                    //Haptics
                    if (operatorID != null)
                    {
                        HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("QuickJolt");
                        GameManager.Instance.SystemEffects.ApplyControllerHaptics(operatorID.GetPlayerData().playerInput, setting); //Apply haptics
                    }
                }

                if (gunType == GunType.MORTAR)
                {
                    int random = Random.Range(0, 2);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(random, particleSpots[0].position, particleScale, null);
                    GameManager.Instance.AudioManager.Play("CannonThunk", gameObject);
                    GameManager.Instance.AudioManager.Play("ProjectileInAirSFX", gameObject);
                    CameraManipulator.main?.ShakeTankCamera(tank, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Jolt"));
                    GameManager.Instance.UIManager.AddTaskBar(gameObject, new Vector2(-40f, -45f), rateOfFire, true);
                    isCooldownActive = true;

                    //Haptics
                    if (operatorID != null)
                    {
                        HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("LowRumble");
                        GameManager.Instance.SystemEffects.ApplyControllerHaptics(operatorID.GetPlayerData().playerInput, setting); //Apply haptics
                    }
                }

                //Set Cooldown
                fireCooldownTimer = rateOfFire;
            }
        }

        public void RotateBarrel(float force, bool withSound)
        {
            float speed = rotateSpeed * Time.deltaTime;

            currentRotation += new Vector3(0, 0, force * speed * direction);

            if (currentRotation.z > gimbalRange) currentRotation = new Vector3(0, 0, gimbalRange);
            if (currentRotation.z < -gimbalRange) currentRotation = new Vector3(0, 0, -gimbalRange);

            //Play SFX
            if (withSound)
            {
                if (force != 0)
                {
                    if (!GameManager.Instance.AudioManager.IsPlaying("CannonRotate", gameObject)) GameManager.Instance.AudioManager.Play("CannonRotate", gameObject);
                }
                else if (GameManager.Instance.AudioManager.IsPlaying("CannonRotate", gameObject)) GameManager.Instance.AudioManager.Stop("CannonRotate", gameObject);
            }
        }

        public void AddSpecialAmmo(GameObject ammo, int quantity, bool enableSounds = true)
        {
            for (int i = 0; i < quantity; i++)
            {
                specialAmmo.Add(ammo);
            }

            //If there is special ammo already, add to the display
            if (currentSpecialAmmo != null)
                currentSpecialAmmo.UpdateDisplay(specialAmmo.Count.ToString());
            //If not, create the display
            else
            {
                Sprite sprite = GameManager.Instance.CargoManager.ammoSymbols[0];
                Color color = Color.white;

                //Change the display position based on the type of gun
                Vector2 displayPos = Vector2.zero;
                switch (gunType)
                {
                    case GunType.CANNON:
                        displayPos = new Vector2(0f, 70f); 
                        break;
                    case GunType.MACHINEGUN:
                        displayPos = new Vector2(0f, 70f);
                        break;
                    case GunType.MORTAR:
                        displayPos = new Vector2(0f, -60f);
                        break;
                }

                currentSpecialAmmo = GameManager.Instance.UIManager.AddSymbolDisplay(gameObject, displayPos, sprite, quantity.ToString(), color);
            }
            if (enableSounds) GameManager.Instance.AudioManager.Play("CannonReload", this.gameObject);
        }

        public void ToggleTrajectory(bool toggle)
        {
            List<Vector3> trajectoryPoints = Trajectory.GetTrajectory(barrel.position, barrel.up * muzzleVelocity, 30, 100);

            if (operatorID)
            {
                Color playerColor = operatorID.GetCharacterColor();
                trajectoryLine.startColor = playerColor;
                trajectoryLine.endColor = playerColor;

                trajectoryLine.enabled = true;
                for (int i = 0; i < trajectoryPoints.Count; i++)
                {
                    trajectoryLine.SetPosition(i, trajectoryPoints[i]);
                }
            }

            trajectoryLine.enabled = toggle;
        }

        //DEBUG METHODS:
        public void ChangeRateOfFire(float multiplier)
        {
            float newROF = rateOfFire * multiplier;
            rateOfFire = newROF;
        }
    }
}