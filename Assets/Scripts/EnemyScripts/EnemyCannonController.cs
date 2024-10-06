using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
    public class EnemyCannonController : CannonController
    {
        [SerializeField, Tooltip("The minimum and maximum number of seconds to wait for the enemy cannon to fire.")] private int secondsToFireMinimum = 7, secondsToFireMaximum = 12;
        [SerializeField, Tooltip("The radius that the enemy cannon has for any targets it wants to hit.")] private float targetRadius = 5f;
        [SerializeField, Tooltip("The amount of randomness in each direction that is given to the target position.")] private Vector2 enemyCannonTargetRange;
        [SerializeField, Tooltip("The amount of time to refresh the target's aim (in seconds).")] private float aimTargetRefreshRate;
        [SerializeField, Tooltip("The amount of time to refresh the target's random position (in seconds).")] private float randomTargetRefreshRate = 3f;

        private float currentAimRefreshTimer;  //The time since the last aim cannon refresh
        private float currentRandomRefreshTimer;  //The time since the last random target refresh
        private Vector2 currentOffset; //The current offset of the target position

        private bool canHitTarget;  //If true, the cannon can hit its target. If false, the cannon cannot hit its target.
        private bool readyForAimUpdate; //If true, update the target angle. If false, do not update the target angle
        private float targetAngle;  //The desired angle of the enemy cannon

        /// <summary>
        /// Fires the enemy cannon at a random interval.
        /// </summary>
        /// <returns></returns>
        public IEnumerator FireAtDelay()
        {
            while (true)
            {
                //Debug.Log("Starting Fire Wait...");
                int timeWait = Random.Range(secondsToFireMinimum, secondsToFireMaximum);
                yield return new WaitForSeconds(timeWait);

                Debug.Log("Enemy Fire!");
                Fire();
            }
        }

        /// <summary>
        /// Aims the cannon at a desired target.
        /// </summary>
        /// <param name="target">The target to aim at.</param>
        public void AimAtTarget(Vector3 target)
        {
            //Randomize the target position a bit
            target.x += currentOffset.x;
            target.y += currentOffset.y;

            Debug.DrawCircle(target, targetRadius, 20, Color.red);

            //If the refresh for the aim is cooled off, update the target angle
            if (readyForAimUpdate)
            {
                canHitTarget = CheckForTargetInTrajectory(target, targetRadius);

                //If there is no target available but the target is in range
                if (!canHitTarget)
                {
                    targetAngle = upperAngleBound;

                    if (closestPointToTarget.y < target.y - targetRadius)
                        targetAngle = upperAngleBound;
                    else if (closestPointToTarget.y > target.y + targetRadius)
                        targetAngle = lowerAngleBound;
                }

                readyForAimUpdate = false;  //Set to false so that it does not call repeatedly
            }
            else
            {
                CheckForTargetInTrajectory(target, targetRadius);   //Show the debug line for the cannon only
            }
        }

        private void Update()
        {
            //Timers for refresh rates
            AimTargetRefresh();
            RandomPositionRefresh();

            MoveCannonToTargetAngle();
        }

        /// <summary>
        /// Updates the target angle after a defined amount of time.
        /// </summary>
        private void AimTargetRefresh()
        {
            if (currentAimRefreshTimer < 0)
            {
                currentAimRefreshTimer = aimTargetRefreshRate;
                readyForAimUpdate = true;
            }
            else
                currentAimRefreshTimer -= Time.deltaTime;
        }

        /// <summary>
        /// Randomizes the target position after a defined amount of time.
        /// </summary>
        private void RandomPositionRefresh()
        {
            if (currentRandomRefreshTimer < 0)
            {
                currentRandomRefreshTimer = randomTargetRefreshRate;
                RandomizeTarget();
            }
            else
                currentRandomRefreshTimer -= Time.deltaTime;
        }

        /// <summary>
        /// Angles the cannon to the desired target angle.
        /// </summary>
        private void MoveCannonToTargetAngle()
        {
            //Move the cannon if they cannot hit their target
            if (!canHitTarget)
            {
                //Debug.Log("Cannon Degree: " + targetAngle);
                Quaternion lookAngle = Quaternion.Euler(0, 0, targetAngle);
                //Debug.Log("Look Angle: " + lookAngle);
                Quaternion currentAngle = Quaternion.Slerp(cannonPivot.transform.rotation, lookAngle, cannonSpeed * Time.deltaTime);

                //Clamp the angle of the cannon
                if (lowerAngleBound > upperAngleBound)
                    currentAngle.z = Mathf.Clamp(currentAngle.z, upperAngleBound, lowerAngleBound);
                else
                    currentAngle.z = Mathf.Clamp(currentAngle.z, lowerAngleBound, upperAngleBound);

                //Debug.Log("Current Angle: " + currentAngle);

                cannonPivot.transform.rotation = currentAngle;
                cannonRotation = cannonPivot.eulerAngles;

                //Debug.Log("Current Angle: " + cannonRotation);
            }
        }

        /// <summary>
        /// Generates a random offset so that the enemy has imperfect precision.
        /// </summary>
        private void RandomizeTarget()
        {
            currentOffset.x = Random.Range(-enemyCannonTargetRange.x, enemyCannonTargetRange.x);
            currentOffset.y = Random.Range(-enemyCannonTargetRange.y, enemyCannonTargetRange.y);
        }
    }
}
