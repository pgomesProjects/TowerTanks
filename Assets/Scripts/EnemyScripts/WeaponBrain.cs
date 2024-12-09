using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class WeaponBrain : InteractableBrain
    {
        internal GunController gunScript;
        
        public float fireCooldown;
        internal float fireTimer;
        public float cooldownOffset;
        
        public bool isRotating;
        internal float currentForce;
        internal List<Vector3> trajectoryPoints;
        internal RaycastHit2D aimHit;

        private void Awake()
        {
            gunScript = GetComponent<GunController>();
        }

        protected void Start()
        {
            fireTimer = 0;
        }

        // Update is called once per frame
        protected void Update()
        {
            AimAtTarget();
            gunScript.RotateBarrel(currentForce, false);
            
            if (fireTimer < fireCooldown) fireTimer += Time.deltaTime;
            else if (!AimingAtMyself())
            {
                gunScript.Fire(true, gunScript.tank.tankType);
                float randomOffset = Random.Range(-cooldownOffset, cooldownOffset);
                fireTimer = 0 + randomOffset;
            }
            else
            {
                
            }
            
        }

        private bool AimingAtMyself()
        {
            if (aimHit.collider != null && aimHit.collider.transform.IsChildOf(gunScript.tank.transform))
            {
                return true;
            }
            return false;
        }

        [Button]
        public void AimingAt()
        {
            Debug.Log($"I am aiming at: {aimHit.collider.transform.name}");
            if (AimingAtMyself()) Debug.Log("I am aiming at myself!");
        }
        
        public virtual void AimAtTarget()
        {
            if (myTankAI.targetTank == null) return;

            var proj = gunScript.projectilePrefab.GetComponent<Projectile>();

            trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position, gunScript.barrel.right * gunScript.muzzleVelocity, proj.gravity, 100);
            aimHit = Trajectory.GetHitPoint(trajectoryPoints);

            Vector3 tankPosition = myTankAI.tank.treadSystem.transform.position + Vector3.up * 2.5f;
            Vector3 targetPosition = myTankAI.targetTank.treadSystem.transform.position + Vector3.up * 2.5f;
            
            bool hitPointIsRightOfTarget = aimHit.point.x > targetPosition.x;

            // if our projected hitpoint is past the tank we're fighting, the hitpoint is set right in front of the barrel, because in that scenario we want to aim based on our gun's general direction and not our hitpoint (this doesnt apply to mortars)
            if ((!myTankAI.TankIsRightOfTarget() && hitPointIsRightOfTarget) || (myTankAI.TankIsRightOfTarget() && !hitPointIsRightOfTarget) || aimHit.collider == null || AimingAtMyself())
            {
                aimHit.point = trajectoryPoints[2];
            }

            Vector3 direction = targetPosition - tankPosition;

            // Project the hit point onto the direction vector
            Vector3 aimHitPoint = aimHit.point; //converts to vec3 (using vec3 for project function)
            Vector3 projectedPoint = tankPosition + Vector3.Project(aimHitPoint - tankPosition, direction);

            // Determine if the hit point is above or below the projected point
            if (aimHit.point.y > projectedPoint.y)
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
                if (Vector3.Distance(trajectoryPoints[i], aimHit.point) < 0.1f) break; // stops projecting line at target
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(aimHit.point, 0.5f);
            
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(myTankAI.targetTank.treadSystem.transform.position + Vector3.up * 2.5f, 1f);
            
        }
    }
}