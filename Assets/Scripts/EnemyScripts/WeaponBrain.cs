using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class WeaponBrain : InteractableBrain
    {
        internal GunController gunScript;
        
        public float fireCooldown;
        internal float fireTimer;
        public float aimCooldown;
        internal float aimTimer;
        public float cooldownOffset;
        
        public bool isRotating;
        internal float currentForce;
        internal List<Vector3> trajectoryPoints;
        internal Vector3 hitPoint;

        private void Awake()
        {
            gunScript = GetComponent<GunController>();
        }

        protected void Start()
        {
            fireTimer = 0;
            aimTimer = 0;
        }

        // Update is called once per frame
        protected void Update()
        {
            if (fireTimer < fireCooldown) fireTimer += Time.deltaTime;
            else
            {
                gunScript.Fire(true, gunScript.tank.tankType);
                float randomOffset = Random.Range(-cooldownOffset, cooldownOffset);
                fireTimer = 0 + randomOffset;
            }

            if (aimTimer < aimCooldown) aimTimer += Time.deltaTime;
            else
            {
                float randomForce = Random.Range(-1.2f, 1.2f);
                //StartCoroutine(AimCannon(randomForce));

                float randomOffset = Random.Range(-2f, 2f);
                aimTimer = 0 + randomOffset;
            }
            AimAtTarget();
            gunScript.RotateBarrel(currentForce, false);
            
        }
        
        public virtual void AimAtTarget()
        {
            if (myTankAI.targetTank == null) return;

            var proj = gunScript.projectilePrefab.GetComponent<Projectile>();

            trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position, gunScript.barrel.right * gunScript.muzzleVelocity, proj.gravity, 100);
            RaycastHit2D hit = Trajectory.GetHitPoint(trajectoryPoints);

            hitPoint = hit.point;

            Vector3 tankPosition = myTankAI.tank.treadSystem.transform.position + Vector3.up * 2.5f;
            Vector3 targetPosition = myTankAI.targetTank.treadSystem.transform.position + Vector3.up * 2.5f;
            
            bool hitPointIsRightOfTarget = hitPoint.x > targetPosition.x;

            // if our projected hitpoint is past the tank we're fighting, the hitpoint is set right in front of the barrel, because in that scenario we want to aim based on our gun's general direction and not our hitpoint
            if ((!myTankAI.TankIsRightOfTarget() && hitPointIsRightOfTarget) || (myTankAI.TankIsRightOfTarget() && !hitPointIsRightOfTarget) || hit.collider == null)
            {
                hitPoint = trajectoryPoints[2];
            }

            Vector3 direction = targetPosition - tankPosition;

            // Project the hit point onto the direction vector
            Vector3 projectedPoint = tankPosition + Vector3.Project(hitPoint - tankPosition, direction);

            // Determine if the hit point is above or below the projected point
            if (hitPoint.y > projectedPoint.y)
            {
                currentForce = myTankAI.TankIsRightOfTarget() ? 1 : -1;
            }
            else
            {
                currentForce = myTankAI.TankIsRightOfTarget() ? -1 : 1;
            }

            gunScript.RotateBarrel(currentForce, false);
        }

        private void OnDrawGizmos()
        {
            if (trajectoryPoints == null) return;
            if (!tokenActivated) return;
            for (int i = 0; i < trajectoryPoints.Count - 1; i++)
            {
                Gizmos.color = myTankAI.TankIsRightOfTarget() ? Color.red : Color.blue;
                Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
                if (trajectoryPoints[i] == hitPoint) break; // stops projecting line at target
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hitPoint, 0.5f);
            
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(myTankAI.targetTank.treadSystem.transform.position + Vector3.up * 2.5f, 1f);
            
        }
    }
}