using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMortarBrain : WeaponBrain
    {
        public override void AimAtTarget()
        {
            if (myTankAI.targetTank == null) return;

            var proj = gunScript.projectilePrefab.GetComponent<Projectile>();

            trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position, gunScript.barrel.up * gunScript.muzzleVelocity, proj.gravity, 100);
            aimHit = Trajectory.GetHitPoint(trajectoryPoints);
            
            Vector3 aimHitPoint = aimHit.point;
            if (aimHitPoint == Vector3.zero) return;
            
            //Adding Vector3.up * 2.5f because the tread system's transform is a bit low, we want to aim a little above that
            Vector3 target = myTankAI.targetTank.treadSystem.transform.position;
            
            
            Vector3 diff = aimHitPoint - target;
            
            bool tankIsRightOfTarget = myTankAI.TankIsRightOfTarget();
            bool diffIsPositive = diff.x >= 0;

            if (tankIsRightOfTarget)
            {
                gunScript.ChargeMortar(diffIsPositive);
            }
            else
            {
                gunScript.ChargeMortar(!diffIsPositive);
            }
            
            currentForce = diffIsPositive ? .75f : -.75f;
            
            //currentForce = diff.x >= 0 ? .5f : -.5f; //with the mortar, positive force is right, negative force is left
        }
        
    }
}