using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

namespace TowerTanks.Scripts
{
    public class TreadSystem : MonoBehaviour, IDamageable
    {
        //Objects & Components:
        [Tooltip("Controller for this tread's parent tank.")]                                                         private TankController tankController;
        [Tooltip("Rigidbody for affecting tank movement.")]                                                           internal Rigidbody2D r;
        [Tooltip("Wheels controlled by this system.")]                                                                internal TreadWheel[] wheels;
        [SerializeField, Tooltip("Prefab which will be used to generate caterpillar treads (should be 1 unit long)")] private GameObject treadPrefab;
        [Tooltip("Array of all tread segments in system (one per wheel).")]                                           private Transform[] treads;
        [Tooltip("Object containing colliders which make up collide-able mass of tank.")]                             internal Transform colliderSystem;
        [Tooltip("Animator component used for tread animations.")]                                                    public Animator animator;

        //Settings:
        [Header("Center of Gravity Settings:")]
        [Tooltip("Height at which center of gravity is locked relative to tread system.")]                                                                                  public float COGHeight;
        [Tooltip("Extents of center of gravity (affects how far tank can lean).")]                                                                                          public float COGWidth;
        [SerializeField, Tooltip("Amount by which to multiply final tank mass."), Min(0.001f)]                                                                              private float massMultiplier = 1;
        [SerializeField, Tooltip("How much each individual cell in the tank weighs."), Min(0.001f)]                                                                         private float cellWeight = 1;
        [Tooltip("Maximum height above centerpoint of treadsystem at which impact points will be processed, prevents tank from being jerked around by high hits."), Min(0)] public float maximumRelativeImpactHeight;
        [Header("Engine Properties:")]
        [Min(1), Tooltip("Number of positions throttle can be in (includes neutral and reverse)")]      public int gearPositions = 5;
        [SerializeField, Tooltip("Constant maximum torque tank's engine can produce with no boilers.")] private float baseEnginePower;
        [SerializeField, Tooltip("Amount of engine torque added by a fully-stoked boiler.")]            private float boilerPowerAdd;
        [SerializeField, Tooltip("Additional torque added by a boiler when it is surging.")]            private float boilerSurgePower;
        [Space()]
        [SerializeField, Tooltip("Idling RPM of tank engine.")]                                                                                               private float minRPM;
        [SerializeField, Tooltip("Maximum speed engine can turn in rotations per minute.")]                                                                   private float maxRPM;
        [SerializeField, Tooltip("Rate at which RPM can be changed by throttle."), Range(0, 1)]                                                               private float RPMAccelFactor;
        [SerializeField, Tooltip("Determines the torque output of the engine (as a percentage of enginePower) based on the RPM (as a percentage of maxRPM)")] private AnimationCurve engineTorqueCurve;
        [Header("Tread Properties:")]
        [Tooltip("Determines the amount of traction induced by the tread (affected on a wheel-by-wheel basis depending on mass and suspension compression)."), Min(0)] public float frictionCoefficient = 1;
        [SerializeField, Tooltip("Curve describing falloff of wheel grip efficacy depending on slip ratio (T = 1 corresponds to when slipRatio = maxSlipRatio).")]     private AnimationCurve slipRatioCurve;
        [SerializeField, Tooltip("Hard clamp on magnitude of slip ratio, used to prevent tanks from launching themselves into orbit."), Min(0)]                        private float maxSlipRatio;
        [Tooltip("Basically how massive the moving components of the tread system are. Higher inertia means the wheels change speed slower."), Min(0)]                 public float treadInertia;
        [Tooltip("Drag which resists rotation of system axles (caps maximum speed wheels can turn at)."), Min(0)]                                                      public float axleDragCoefficient;
        [Space()]
        [SerializeField, Min(0), Tooltip("Force which tries to keep wheels stuck to the ground.")]    private float wheelStickiness;
        [SerializeField, Tooltip("Curve describing how sticky wheel is based on compression value.")] private AnimationCurve wheelStickCompressionCurve;
        [Header("Brake Properties:")]
        [SerializeField, Tooltip("Amount of drag torque each brake applies to a wheel.")]                         private float brakeDragCoefficient;
        [SerializeField, Tooltip("Amount of time system has to spend in 0 gear before applying brakes."), Min(0)] private float brakeDwellTime;
        [SerializeField, Tooltip("Time it takes for brakes to reach full efficacy."), Min(0)]                     private float brakeSaturationTime;
        [SerializeField, Tooltip("Curve describing efficacy of breaks as they reach saturation time.")]           private AnimationCurve brakeSaturationCurve;

        [Header("Traction & Drag Settings:")]
        [SerializeField, Tooltip("Default drag factor applied by air causing tank to lean while in motion (scales based on speed)."), Min(0)] private float baseAirDragForce;
        [SerializeField, Tooltip("Angular drag when all (non-extra) wheels are on the ground."), Min(0)]                                      private float maxAngularDrag;
        [Space()]
        [Range(0, 90), SerializeField, Tooltip("Maximum angle (left or right) at which tank can be tipped.")]                                             private float maxTipAngle;
        [Min(0), SerializeField, Tooltip("Radial area (inside max tip angle) where torque will be applied to prevent tank from reaching max tip angle.")] private float tipAngleBufferZone;
        [Min(0), SerializeField, Tooltip("Scale of force applied to prevent tippage.")]                                                                   private float tipPreventionForce;

        [Header("Ramming & Collision Settings:")]
        [SerializeField, Tooltip("Multiplier applied to forces generated by collisions with other tanks."), Min(0)] private float impactForceMultiplier = 1;
        [SerializeField, Tooltip("Minimum Speed for Ramming Effects to Apply")]                                     public float rammingSpeed;
        [SerializeField, Tooltip("While true, tank will ignore most knockback effects applied to it.")]             public bool ramming;
        [Tooltip("Angular velocity of the tank's rigidbody on the last frame.")] private float lastAngVelocity = 0;
        [Tooltip("Velocity of the tank's rigidbody on the last frame.")] private Vector2 lastVelocity;

        [Header("Jamming Settings:")]
        [SerializeField, Tooltip("Amount of damage treads can take.")]                                            private float treadMaxHealth = 200f;
        [SerializeField, Tooltip("Tread health regeneration factor (in points per second).")]                     private float healthRegenRate;
        [SerializeField, Tooltip("Health value below which treads will jam, and above which treads will unjam.")] private float unjamHealthThreshold = 60f;
                                                                                                                  public Transform jammedSprite;
        [Header("Animation:")]
        [SerializeField, Tooltip("Gear in the center of the treadbase.")]   private Transform centerGear;
        [SerializeField, Tooltip("Modifies speed of center gear."), Min(0)] private float centerGearSpeedScale;

        [Header("Debug Settings:")]
        [SerializeField, Tooltip("Enables the MANY gizmos generated by treadsystem for debugging purposes.")] private bool gizmosOn = true;
        [Button("JamTreads", Icon = SdfIconType.Wrench)]
        private void JamTreads() { Damage(treadHealth); }

        //Runtime Variables:
        private bool initialized; //True if tread system has already been set up

        public float engineRPM;      //Speed (in rotations per minute) at which engine is currently turning
        internal int gear;             //Basic designator for current direction and speed the tank is set to move in (0 = Neutral)
        private float brakeSaturation; //Time tank has spent braking (ticks up when brakes are active, ticks down when brakes are inactive)
        [Tooltip("Value between -1 and 1 representing current real normalized position of throttle.")] private float throttleValue; //Current value of the throttle, adjusted over time based on acceleration and gear for smooth movement
        
        private float treadHealth;         //Current tread health value
        private float jamEffectTimer = 0f; //Timer used to space out jam particle effect spawns
        public bool isJammed = false;     //Whether or not treads are currently jammed 

        //RUNTIME METHODS:
        private void Awake()
        {
            Initialize(); //Set up treads
        }
        private void Update()
        {
            //Update timers:
            brakeSaturation += Time.deltaTime * (gear == 0 && !isJammed ? 1 : -1);  //Increment or decrement value tracking brake saturation time (this is done so feathering the brake doesn't reset its saturation value and cause a jolt) (break saturation only decreases when treads are jammed)
            brakeSaturation = Mathf.Clamp(brakeSaturation, 0, brakeSaturationTime); //Clamp saturation value so it can't go negative (upper end isn't really necessary)

            //Update treads:
            for (int wheelIndex = 0; wheelIndex < wheels.Length; wheelIndex++) //Iterate once for each wheel
            {
                int nextWheelIndex = wheelIndex + 1; if (nextWheelIndex == wheels.Length) nextWheelIndex = 0; //Get index for wheel after current one (wrap around at last wheel)
                Transform wheel1 = wheels[wheelIndex].transform;                                              //Get transform from first wheel
                Transform wheel2 = wheels[nextWheelIndex].transform;                                          //Get transform from second wheel
                float treadWidth = Vector2.Distance(wheel1.position, wheel2.position);                        //Tread width is the exact distance between both wheels
                Vector2 treadPos = Vector2.Lerp(wheel1.position, wheel2.position, 0.5f);                      //Position of tread starts exactly between both wheels
                Vector2 treadNormal = Vector2.Perpendicular(wheel1.position - wheel2.position).normalized;    //Get normal of tread so that it can be moved later (using difference in position between both wheels)
                treadPos += treadNormal * wheels[wheelIndex].radius;                                          //Move tread position to account for radius of wheel

                Transform tread = treads[wheelIndex];                                               //Get tread at current wheel index
                tread.position = treadPos;                                                          //Move tread to target position
                tread.eulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.up, treadNormal); //Rotate tread to target rotation
                tread.localScale = new Vector3(treadWidth, tread.localScale.y, 1);                  //Scale tread to target length
            }

            //Check Ramming Speed
            float speed = r.velocity.x;
            if (Mathf.Abs(speed) >= rammingSpeed)
            {
                ramming = true;
            }
            else ramming = false;

            //Animate:
            if (centerGear != null) //Big treadbase gear is present
            {
                float gearSpeed = engineRPM * centerGearSpeedScale;             //Get angular speed at which gear is to be rotated (use engine speed because it's a useful piece of information to subtly break out)
                centerGear.Rotate(Vector3.forward, gearSpeed * Time.deltaTime); //Rotate gear
            }
        }
        private void FixedUpdate()
        {
            //Update Tread Health:
            if (treadHealth < treadMaxHealth) //Treads are damaged
            {
                treadHealth = Mathf.Min(treadHealth + (healthRegenRate * Time.fixedDeltaTime), treadMaxHealth); //Regen health according to regen rate (capping at max health)
                if (treadHealth >= unjamHealthThreshold)
                {
                    if (isJammed)
                    {
                        isJammed = false;                        //Unjam treads if above health threshold
                        jammedSprite.gameObject.SetActive(false);
                    }
                }
            }

            //Jam effects:
            if (isJammed) //Treads are currently jammed
            {
                jamEffectTimer -= Time.fixedDeltaTime; //Decrement jam effect timer

                if (jamEffectTimer <= 0) //Jam effect timer has run out
                {
                    //Randomize Position
                    float randomX = Random.Range(-3f, 3f);
                    float randomY = Random.Range(-0.7f, 0f);

                    Vector2 randomPos = new Vector2(transform.position.x + randomX, transform.position.y + randomY);

                    //Spark Particle
                    float particleScale = Random.Range(0.05f, 0.1f);
                    GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(14, randomPos, particleScale, transform);

                    //Smoke Particle
                    GameManager.Instance.ParticleSpawner.SpawnParticle(3, randomPos, particleScale, transform);

                    //Randomize Rotation
                    float randomRot = Random.Range(0f, 360f);
                    Quaternion newRot = Quaternion.Euler(0, 0, randomRot);
                    particle.transform.rotation = newRot;

                    //Randomize Interval between Sparks
                    float randomTimer = Random.Range(0.1f, 0.5f);
                    jamEffectTimer = randomTimer;

                    //TODO: Sparking Sound Effect here
                }
            }

            //Update throttle:
            throttleValue = gear / (float)((gearPositions - 1) / 2); //Get throttle value between -1 and 1 based on current gear setting and number of available gears

            //Update engine RPM:
            float targetRPM = Mathf.Lerp(-maxRPM, maxRPM, Mathf.InverseLerp(-1, 1, throttleValue));                                   //Get target RPM based on value of throttle
            if (GetEnginePower() == 0 || isJammed) targetRPM = 0;
            engineRPM = Mathf.Lerp(engineRPM, targetRPM, RPMAccelFactor);                                                             //Adjust RPM based on RPM acceleration factor (physics approximation, I dunno how to actually relate this accurately to throttle)
            float driveTorque = engineTorqueCurve.Evaluate(Mathf.Abs(engineRPM) / maxRPM) * Mathf.Sign(engineRPM) * GetEnginePower(); //Get amount of torque exerted by engine based on current RPM, maximum engine power, and engine torque curve
            if (isJammed) driveTorque = 0;                                                                                            //Cancel ALL drive torque if treads are jammed

            if (Time.timeScale != 1) return;

            //Apply wheel forces:
            Vector2 alignedVelocity = Vector3.Project(r.velocity, transform.right);         //Get velocity of tank aligned to tank's horizontal axis
            float longVelocity = alignedVelocity.magnitude * Mathf.Sign(alignedVelocity.x); //Get float velocity of tank along its forward axis (longitudinal velocity) NOTE: Might need to be modified to check on a wheel-by-wheel basis later
            float totalTractionTorque = 0;                                                  //Create container to add up sum of traction torque on all wheels (this is done as one operation because the wheels are all connected rotationally by the treads)
            foreach (TreadWheel wheel in wheels) //Iterate through wheel list
            {
                if (wheel.grounded) //Only apply force from grounded wheels
                {
                    //Get suspension force:
                    float suspensionMagnitude = wheel.stiffnessCurve.Evaluate(wheel.compressionValue) * wheel.stiffness; //Use wheel compression value and stiffness to determine magnitude of exerted force
                    float dragMagnitude = wheel.damper * wheel.springSpeed;                                              //Get magnitude of force applied by spring damper (drag and inefficiency of suspension)
                    Vector2 suspensionForce = transform.up * (suspensionMagnitude + dragMagnitude);                      //Get directional force to apply to rigidbody
                    r.AddForceAtPosition(suspensionForce, wheel.transform.position, ForceMode2D.Force);                  //Apply total spring forces to rigidbody at position of wheel

                    //Apply wheel stickiness:
                    if (!wheel.nonStick && wheel.springSpeed < 0) //Wheel appears to be leaving the ground (negative spring speed indicates that spring is decompressing)
                    {
                        float stickForce = wheelStickiness * -wheel.springSpeed * (1 - wheel.compressionValue);                       //Determine stick force based on setting and velocity of wheel decompression
                        stickForce *= wheelStickCompressionCurve.Evaluate(wheel.compressionValue);                                    //Modify stick force based on how compressed wheel is (prevents nasty behavior which artificially compresses tank)
                        r.AddForceAtPosition(-wheel.lastGroundHit.normal * stickForce, wheel.lastGroundHit.point, ForceMode2D.Force); //Apply downward stick force on tank at position of wheel (based on direction of wheel contact with ground)
                        Debug.DrawRay(wheel.lastGroundHit.point, -transform.up * stickForce, Color.yellow);
                    }
                }
            }
            foreach (TreadWheel wheel in wheels) //This needs to be done after other tank forces are applied
            {
                if (wheel.grounded) //Only apply force from grounded wheels
                {
                    //Terrain interaction behaviors:
                    Vector2 wheelDirection = Vector2.Perpendicular(wheel.lastGroundHit.normal); //Get direction wheel is applying force in
                    if (wheel.lastGroundHit.collider != null) //Wheel has valid information about hit ground
                    {
                        //Get traction:
                        float frictionLimit = frictionCoefficient * r.mass * -Physics2D.gravity.y;                                 //Determine the maximum amount of friction force which can be exerted by this wheel based on how much weight is on it (how much force it is supplying to keeping the tank suspended)
                        float slipDelta = (((Mathf.Deg2Rad * -wheel.angularVelocity) * wheel.radius) - longVelocity);              //Get difference in speed between tank ground velocity and linear desired motion of wheel
                        slipDelta = Mathf.Clamp(slipDelta, -maxSlipRatio, maxSlipRatio);                                           //Clamp output slip ratio to prevent HUGE values from propogating and annihilating the tank
                        slipDelta = slipRatioCurve.Evaluate(Mathf.InverseLerp(0, maxSlipRatio, Mathf.Abs(slipDelta))) * slipDelta; //Apply slip ratio curve so that wheel traction is highest at certain slip ratios (usually <10%) and then falls off at higher speeds (burnouts)
                        float tractionForce = -slipDelta * frictionLimit;                                                          //Get traction force as a product of the slip ratio between the ground and the wheel, and the maximum amount of friction allowed to be produced by the wheel based on load

                        //Apply traction forces:
                        r.AddForceAtPosition(wheelDirection * tractionForce * Time.fixedDeltaTime, wheel.lastGroundHit.point, ForceMode2D.Force); //Apply traction force induced by friction between wheel and ground
                        totalTractionTorque -= tractionForce * wheel.radius;                                                                      //Get wheel torque induced by traction
                    }
                }
            }

            //Apply wheel torques:
            float totalWheelTorque = totalTractionTorque - driveTorque; //Add up torques affecting wheels NOTE: Add torque for brakes here
            float wheelAngAccel = totalWheelTorque / treadInertia;      //Get amount by which to angularly accelerate each wheel (based on inertia (cumulative system mass) of moving parts in tread system)
            foreach (TreadWheel wheel in wheels) //Iterate through wheel list AGAIN now that traction torques have been calculated
            {
                wheel.angularVelocity += wheelAngAccel * Mathf.Rad2Deg * Time.fixedDeltaTime;                                                                  //Accelerate all wheels together by the same amount
                wheel.angularVelocity += axleDragCoefficient * Mathf.Pow(wheel.angularVelocity, 2) * Time.fixedDeltaTime * -Mathf.Sign(wheel.angularVelocity); //Apply axle drag coefficient to angular velocity of wheel (always opposing wheel rotation direction)
                if (gear == 0 && brakeSaturation >= brakeDwellTime) //Brakes are active
                {
                    float brakeValue = Mathf.InverseLerp(brakeDwellTime, brakeSaturationTime, brakeSaturation);                                           //Get value representing how far along brake is in phase
                    brakeValue = brakeSaturationCurve.Evaluate(brakeValue) * brakeDragCoefficient;                                                        //Evaluate efficacy of brakes based on
                    wheel.angularVelocity += brakeValue * Mathf.Pow(wheel.angularVelocity, 2) * Time.fixedDeltaTime * -Mathf.Sign(wheel.angularVelocity); //Apply brake drag coefficient to angular velocity of wheel (always opposing wheel rotation direction)
                }
            }

            //NOTE: EVERYTHING below needs a revision pass

            //Add air drag:
            float actualAirDrag = baseAirDragForce * Time.fixedDeltaTime * r.velocity.x; //Calculate air drag based on given value and horizontal speed of tank
            r.AddTorque(actualAirDrag, ForceMode2D.Force);                               //Apply force as torque to tread system rigidbody (tilting it away from direction of motion)

            //Add angular drag:
            //NOTE: REWORK THIS
            //groundedWheels = Mathf.Min(groundedWheels, wheels.Length);                            //Cap grounded wheels in case extras would push number over calculated maximum
            //r.angularDrag = maxAngularDrag * Mathf.Min((float)groundedWheels / wheels.Length, 1); //Make angular drag proportional to number of grounded (non-extra) wheels

            //Prevent tipping:
            if (r.rotation > maxTipAngle - tipAngleBufferZone || r.rotation < (maxTipAngle - tipAngleBufferZone)) //Tank is getting close to tipping over
            {
                if (r.rotation > maxTipAngle || r.rotation < -maxTipAngle) //Tank is exceeding absolute max tip angle
                {
                    r.MoveRotation(maxTipAngle * Mathf.Sign(r.rotation)); //Hard cap tip angle
                    r.angularVelocity = 0;                                //Negate angular velocity
                }
                float correctiveForce = Mathf.InverseLerp(maxTipAngle - tipAngleBufferZone, maxTipAngle, Mathf.Abs(r.rotation)); //Get value between 0 and 1 representing how close tank is to tipping over
                correctiveForce *= tipPreventionForce * -Mathf.Sign(r.rotation);                                                 //Calculate corrective force based on setting, angular position in buffer zone, and direction of tippage
                r.AddTorque(correctiveForce, ForceMode2D.Force);                                                                 //Apply corrective force to prevent tippage
            }

            //Update SFX
            UpdateSFX();

            //Update RB
            lastAngVelocity = r.angularVelocity;
            lastVelocity = r.velocity;
        }

        private void OnDrawGizmos()
        {
            if (gizmosOn) //Only draw gizmos if functionality is turned on
            {
                //Draw center of mass:
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.TransformPoint(new Vector2(-COGWidth / 2, COGHeight)), transform.TransformPoint(new Vector2(COGWidth / 2, COGHeight)));
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(GetComponent<Rigidbody2D>().worldCenterOfMass, 0.2f);

                //Draw tip prevention diagram:
                Gizmos.DrawRay(transform.position, transform.up * 3);
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(maxTipAngle, Vector3.forward) * Vector3.up * 3);
                Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(-maxTipAngle, Vector3.forward) * Vector3.up * 3);
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(maxTipAngle - tipAngleBufferZone, Vector3.forward) * Vector3.up * 3);
                Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(-(maxTipAngle - tipAngleBufferZone), Vector3.forward) * Vector3.up * 3);

                //Draw impact height limit:
                Gizmos.color = Color.magenta;
                Vector2 heightLimitPoint = transform.position + (transform.up * maximumRelativeImpactHeight);
                Gizmos.DrawLine(heightLimitPoint - (Vector2)(transform.right * 3), heightLimitPoint + (Vector2)(transform.right * 3));

                //Draw bounds:
                if (Application.isPlaying)
                {
                    Gizmos.color = Color.green;
                    Bounds treadBounds = GetTreadBounds();
                    Gizmos.DrawWireCube(treadBounds.center, treadBounds.size);
                }
            }
        }

        //FUNCTIONALITY METHODS:
        public void Initialize()
        {
            //Initialization check:
            if (initialized) return; //Do not re-initialize treads
            initialized = true;      //Indicate that treads have been initialized

            //Get objects & components:
            tankController = GetComponentInParent<TankController>(); //Get tank controller object from parent
            r = GetComponent<Rigidbody2D>();                         //Get rigidbody component
            wheels = GetComponentsInChildren<TreadWheel>();          //Get array of all wheels in system
            animator = GetComponent<Animator>();

            //Generate treads:
            List<Transform> newTreads = new List<Transform>(); //Instantiate list to store spawned treads
            for (int x = 0; x < wheels.Length; x++) //Iterate once for each wheel in tank
            {
                Transform newTread = Instantiate(treadPrefab, transform).transform; //Instantiate new tread object
                newTreads.Add(newTread);                                            //Add new tread to list
            }
            treads = newTreads.ToArray(); //Commit generated list to array

            //Get starting variables:
            treadHealth = treadMaxHealth; //Set up tread health

            //Generate collider system:
            colliderSystem = new GameObject("ColliderSystem").transform; //Generate empty gameobject
            colliderSystem.transform.parent = transform;                 //Child system to treadSystem
            colliderSystem.transform.localPosition = Vector3.zero;       //Zero out position for neatness
            colliderSystem.transform.localEulerAngles = Vector3.zero;    //Zero out rotation for neatness
        }

        /// <summary>
        /// Shifts to target gear.
        /// </summary>
        /// <param name="targetGear"></param>
        public void ChangeGear(int targetGear)
        {
            gear = -targetGear; //Update gear setting
            brakeSaturation = 0;     //Reset gear time tracker
        }

        /// <summary>
        /// Damages treads with given projectile and assigns impact value.
        /// </summary>
        /// <param name="projectile">Projectile damaging the treadsystem.</param>
        /// <param name="position">Position on treadsystem which projectile is striking.</param>
        /// <returns>Remaining damage after projectile damage is assigned (used for tunneling)</returns>
        public float Damage(Projectile projectile, Vector2 position)
        {
            //Check For Land Mine
            bool overrideRelative = false;
            if (projectile.type == Projectile.ProjectileType.OTHER) { overrideRelative = true; }

            //Handle projectile effects:
            HandleImpact(projectile, position, overrideRelative); //Handle impact from projectile
            Damage(projectile.remainingDamage, true); //Assign damage to treads

            //Other effects:
            GameManager.Instance.AudioManager.Play("TankImpact", gameObject); //Play tread impact sound
            TankController tank = this.tankController;
            if (tank != null)
            {
                float duration = Mathf.Lerp(0.05f, 1f, projectile.remainingDamage / 100);
                float intensity = Mathf.Lerp(1f, 15f, projectile.remainingDamage / 100);
                CameraManipulator.main?.ShakeTankCamera(tank, intensity, duration);

                if (overrideRelative)
                {
                    CameraManipulator.main?.ShakeTankCamera(tank, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Explosion"));

                    //Apply Haptics to Players inside this tank
                    foreach (Character character in tank.GetCharactersInTank())
                    {
                        PlayerMovement player = character.GetComponent<PlayerMovement>();
                        if (player != null)
                        {
                            HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("LowRumble");
                            GameManager.Instance.SystemEffects.ApplyControllerHaptics(player.GetPlayerData().playerInput, setting); //Apply haptics
                        }
                    }
                }
            }

            //Cleanup:
            return 0; //Never allow projectiles to pass through treads
        }
        public float Damage(float damage, bool triggerHitEffects = false)
        {
            treadHealth -= damage; //Reduce tread health

            //Effects
            if (triggerHitEffects)
            {
                if (damage > 40) HitEffects(0.5f);
                else HitEffects(4f);
            }

            //Check jam:
            if (treadHealth <= 0) //Tread health has fallen below zero
            {
                treadHealth = 0; //Clamp tread health to zero
                Jam();           //Jam treads
            }

            return damage;
        }

        public void HitEffects(float speedScale)
        {
            animator.SetFloat("SpeedScale", speedScale);
            animator.Play("TreadFlash", 0, 0);
        }

        public void Unjam()
        {
            isJammed = false;
            jammedSprite.gameObject.SetActive(false);
            if (treadHealth < treadMaxHealth)
            {
                treadHealth = treadMaxHealth;
                GameManager.Instance.AudioManager.Play("ItemPickup", gameObject);
               
                HitEffects(1.5f);
                GameManager.Instance.AudioManager.Play("UseWrench", gameObject);
            }
        }

        /// <summary>
        /// Processes physical impact force from given projectile.
        /// </summary>
        /// <param name="projectile">Projectile doing the impact.</param>
        /// <param name="point">Point of impact.</param>
        /// <param name="overrideRelative">Ignores projectile's relative velocity (used for stationary projectiles)</param>
        public void HandleImpact(Projectile projectile, Vector2 point, bool overrideRelative = false)
        {
            if (r == null) return;

            //Handle initial impact:
            Vector2 relativeVelocity = projectile.velocity - r.GetPointVelocity(point); //Get difference in velocity between projectile and tread system at point of impact
            float sign = Mathf.Sign(projectile.transform.position.x - transform.position.x);
            if (overrideRelative) relativeVelocity = new Vector2(40 * -sign, 40) - r.GetPointVelocity(point);
            Vector2 impactForce = projectile.hitProperties.mass * relativeVelocity;     //Get impact force as result of mass times (relative) velocity
            //if (ramming) impactForce *= 0.5f;
            HandleImpact(impactForce, point);                                           //Pass to basic impact handler

            //Handle extra slam force:
            Vector2 slamImpactForce = relativeVelocity * projectile.hitProperties.slamForce; //Get additional impact force used to push tanks around (for gameplay reasons)
            //if (ramming) slamImpactForce *= 0.5f;
            r.AddForce(slamImpactForce, ForceMode2D.Force);                                  //Add force to rigidbody without inducing torque
        }
        /// <summary>
        /// Processes physical impact force from a generic source.
        /// </summary>
        /// <param name="force">Direction and magnitude of force.</param>
        /// <param name="point">Point (in world space) on tank at which force is being applied.</param>
        public void HandleImpact(Vector2 force, Vector2 point)
        {
            //Limit height of impact point:
            float deltaHeight = Vector3.Project((Vector3)point - transform.position, transform.up).magnitude;                              //Get difference in height between impact point and center of tank (linear difference is aligned with tank up value)
            if (deltaHeight > maximumRelativeImpactHeight) point -= (Vector2)(transform.up * (deltaHeight - maximumRelativeImpactHeight)); //Adjust impact point downward if it is higher than allowed level on tank

            //Cleanup:
            r.AddForceAtPosition(force, point, ForceMode2D.Force); //Apply force to rigidbody
            Debug.DrawLine(point, point + force, Color.red, 1.5f);
        }

        public void SetVelocity(float percentageOffset = 1)
        {
            //lastAngVelocity *= percentageOffset;
            lastVelocity *= percentageOffset;
            r.angularVelocity = lastAngVelocity;
            r.velocity = lastVelocity;
        }

        public void Jam()
        {
            if (!isJammed)
            {
                isJammed = true;
                GameManager.Instance.AudioManager.Play("EngineDyingSFX", this.gameObject);
                jammedSprite.gameObject.SetActive(true);
            }
        }

        public void UpdateSFX()
        {
            //Engine Sound
            if (!isJammed && GameManager.Instance.currentSceneState != SCENESTATE.BuildScene)
            {
                if (!GameManager.Instance.AudioManager.IsPlaying("TankIdle", this.gameObject)) GameManager.Instance.AudioManager.Play("TankIdle", this.gameObject);
            }
            else if (GameManager.Instance.AudioManager.IsPlaying("TankIdle", this.gameObject)) GameManager.Instance.AudioManager.Stop("TankIdle", this.gameObject);

            //Update Pitch
            float engineRatio = (Mathf.Abs(engineRPM) / maxRPM) * 100f;
            Mathf.Clamp(engineRatio, 0, 100);
            GameManager.Instance.AudioManager.UpdateRTPCValue("EnginePitch", engineRatio, this.gameObject);

            //Wheels Moving
            if (Mathf.Abs(engineRPM) > minRPM)
            {
                if (!GameManager.Instance.AudioManager.IsPlaying("TreadsRolling", this.gameObject)) GameManager.Instance.AudioManager.Play("TreadsRolling", this.gameObject);
            }
            else if (GameManager.Instance.AudioManager.IsPlaying("TreadsRolling", this.gameObject)) GameManager.Instance.AudioManager.Stop("TreadsRolling", this.gameObject);
        }

        //UTILITY METHODS:
        /// <summary>
        /// Evaluates mass and center of gravity for tank depending on position and quantity of cells.
        /// </summary>
        public void ReCalculateMass()
        {
            //Initialization:
            int cellCount = 0;                  //Initialize value to store number of cells counted by evaluation
            Vector2 avgCellPos = new Vector2(); //Initialize value to store average cell position

            //Cell roundup:
            foreach (Room room in tankController.rooms) //Iterate through each room in tank
            {
                foreach (Cell cell in room.cells) //Iterate through each cell in room
                {
                    cellCount++;                                                                     //Add to cell count
                    avgCellPos += (Vector2)transform.InverseTransformPoint(cell.transform.position); //Add local position of cell (relative to tank) to average
                }
            }

            //Calculation:

            r.mass = cellCount * cellWeight * massMultiplier; //Apply mass of tank according to cell weight and overall multiplier factors
            print("Final Mass = " + r.mass);
            avgCellPos /= cellCount;                          //Get average position of cells
            r.centerOfMass = Vector2.zero; //NOTE: Temporary
            //r.centerOfMass = new Vector2(Mathf.Clamp(avgCellPos.x, -COGWidth / 2, COGWidth / 2), COGHeight); //Constrain center mass to line segment controlled in settings (for tank handling reliability)
        }
        /// <summary>
        /// Calculates cumulative torque of base engine plus additional torque from each boiler.
        /// </summary>
        /// <returns></returns>
        private float GetEnginePower()
        {
            float totalEnginePower = baseEnginePower;                                                //Start value with base power of tread engine
            EngineController[] engines = tankController.GetComponentsInChildren<EngineController>(); //Get array of all engines in tank
            foreach(EngineController engine in engines) //Iterate through engine array
            {
                totalEnginePower += engine.power * boilerPowerAdd;          //Add power proportional to how stoked boiler is
                if (engine.isSurging) totalEnginePower += boilerSurgePower; //Add more power if boiler is surging
            }
            return totalEnginePower; //Return calculated engine power
        }
        /// <summary>
        /// Returns bounding box which encapsulates entire tank.
        /// </summary>
        public Bounds GetTankBounds()
        {
            Bounds tankBounds = new Bounds(transform.position, Vector3.zero);                         //Start with zeroed-out bounds at center point of the tank
            foreach (Room room in tankController.rooms) tankBounds.Encapsulate(room.GetRoomBounds()); //Encapsulate bounds of each room in tank
            tankBounds.Encapsulate(GetTreadBounds());                                                 //Encapsulate treadbase bounds (including wheels)
            return tankBounds;                                                                        //Return fully-encapsulated bounds
        }
        /// <summary>
        /// Returns bounding box which encapsulates tank treads (including wheels and treadbase).
        /// </summary>
        /// <returns></returns>
        private Bounds GetTreadBounds()
        {
            Bounds treadBounds = new Bounds(transform.position, Vector3.zero); //Start with zeroed-out bounds at center point of the tank
            foreach (TreadWheel wheel in wheels) //Iterate through wheels in treadsystem
            {
                Bounds wheelBounds = new Bounds(wheel.transform.position, wheel.radius * 2 * Vector2.one); //Get square bounds which encapsulate wheel
                treadBounds.Encapsulate(wheelBounds);                                                      //Encapsulate wheel bounds
            }
            foreach (BoxCollider2D treadBaseColl in transform.GetComponentsInChildren<BoxCollider2D>()) //Iterate through colliders in treadbase
            {
                treadBounds.Encapsulate(treadBaseColl.bounds); //Encapsulate collider of each object in treadbase (might need some massaging later)
            }
            return treadBounds; //Return fully-encapsulated bounds
        }

        public void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponentInParent<TreadSystem>() != null) //Tread has collided with another tank
            {
                //Get information:
                TreadSystem opposingTreads = collision.collider.GetComponentInParent<TreadSystem>(); //Get treadsystem of opposing tank
                if (collision.collider.gameObject.GetComponent<BoxCollider2D>() == null) return; //only do collision-related code when colliding with the treadbase

                if (collision.contacts.Length > 0) //Hit properties can only be handled if there is an actual contact
                {
                    //Apply collision properties:
                    ContactPoint2D contact = collision.GetContact(0);
                    opposingTreads.HandleImpact(-collision.GetContact(0).normal * 75, contact.point);
                    opposingTreads.Damage(20, true);
                    Damage(20, true);

                    //Other effects:
                    //GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
                    GameManager.Instance.AudioManager.Play("TankImpact", gameObject);
                    for (int x = 0; x < 3; x++) //Spawn cloud of particle effects
                    {
                        Vector2 offset = Random.insideUnitCircle * 0.20f;
                        GameManager.Instance.ParticleSpawner.SpawnParticle(25, contact.point + offset, 1f);
                    }

                    //Camera Shake
                    CameraManipulator.main?.ShakeTankCamera(tankController, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Impact"));

                    //Apply Haptics to Players inside this tank
                    foreach (Character character in tankController.GetCharactersInTank())
                    {
                        PlayerMovement player = character.GetComponent<PlayerMovement>();
                        if (player != null)
                        {
                            HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("ImpactRumble");
                            GameManager.Instance.SystemEffects.ApplyControllerHaptics(player.GetPlayerData().playerInput, setting); //Apply haptics
                        }
                    }
                }
            }

            if (collision.collider.GetComponentInParent<DestructibleObject>() != null) //Room has collided with an obstacle
            {
                DestructibleObject obstacle = collision.collider.GetComponentInParent<DestructibleObject>();

                if (collision.contacts.Length > 0 && obstacle.isObstacle)
                {
                    if (collision.otherCollider.gameObject.GetComponent<BoxCollider2D>() == null) return; //only do collision-related code when colliding with the treadbase
                    //Get Point of Contact
                    ContactPoint2D contact = collision.GetContact(0);

                    //Calculate impact magnitude
                    float impactSpeed = contact.relativeVelocity.magnitude; //Get speed of impact
                    //Debug.Log("Speed of Impact: " + impactSpeed);
                    float impactDamage = (10 * Mathf.Abs(impactSpeed)) * obstacle.collisionResistance;
                    if (ramming) impactDamage = 200f; //double the impact damage

                    //Apply Collision Properties
                    float knockbackForce = 75f;

                    if (!ramming && collision.contactCount > 0) HandleImpact(collision.GetContact(0).normal * knockbackForce, contact.point);
                    else SetVelocity(0.8f);

                    obstacle.ApplyImpactDirection(collision.GetContact(0).normal * knockbackForce, contact.point);
                    obstacle.Damage(impactDamage);
                    Damage(20, true);

                    //Other effects:
                    GameManager.Instance.AudioManager.Play("TankImpact", obstacle.gameObject);
                    for (int x = 0; x < 3; x++) //Spawn cloud of particle effects
                    {
                        Vector2 offset = Random.insideUnitCircle * 0.10f;
                        GameManager.Instance.ParticleSpawner.SpawnParticle(25, contact.point + offset, 1f);
                    }

                    //Camera Shake
                    if (!ramming) CameraManipulator.main?.ShakeTankCamera(tankController, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Jolt"));
                    CameraManipulator.main?.ShakeTankCamera(tankController, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Jolt"));

                    //Apply Haptics to Players inside this tank
                    foreach (Character character in tankController.GetCharactersInTank())
                    {
                        PlayerMovement player = character.GetComponent<PlayerMovement>();
                        if (player != null)
                        {
                            HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("ImpactJolt");
                            if (ramming) setting = GameManager.Instance.SystemEffects.GetHapticsSetting("QuickJolt");
                            GameManager.Instance.SystemEffects.ApplyControllerHaptics(player.GetPlayerData().playerInput, setting); //Apply haptics
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the miles per hour of the tank.
        /// </summary>
        /// <returns>f(x) = magnitude * 2.23693629 (conversion from meters to mph).</returns>
        public float GetMPH() => r.velocity.magnitude * 2.23693629f;
        public void OnDestroy()
        {
            if (GameManager.Instance.AudioManager.IsPlaying("TankIdle", this.gameObject)) GameManager.Instance.AudioManager.Stop("TankIdle", this.gameObject);
            if (GameManager.Instance.AudioManager.IsPlaying("TreadsRolling", this.gameObject)) GameManager.Instance.AudioManager.Stop("TreadsRolling", this.gameObject);
        }
    }
}
