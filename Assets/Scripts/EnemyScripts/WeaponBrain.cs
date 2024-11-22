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
        
        public virtual void AimAtTarget() //mortar overrides bc it uses slightly different aiming
        {
            if (myTankAI.targetTank == null) return;

            var proj = gunScript.projectilePrefab.GetComponent<Projectile>();

            trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position, gunScript.barrel.right * gunScript.muzzleVelocity, proj.gravity, 100);
            hitPoint = Trajectory.GetHitPoint(trajectoryPoints);
            
            //Adding Vector3.up * 2.5f because the tread system's transform is a bit low, we want to aim a little above that
            Vector3 target = myTankAI.targetTank.treadSystem.transform.position + Vector3.up * 2.5f;
            
            Vector3 diff = hitPoint - target;
            
            
            currentForce = diff.y > 0 ? 1 : -1;
        }

        private void OnDrawGizmos()
        {
            if (trajectoryPoints == null) return;
            for (int i = 0; i < trajectoryPoints.Count - 1; i++)
            {
                Gizmos.color = Color.red;
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