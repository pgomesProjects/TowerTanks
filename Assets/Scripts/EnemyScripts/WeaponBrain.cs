using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class WeaponBrain : InteractableBrain
    {
        internal GunController gunScript;
        private Projectile myProjectile;
        
        internal float fireTimer;
        public float cooldownOffset;
        
        public bool isRotating;
        internal float currentForce;
        internal RaycastHit2D aimHit;
        
        internal float minTurnSpeed = 00f;
        internal float maxTurnSpeed = 1f;
        internal Vector3 targetPoint;
        protected Vector3 targetPointOffset;
        protected bool miss;
        private bool started;
        protected bool stopFiring = false;
        private bool obstruction;
        protected float aggroCooldownTimer;
        public float aggroCooldown;
        private Transform overrideTarget;
        public Coroutine updateAimTarget;

        private void Awake()
        {
            gunScript = GetComponent<GunController>();
            myProjectile = gunScript.projectilePrefab.GetComponent<Projectile>();
            
        }

        public override void Init()
        {
            fireTimer = 0;
            gunScript.usingAIbrain = true;
            StartCoroutine(AimAtTarget());
            updateAimTarget = StartCoroutine(UpdateTargetPoint(myTankAI.aiSettings.tankAccuracy));
            aggroCooldown = Mathf.Lerp(myTankAI.aiSettings.maxFireCooldown, 0, myTankAI.aiSettings.aggression);
            gunScript.fireCooldownTimer = aggroCooldown + GetAggroOffset();
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            gunScript.RotateBarrel(currentForce, false);

            if (myTankAI.aiSettings.aggression == 0) stopFiring = true;
            if (!AimingAtMyself() && !stopFiring && !obstruction)
            {
                if (gunScript.gunType == GunController.GunType.MACHINEGUN ||
                    aggroCooldownTimer > aggroCooldown)
                {
                    gunScript.Fire(false, gunScript.tank.tankType, bypassSpinup:true);
                    if (gunScript.gunType != GunController.GunType.MACHINEGUN)
                    {
                        aggroCooldownTimer = 0 + GetAggroOffset();
                    }
                }
                    
                fireTimer = 0;
            }
            if (DirectionToTargetBlocked() && gunScript.gunType != GunController.GunType.MORTAR)
            {
                obstruction = true;
            } else
            {
                obstruction = false;
            }
            
            if (myTankAI.targetTank != null && overrideTarget == null)
            {
                targetPoint = myTankAI.targetTank.treadSystem.transform.position + targetPointOffset;
            }
            
        }

        protected virtual void FixedUpdate()
        {
            fireTimer += Time.fixedDeltaTime; //firetimer is used for rate of fire
            if (myTankAI.aiSettings.aggression != 0 && gunScript.fireCooldownTimer <= 0) aggroCooldownTimer += Time.fixedDeltaTime;
        }

        private bool AimingAtMyself()
        {
            return aimHit.collider != null && aimHit.collider.transform.IsChildOf(gunScript.tank.transform);
        }
        
        private bool DirectionToTargetBlocked() // this is necessary because we want to check if we WOULD hit our own tank
                                                // IF we were to shoot exactly at the target right now. AimingAtMyself
        {                                       // just tells you if the CURRENT aim will hit the tank or not
            var excludeLayer = (1 << LayerMask.NameToLayer("Camera")) |
                               (1 << LayerMask.NameToLayer("Projectiles"));
            return Physics2D.Raycast(gunScript.barrel.position, targetPoint - gunScript.barrel.position, 5, ~excludeLayer).collider != null;
        }
        
        protected Vector2 GetRandomPointBetweenVectors(Vector2 pointA, Vector2 pointB)
        {
            float t = Random.Range(0f, 1f);
            return Vector2.Lerp(pointA, pointB, t); // it just works
        }
        
        /// <summary>
        /// Will change this weapon's current aim target.
        /// </summary>
        /// <param name="aimFactor">
        ///  A value between 0 and 100 which determines accuracy. 100 is the most accurate, 0 is the least accurate.
        /// </param>
        public virtual IEnumerator UpdateTargetPoint(float aimFactor)
        {
            while (tokenActivated)
            {
                if (overrideTarget == null && myTankAI.targetTank != myTankAI.tank && myTankAI.targetTank != null && myTankAI.targetTank.treadSystem != null)
                {
                    var targetTankTransform = myTankAI.targetTank.treadSystem.transform;
                    var upmostCell = myTankAI.targetTank.upMostCell.transform;
                    // get random number between 0 and aimfactor
                    float randomX = Random.Range(0, 100);
                    bool hit = randomX <= aimFactor;
                
                    if (hit)
                    {
                        miss = false;
                        targetPoint = GetRandomPointBetweenVectors(targetTankTransform.position + targetTankTransform.up * 1.5f, upmostCell.position);
                        targetPointOffset = targetPoint - targetTankTransform.position;
                        Debug.DrawLine(targetTankTransform.position, targetPoint, Color.green, 5);
                    }
                    else
                    {
                        miss = true;
                        Debug.DrawLine(targetTankTransform.position, targetPoint, Color.green, 3);
                        var pointBelowTarget = targetTankTransform.position - targetTankTransform.up * 2f;
                        var pointAboveTarget = upmostCell.position + targetTankTransform.up * 4f;
                        var rand = Random.Range(0, 2);
                        if (rand == 0) targetPoint = GetRandomPointBetweenVectors(upmostCell.position + targetTankTransform.up * 1.5f, pointAboveTarget);
                        else           targetPoint = GetRandomPointBetweenVectors(targetTankTransform.position - targetTankTransform.up, pointBelowTarget);
                        targetPointOffset = targetPoint - targetTankTransform.position;
                    }
                }
                

                var time = 3f;
                if (overrideTarget != null)
                {
                    targetPoint = overrideTarget.position;
                    time = .03f;
                } 
                yield return new WaitForSeconds(time);
            }
            
        }

        protected virtual IEnumerator AimAtTarget(float refreshRate = 0.1f, bool everyFrame = false)
        {
            while (tokenActivated)
            {
                var trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position, gunScript.barrel.right * gunScript.muzzleVelocity, myProjectile.gravity, 100);
                aimHit = Trajectory.GetHitPoint(trajectoryPoints);

                Vector3 myTankPosition = myTankAI.tank.treadSystem.transform.position + Vector3.up * 2.5f;

                if (DirectionToTargetBlocked() && !miss && overrideTarget == null) targetPoint = gunScript.transform.right; // basically if the direction from our weapon
                                                                                            // to its target point is obstructed by the weapon's tank, 
                                                                                            // this will aim the weapon forward instead of at the target
                
                bool hitPointIsRightOfTarget = aimHit.point.x > targetPoint.x;

                // if our projected hitpoint is past the tank we're fighting, the hitpoint is set right in front of the barrel, because in that scenario we want to aim based on our gun's general direction and not our hitpoint (this doesnt apply to mortars)
                if ((!myTankAI.TankIsRightOfTarget() && hitPointIsRightOfTarget) || (myTankAI.TankIsRightOfTarget() && !hitPointIsRightOfTarget) || aimHit.collider == null || AimingAtMyself())
                {
                    aimHit.point = trajectoryPoints[2];
                }

                Vector3 direction = targetPoint - myTankPosition;

                // Project the hit point onto the direction vector
                Vector3 aimHitPoint = aimHit.point; //converts to vec3 (using vec3 for project function)
                Vector3 projectedPoint = myTankPosition + Vector3.Project(aimHitPoint - myTankPosition, direction);
                if (DirectionToTargetBlocked() && overrideTarget == null) projectedPoint = gunScript.barrel.position;
                float HowFarFromTarget() => Vector3.Distance(aimHit.point, projectedPoint);
                
                var distFactor = Mathf.InverseLerp(0, 2, HowFarFromTarget());
                var moveSpeed = Mathf.Lerp(minTurnSpeed, maxTurnSpeed, distFactor);
                // Determine if the hit point is above or below the projected point
                if (aimHit.point.y > projectedPoint.y)
                {
                    currentForce = myTankAI.TankIsRightOfTarget() ? moveSpeed : -moveSpeed;
                }
                else
                {
                    currentForce = myTankAI.TankIsRightOfTarget() ? -moveSpeed : moveSpeed;
                }
                
                yield return new WaitForSeconds(refreshRate);
            }
            
        }

        public float GetAggroOffset()
        {
            float number = 0;
            float offset = myTankAI.aiSettings.aggressionCooldownOffset;

            if (aggroCooldown >= offset) //Only apply offset when it can't reduce our cooldown timer below 0
            {
                float random = Random.Range(-offset, offset);
                number = 0 + random;
            }

            return number;
        }
        
        public void OverrideTargetPoint(Transform target)
        {
            overrideTarget = target;
        }

        public void ResetTargetPoint()
        {
            overrideTarget = null;
        }

        private void OnDrawGizmos()
        {
            bool mortar = mySpecificType == INTERACTABLE.Mortar;
            Vector2 fireVelocity = (mortar ? gunScript.barrel.up : gunScript.barrel.right) * gunScript.muzzleVelocity;
            if (mortar) fireVelocity += myTankAI.tank.treadSystem.r.velocity;
            var trajPoints = Trajectory.GetTrajectory(gunScript.barrel.position, 
                                                    fireVelocity,
                                                                   myProjectile.gravity,
                                                     100);
            if (!tokenActivated) return;
            for (int i = 0; i < trajPoints.Count - 1; i++)
            {
                if (Vector3.Distance(trajPoints[i], aimHit.point) < 0.25f) break; // stops projecting line at target
                Gizmos.color = myTankAI.TankIsRightOfTarget() ? Color.red : Color.blue; //make tank is right of target change to player if the only tank is itself
                Gizmos.DrawLine(trajPoints[i], trajPoints[i + 1]);
                
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(aimHit.point, 0.5f);
            
            
            Gizmos.color = Color.blue;
            if (mySpecificType == INTERACTABLE.Mortar) Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPoint, 1f);
            
        }
    }
}