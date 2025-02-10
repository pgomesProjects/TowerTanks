using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMortarBrain : WeaponBrain
    {
        private float HowFarFromTarget() => Mathf.Abs(aimHit.point.x - (overrideTarget == null ? leadTargetPoint.x : targetPoint.x));
        Vector3 leadTargetPoint;

        protected override void Update()
        {
            base.Update();
            if (HowFarFromTarget() > 15)
            {
                stopFiring = true; //mortar wont fire if its way way off from hitting 
            }
            else
            {
                stopFiring = false;
            }

            //leadTargetPoint += targetPointOffset;
        }

        protected override IEnumerator AimAtTarget(float refreshRate = .001f, bool everyFrame = true)
        {
            while (tokenActivated)
            {
                if (myTankAI.targetTank == null) yield break;
                
                var proj = gunScript.projectilePrefab.GetComponent<Projectile>();
                Vector2 fireVelocity = gunScript.barrel.up * gunScript.muzzleVelocity;
                //fireVelocity += myTankAI.tank.treadSystem.r.velocity;
                var trajectoryPoints = Trajectory.GetTrajectory(gunScript.barrel.position,
                    fireVelocity, proj.gravity, 100);
                aimHit = Trajectory.GetHitPoint(trajectoryPoints);
                

                Vector3 aimHitPoint = aimHit.point;
                if (aimHitPoint == Vector3.zero) aimHitPoint = trajectoryPoints[^1];
                
                float leadTime = Trajectory.CalculateTimeToHitGround(gunScript.barrel.up, gunScript.barrel.transform.position, gunScript.muzzleVelocity, proj.gravity, aimHit.point.y);
                
                Vector3 leadDisplacement = myTankAI.targetTank.treadSystem.r.velocity * leadTime;
                
                leadTargetPoint = targetPoint + leadDisplacement;
                
                var distFactor = Mathf.InverseLerp(0, 10, HowFarFromTarget());
                var moveSpeed = Mathf.Lerp(minTurnSpeed, maxTurnSpeed, distFactor);
                // Determine if the hit point is above or below the projected point
                if (aimHitPoint.x > (overrideTarget == null ? leadTargetPoint.x : targetPoint.x))
                {
                    currentForce = moveSpeed;
                }
                else
                {
                    currentForce = -moveSpeed;
                }
                
                if (!everyFrame) yield return new WaitForSeconds(refreshRate);
                 else yield return null;

            }
            //currentForce = diff.x >= 0 ? .5f : -.5f; //with the mortar, positive force is right, negative force is left
        }

        public override IEnumerator UpdateTargetPoint(float aimFactor)
        {
            while (enabled)
            {
                if (overrideTarget == null && myTankAI.targetTank != myTankAI.tank && myTankAI.targetTank != null)
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
                        targetPoint = GetRandomPointBetweenVectors(leftMostCell.position + Vector3.right * 2, rightMostCell.position + Vector3.left * 2);
                        targetPointOffset = targetPoint - targetTankTransform.position;
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

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.magenta;
            
            Gizmos.DrawWireSphere(leadTargetPoint, 1f);
            
        }
    }
}