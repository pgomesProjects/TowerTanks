using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCannonController : CannonController
{
    [SerializeField, Tooltip("The minimum and maximum number of seconds to wait for the enemy cannon to fire.")] private int secondsToFireMinimum = 7, secondsToFireMaximum = 12;
    [SerializeField, Tooltip("The radius that the enemy cannon has for any targets it wants to hit.")] private float targetRadius = 5f;

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
        Debug.DrawCircle(target, targetRadius, 20, Color.red);
        CheckForTargetInTrajectory(target, targetRadius);

        // Get angle in Radians
        float cannonAngleRad = Mathf.Atan2(target.y - cannonPivot.transform.position.y, target.x - cannonPivot.transform.position.x);
        // Get angle in Degrees
        float cannonAngleDeg = (180 / Mathf.PI) * cannonAngleRad;

        switch (currentCannonDirection)
        {
            //Flip the cannon if facing left
            case CANNONDIRECTION.LEFT:
                cannonAngleDeg += 180;
                break;
        }

        //Debug.Log("Cannon Degree: " + cannonAngleDeg);

        Quaternion lookAngle = Quaternion.Euler(0, 0, cannonAngleDeg);
        Quaternion currentAngle = Quaternion.Slerp(cannonPivot.transform.rotation, lookAngle, Time.deltaTime);

        //Clamp the angle of the cannon
        currentAngle.z = Mathf.Clamp(currentAngle.z, lowerAngleBound, upperAngleBound);

        // Rotate Object
        cannonPivot.transform.rotation = currentAngle;
        cannonRotation = cannonPivot.eulerAngles;

        //Debug.Log("Current Angle: " + cannonRotation);
    }
}
