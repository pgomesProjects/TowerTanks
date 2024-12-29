using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMortarBrain : WeaponBrain
    {
        private float minChargeTime = 3f;

        protected override void Update()
        {
            base.Update();
            Vector3 aimHitPoint = aimHit.point;
            var diff = aimHitPoint - targetPoint;
            
            bool diffIsPositive = diff.x >= 0;
            
            if (myTankAI.TankIsRightOfTarget() && gunScript.chargeTimer < minChargeTime && aimHitPoint.x - targetPoint.x > 2)
            {
                gunScript.ChargeMortar(diffIsPositive);
            }

            if (Vector3.Distance(aimHitPoint, targetPoint) > 8)
            {
                fireTimer = 0; //mortar wont fire if its way way off from hitting 
            }
        }
        protected override IEnumerator AimAtTarget(float refreshRate = .001f, bool everyFrame = true)
        {
            everyFrame = true;
            while (tokenActivated)
            {
                if (myTankAI.targetTank == null) yield break;
                
                var proj = gunScript.projectilePrefab.GetComponent<Projectile>();
                Vector2 fireVelocity = gunScript.barrel.up * gunScript.muzzleVelocity;
                fireVelocity += myTankAI.tank.treadSystem.r.velocity;
                var trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position,
                    fireVelocity, proj.gravity, 100);
                aimHit = Trajectory.GetHitPoint(trajectoryPoints);

                Vector3 aimHitPoint = aimHit.point;
                if (aimHitPoint == Vector3.zero) aimHitPoint = trajectoryPoints[^1];

                float HowFarFromTarget() => Mathf.Abs(aimHitPoint.x - targetPoint.x);

                maxTurnSpeed = .75f;
                var distFactor = Mathf.InverseLerp(0, 2, HowFarFromTarget());
                var moveSpeed = Mathf.Lerp(minTurnSpeed, maxTurnSpeed, distFactor);
                // Determine if the hit point is above or below the projected point
                if (aimHitPoint.x > targetPoint.x)
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

        protected override IEnumerator UpdateTargetPoint(float aimFactor)
        {
            while (enabled)
            {
                var leftMostCell = myTankAI.targetTank.leftMostCell.transform;
                var rightMostCell = myTankAI.targetTank.rightMostCell.transform;
                var targetTankTransform = myTankAI.targetTank.treadSystem.transform;
                // get random number between 0 and aimfactor
                float randomX = Random.Range(0, 100);
                bool hit = randomX <= aimFactor;
                
                if (hit)
                {
                    miss = false;
                    targetPoint = GetRandomPointBetweenVectors(leftMostCell.position + (Vector3.right * 2), rightMostCell.position + (Vector3.left * 2));
                }
                else
                {
                    miss = true;
                    var pointLeftOfTarget = leftMostCell.position - leftMostCell.right * 4f;
                    var pointRightTarget = rightMostCell.position + rightMostCell.right * 4f;
                    if (myTankAI.TankIsRightOfTarget())
                    {
                        targetPoint = GetRandomPointBetweenVectors(leftMostCell.position - leftMostCell.right * 2f, pointLeftOfTarget);
                    }
                    else
                    {
                        targetPoint = GetRandomPointBetweenVectors(rightMostCell.position + rightMostCell.right * 2f, pointRightTarget);
                    }
                }
                targetPointOffset = targetPoint - targetTankTransform.position;
                yield return new WaitForSeconds(3);
            }
        }
        
    }
}