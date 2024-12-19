using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMortarBrain : WeaponBrain
    {
        private float minChargeTime = 5f;
        
        

        protected override void Update()
        {
            base.Update();
            Vector3 aimHitPoint = aimHit.point;
            var diff = aimHitPoint - myTankAI.targetTank.treadSystem.transform.position;
            
            bool diffIsPositive = diff.x >= 0;
            
            if (myTankAI.TankIsRightOfTarget() && gunScript.chargeTimer < minChargeTime)
            {
                gunScript.ChargeMortar(diffIsPositive);
            }
        }
        public override IEnumerator AimAtTarget(float refreshRate = .001f, bool everyFrame = true)
        {
            everyFrame = true;
            while (tokenActivated)
            {
                if (myTankAI.targetTank == null) yield break;
                
                var proj = gunScript.projectilePrefab.GetComponent<Projectile>();
                Vector2 fireVelocity = gunScript.barrel.up * gunScript.muzzleVelocity;
                fireVelocity += myTankAI.tank.treadSystem.r.GetPointVelocity(gunScript.barrel.position);
                var trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position,
                    fireVelocity, proj.gravity, 100);
                aimHit = Trajectory.GetHitPoint(trajectoryPoints);

                Vector3 aimHitPoint = aimHit.point;
                if (aimHitPoint == Vector3.zero) aimHitPoint = trajectoryPoints[^1];

                //Adding Vector3.up * 2.5f because the tread system's transform is a bit low, we want to aim a little above that
                Vector3 target = myTankAI.targetTank.treadSystem.transform.position;

                float HowFarFromTarget() => Vector3.Distance(aimHitPoint, target);

                maxTurnSpeed = .75f;
                var distFactor = Mathf.InverseLerp(0, 2, HowFarFromTarget());
                var moveSpeed = Mathf.Lerp(minTurnSpeed, maxTurnSpeed, distFactor);
                // Determine if the hit point is above or below the projected point
                if (aimHitPoint.x > target.x)
                {
                    currentForce = myTankAI.TankIsRightOfTarget() ? moveSpeed : -moveSpeed;
                }
                else
                {
                    currentForce = myTankAI.TankIsRightOfTarget() ? -moveSpeed : moveSpeed;
                }
                
                if (!everyFrame) yield return new WaitForSeconds(refreshRate);
                 else yield return null;

            }
            //currentForce = diff.x >= 0 ? .5f : -.5f; //with the mortar, positive force is right, negative force is left
        }
        
    }
}