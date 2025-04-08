using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class WeaponBrain : InteractableBrain
    {
        internal GunController gunScript;
        private Projectile myProjectile;
        

        internal float currentForce;
        internal RaycastHit2D aimHit;
        
        internal float minTurnSpeed = 00f;
        internal float maxTurnSpeed = 1f;
        internal Vector3 targetPoint;
        protected Vector3 targetPointOffset;
        protected bool miss;
        private bool started;
        protected bool stopFiring = false;
        protected float aggroCooldownTimer;
        public float aggroCooldown;
        protected Transform overrideTarget;
        public Coroutine updateAimTarget;
        bool aimingPastOurTarget;
        protected bool shotFired;
        [Tooltip("The curve used to slow the aiming of the gun. The closer the gun gets to the target, the slower" +
                 " it will aim, based on this curve. So, once the gun is at it's target, it will stop rotating.")]
        public AnimationCurve AimingCurve; 
        protected override void Awake()
        {
            base.Awake();
            gunScript = GetComponent<GunController>();
            myProjectile = gunScript.projectilePrefab.GetComponent<Projectile>();
            
        }

        public override void Init()
        {
            gunScript.usingAIbrain = true;
            StartCoroutine(AimAtTarget());
            updateAimTarget = StartCoroutine(UpdateTargetPoint(myTankAI.aiSettings.tankAccuracy));
            aggroCooldown = Mathf.Lerp(myTankAI.aiSettings.maxFireCooldown, 0, myTankAI.aiSettings.aggression);
            gunScript.fireCooldownTimer = aggroCooldown + GetAggroOffset();
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            gunScript.RotateBarrel(currentForce, false);

            if (myTankAI.aiSettings.aggression == 0) stopFiring = true;
            if ((!AimingAtMyself() || overrideTarget) && !stopFiring) //if we have an override target, we want to fire regardless of where we are aiming (mortars )
            {
                if (gunScript.gunType == GunController.GunType.MACHINEGUN ||
                    aggroCooldownTimer > aggroCooldown)
                {
                    gunScript.Fire(false, gunScript.tank.tankType, bypassSpinup:true);
                    if (gunScript.gunType != GunController.GunType.MACHINEGUN)
                    {
                        aggroCooldownTimer = 0 + GetAggroOffset();
                        shotFired = true;
                    }
                }
                
            }
            
            
            if (myTankAI.targetTank != null && overrideTarget == null && (!DirectionToTargetBlocked() || gunScript.gunType == GunController.GunType.MORTAR))
            {
                targetPoint = myTankAI.targetTank.treadSystem.transform.position + targetPointOffset;
            }
            
        }

        protected virtual void FixedUpdate()
        {
            if (myTankAI.aiSettings.aggression != 0 && gunScript.fireCooldownTimer <= 0) aggroCooldownTimer += Time.fixedDeltaTime;
        }

        protected bool AimingAtMyself()
        {
            return aimHit.collider != null && aimHit.collider.transform.IsChildOf(gunScript.tank.transform)
                   /*|| (DirectionToTargetBlocked() && gunScript.gunType != GunController.GunType.MORTAR)*/;
        }
        
        protected bool AimingAtGround()
        {
            return aimHit.collider != null &&
                !aimHit.collider.transform.IsChildOf(myTankAI.targetTank.transform) &&
                   aimHit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") &&
                   !aimingPastOurTarget && !AimingAtMyself();
        }
        
        protected bool DirectionToTargetBlocked() // this is necessary because we want to check if we WOULD hit our own tank
                                                // IF we were to shoot exactly at the target right now. AimingAtMyself
        {                                       // just tells you if the CURRENT aim will hit the tank or not
            var excludeLayer = (1 << LayerMask.NameToLayer("Camera")) |
                                   (1 << LayerMask.NameToLayer("Projectiles")) |
                                   (1 << LayerMask.NameToLayer("Player"));
            
            Vector3 direction = targetPoint - gunScript.transform.position;
            return Physics2D.CircleCast(gunScript.barrel.position, .1f, direction, 5, ~excludeLayer).collider != null;
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
                if (overrideTarget == null && myTankAI.targetTank != null && !DirectionToTargetBlocked())
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
                        if (rand == 0) targetPoint = GetRandomPointBetweenVectors(upmostCell.position + targetTankTransform.up * 1.5f, pointAboveTarget); //misses the tank by aiming too high
                        else           targetPoint = GetRandomPointBetweenVectors(targetTankTransform.position - targetTankTransform.up, pointBelowTarget); //misses the tank by aiming too low
                        targetPointOffset = targetPoint - targetTankTransform.position;
                    }
                }
                
                
                if (overrideTarget != null) // if we have an override target, we want to update the target point very quickly
                {
                    targetPoint = overrideTarget.position;
                    yield return new WaitForSeconds(.03f);
                } else if (gunScript.gunType != GunController.GunType.MACHINEGUN) // if we are not a machine gun, we want to wait until we have fired before updating to a new target point
                {
                    yield return new WaitUntil(() => shotFired);
                    shotFired = false;
                }
                else if (overrideTarget == null)// if we are a machine gun, just update the target point once every 3 seconds
                {
                    yield return new WaitForSeconds(3);
                }
            }
            
        }

        private float HowFarFromTarget()
        {
            if (AimingAtGround()) return Vector3.Distance(aimHit.point, targetPoint);
            return Mathf.Abs(aimHit.point.y - targetPoint.y);
        }

        protected virtual IEnumerator AimAtTarget(float refreshRate = 0.1f, bool everyFrame = false)
        {
            while (tokenActivated)
            {
                var trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position, gunScript.barrel.right * gunScript.muzzleVelocity, myProjectile.gravity, 150);
                aimHit = Trajectory.GetHitPoint(trajectoryPoints);

                if (DirectionToTargetBlocked() && overrideTarget == null) 
                {
                    targetPoint = gunScript.transform.position + gunScript.transform.right * 20f; // this will aim the weapon forward instead of at the target
                }
                
                bool hitPointIsRightOfTarget = aimHit.point.x > targetPoint.x;
                // if our projected hitpoint is past the tank we're fighting, we use the intersection between our projected aim and our target's Y axis to determine our aim
                if ((!myTankAI.TankIsRightOfTarget() && hitPointIsRightOfTarget) || (myTankAI.TankIsRightOfTarget() && !hitPointIsRightOfTarget) || AimingAtMyself() || aimHit.collider?.gameObject == null)
                {
                    if (!AimingAtMyself() && aimHit.collider?.gameObject != null) aimingPastOurTarget = true;
                    for (int i = 0; i < trajectoryPoints.Count - 1; i++)
                    {
                        Vector3 p1 = trajectoryPoints[i];
                        Vector3 p2 = trajectoryPoints[i + 1];

                        //checks if the line segment between p1 and p2 intersects with targetpoint's Y axis
                        if ((p1.x <= targetPoint.x && p2.x >= targetPoint.x) || (p1.x >= targetPoint.x && p2.x <= targetPoint.x))
                        {
                            // Calculate the intersection point
                            float t = (targetPoint.x - p1.x) / (p2.x - p1.x); // how far along the line segment the intersection is at. 0 means exactly the 1st point, 1 means the 2nd, .5 is between the two
                            Vector3 intersectionPoint = p1 + t * (p2 - p1); // p2 - p1 gives the direction, mult by t scales to correct length, added to p1 to get the exact coordinate
                            
                            aimHit.point = intersectionPoint;
                            break;
                        }
                    }
                }
                else
                {
                    aimingPastOurTarget = false;
                }
                
                var distFactor = Mathf.InverseLerp(0, overrideTarget ? 4 : 10, HowFarFromTarget()); // How close are we to our target? Returns 0 if we are right on it, returns 1 if we are 10 units away, will return a float for values inbetween
                var moveSpeed = Mathf.Lerp(minTurnSpeed, maxTurnSpeed, AimingCurve.Evaluate(distFactor));
                //if (AimingAtGround()) moveSpeed = maxTurnSpeed;
                
                // Determine if the hit point is above or below the projected point
                if (aimHit.point.y > targetPoint.y && !AimingAtGround()) // we want to aim downward.
                                                                         // the AimingAtGroundCheck assures that the
                {                                                        // weapon will aim up if the hit point is on the ground
                    currentForce = myTankAI.TankIsRightOfTarget() ? moveSpeed : -moveSpeed;
                }
                else //we want to aim upward
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

        public bool AimIsOverridden()
        {
            return overrideTarget != null;
        }

        protected virtual void OnDrawGizmos()
        {
            bool mortar = mySpecificType == INTERACTABLE.Mortar;
            Vector2 fireVelocity = (mortar ? gunScript.barrel.up : gunScript.barrel.right) * gunScript.muzzleVelocity;
            //if (mortar) fireVelocity += myTankAI.tank.treadSystem.r.velocity;
            var trajPoints = Trajectory.GetTrajectory(gunScript.barrel.position, 
                                                    fireVelocity,
                                                                   myProjectile.gravity,
                                                     100);
            if (!tokenActivated) return;
            for (int i = 0; i < trajPoints.Count - 1; i++)
            {
                if (Vector3.Distance(trajPoints[i], aimHit.point) < 1f) break; // stops projecting line at target
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