using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class GunController : TankInteractable
    {
        //Objects & Components:
        [Tooltip("Default projectile which will be fired by this weapon")]                                      public GameObject projectilePrefab;
        [Tooltip("List containing special Ammo loaded into this weapon"), SerializeField]                       private List<GameObject> specialAmmo = new List<GameObject>();
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
        [Tooltip("Cooldown in seconds between when the weapon can fire"), SerializeField, Min(0)] private float rateOfFire;
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
        private bool isOverheating = false;
        private float smokePuffRate = 0.3f; //how much the gun should smoke when overheating (lower = more smoke)
        private float smokePuffTimer = 0;

        private SpriteRenderer heatRenderer;

        //Mortar
        private float maxVelocity;
        private float minVelocity = 20f;
        private float maxChargeTime = 2.4f; //maximum duration weapon can be charged before firing
        private float minChargeTime = 0.4f; //minimum duration the weapon needs to be charged before it can fire
        [HideInInspector]public float chargeTimer = 0;


        [Header("Debug Controls:")]
        public bool fire;
        [Range(0, 1)] public float moveGimbal = 0.5f;
        private Vector3 currentRotation = new Vector3(0, 0, 0);

        //Runtime Variables:
        private Vector2 barrelBasePos;
        private float reciproTimeLeft;

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

            ShowFirePrompt(true);
        }

        public override void Use(bool overrideConditions = false)
        {
            base.Use(overrideConditions);

            if (cooldown <= 0)
                Fire(overrideConditions, tank.tankType);
        }

        public override void Exit(bool sameZone)
        {
            ShowFirePrompt(false);
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

            //Cooldown
            if (fireCooldownTimer > 0)
            {
                fireCooldownTimer -= Time.deltaTime;
            }
            else if(isCooldownActive && !(gunType == GunType.MACHINEGUN))
            {
                isCooldownActive = false;
                ShowFirePrompt(true);
            }

            //Gun Specific
            if (gunType == GunType.CANNON) { };

            if (gunType == GunType.MACHINEGUN) {

                if (spinupTimer > 0) //Calculate barrel spin timings
                {
                    if ((operatorID != null && operatorID.interactInputHeld == false) || operatorID == null || isOverheating)
                    {
                        if (spinTimer > 0)
                        {
                            spinTimer -= Time.deltaTime;
                        }
                        else
                        {
                            spinupTimer -= Time.deltaTime;
                        }
                    }
                }

                if (overheatTimer > 0 && spinTimer <= 0) //Track overheat timer
                {
                    overheatTimer -= (Time.deltaTime * (1 / overheatCooldownMultiplier));
                }
                else
                {
                    if (overheatTimer <= 0)
                    {
                        isOverheating = false;
                        if (isCooldownActive)
                        {
                            isCooldownActive = false;
                            ShowFirePrompt(true);
                        }
                    }
                }

                if (isOverheating)
                {
                    smokePuffTimer += Time.deltaTime;
                    if (smokePuffTimer >= smokePuffRate)
                    {
                        smokePuffTimer = 0;
                        GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[1].position, 0.1f, null);
                    }
                }

                if (heatRenderer != null) //update heat sprite renderer
                {
                    float alpha = Mathf.Lerp(0, 150f, (overheatTimer / overheatTime) * Time.deltaTime);
                    Color newColor = heatRenderer.color;
                    newColor.a = alpha;
                    heatRenderer.color = newColor;
                }

            };

            if (gunType == GunType.MORTAR)
            {
                if (operatorID != null && operatorID.interactInputHeld && fireCooldownTimer <= 0)
                {
                    ChargeMortar();
                }
                else if (tank.tankType != TankId.TankType.ENEMY) 
                {
                    ChargeMortar(false);
                }
                
            }
        }

        /// <summary>
        /// Should be called in update, will charge or discharge the mortar based on the parameter.
        /// </summary>
        /// <param name="charging">
        /// "False" will cooldown it's charge, "True" will increment it.
        /// </param>
        public void ChargeMortar(bool charging = true)
        {
            if (charging)
            {
                if (chargeTimer < maxChargeTime)
                {
                    chargeTimer += Time.deltaTime;
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
                    chargeTimer -= Time.deltaTime;
                }
            }
            
            
            float newVelocity = Mathf.Lerp(minVelocity, maxVelocity, (chargeTimer / maxChargeTime));
            muzzleVelocity = newVelocity;
        }

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Fires the weapon, once.
        /// </summary>
        public void Fire(bool overrideConditions, TankId.TankType inheritance = TankId.TankType.PLAYER)
        {
            bool canFire = true;
            if (tank == null) tank = GetComponentInParent<TankController>();

            //Gun Specific
            if (gunType == GunType.CANNON) { };

            if (gunType == GunType.MACHINEGUN)
            {
                if (isOverheating == false)
                {
                    if (spinupTimer < spinupTime)
                    {
                        canFire = false;
                        if (isOverheating == false) spinupTimer += Time.deltaTime;
                    }
                    else
                    {
                        canFire = true;
                        spinTimer = spinTime;
                        overheatTimer += Time.deltaTime;
                    }

                    if (overrideConditions)
                    {
                        canFire = true;
                    }
                }

                if (overheatTimer > overheatTime)
                {
                    isOverheating = true;
                    if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaust", gameObject) == false) {
                        GameManager.Instance.AudioManager.Play("SteamExhaust", gameObject);
                    }

                    GameManager.Instance.UIManager.AddTaskBar(gameObject, new Vector2(-0.4f, -0.55f), overheatTime / (1 / overheatCooldownMultiplier), true);
                    ShowFirePrompt(false);
                    isCooldownActive = true;
                }

                if (isOverheating) canFire = false;
            };

            if (gunType == GunType.MORTAR)
            {
                canFire = false;

                if ((chargeTimer >= minChargeTime) || overrideConditions)
                {
                    canFire = true;
                    chargeTimer = 0;
                }
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

                //Fire projectile:
                Projectile newProjectile = Instantiate(projectile).GetComponent<Projectile>();
                if (gunType != GunType.MORTAR) newProjectile.Fire(barrel.position, barrel.right * muzzleVelocity);
                else newProjectile.Fire(barrel.position, barrel.up * muzzleVelocity);
                newProjectile.factionId = inheritance;

                //Handle knockback:
                ImpactProperties knockbackProperties = newProjectile.knockbackProperties;              //Get knockback properties from projectile
                if (knockbackProperties == null) knockbackProperties = newProjectile.impactProperties; //Use impact properties if none are given
                if (knockbackProperties != null) //Projectile has useable knockback properties
                {
                    parentCell.room.targetTank.treadSystem.HandleImpact(knockbackProperties, -barrel.right * muzzleVelocity, barrel.position); //Apply knockback at barrel position in reverse direction of projectile
                }

                //If Special, Remove from List
                if (projectile != projectilePrefab) { specialAmmo.RemoveAt(0); }

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
                    GameManager.Instance.UIManager.AddTaskBar(gameObject, new Vector2(-0.4f, -0.55f), rateOfFire, true);
                    ShowFirePrompt(false);
                    isCooldownActive = true;
                }

                if (gunType == GunType.MACHINEGUN)
                {
                    int random = Random.Range(9, 12);
                    float randomScale = Random.Range(0.25f, 0.35f);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(8, particleSpots[0].position, 0.85f, null); //Flash
                    GameObject part = GameManager.Instance.ParticleSpawner.SpawnParticle(random, particleSpots[0].position, randomScale, null); //Flare
                    part.transform.rotation = barrel.rotation;
                    GameManager.Instance.ParticleSpawner.SpawnParticle(12, particleSpots[2].position, 0.1f, null); //Bullet Casing
                    GameManager.Instance.AudioManager.Play("CannonFire", gameObject);
                }

                if (gunType == GunType.MORTAR)
                {
                    int random = Random.Range(0, 2);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(random, particleSpots[0].position, particleScale, null);
                    GameManager.Instance.AudioManager.Play("CannonThunk", gameObject);
                    GameManager.Instance.AudioManager.Play("ProjectileInAirSFX", gameObject);
                    GameManager.Instance.UIManager.AddTaskBar(gameObject, new Vector2(-0.4f, -0.55f), rateOfFire, true);
                    ShowFirePrompt(false);
                    isCooldownActive = true;
                }

                //Set Cooldown
                fireCooldownTimer = rateOfFire;
            }
        }

        private void ShowFirePrompt(bool showPrompt)
        {
            if(operatorID != null)
                operatorID.GetCharacterHUD().SetButtonPrompt(GameAction.Fire, showPrompt);
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

        public void AddSpecialAmmo(GameObject ammo, int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                specialAmmo.Add(ammo);
            }
            GameManager.Instance.AudioManager.Play("CannonReload", this.gameObject);
        }

        //DEBUG METHODS:
        public void ChangeRateOfFire(float multiplier)
        {
            float newROF = rateOfFire * multiplier;
            rateOfFire = newROF;
        }
    }
}