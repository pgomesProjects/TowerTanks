using System.Collections.Generic;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    /// <summary>
    /// Sends a list of trajectory points based on the information given.
    /// </summary>
    /// <param name="start">The starting position for the trajectory.</param>
    /// <param name="initialVelocity">The initial velocity of the projectile.</param>
    /// <param name="gravityScale">The gravity scale of the project.</param>
    /// <param name="trajectoryPoints">The number of trajectory points to get.</param>
    /// <returns>Returns a list of Vector3 points that illustrate the trajectory of a projectile.</returns>
    public static List<Vector3> GetTrajectory(Vector3 start, Vector2 initialVelocity, float gravityScale, int trajectoryPoints = 10)
    {
        List<Vector3> listOfTrajectoryPoints = new List<Vector3>(trajectoryPoints);

        float g = gravityScale;
        //Physics2D.gravity.magnitude * gravityScale <- previous calculation

        float velocity = initialVelocity.magnitude;
        float angle = Mathf.Atan2(initialVelocity.y, initialVelocity.x);

        float timeStep = 0.1f;
        float fTime = 0f;
        for (int i = 0; i < trajectoryPoints; i++)
        {
            float dx = velocity * fTime * Mathf.Cos(angle);
            float dy = velocity * fTime * Mathf.Sin(angle) - (g * fTime * fTime / 2f);
            Vector3 pos = new Vector3(start.x + dx, start.y + dy, 0);
            listOfTrajectoryPoints.Add(pos);
            fTime += timeStep;
        }

        return listOfTrajectoryPoints;
    }
    
    public static RaycastHit2D GetHitPoint(List<Vector3> trajectoryPoints)
    {
        var includeLayer = (1 << LayerMask.NameToLayer("Ground")) |
                           (1 << LayerMask.NameToLayer("Player"));

        for (int i = 0; i < trajectoryPoints.Count - 2; i++)
        {
            Vector3 start = trajectoryPoints[i];
            Vector3 end = trajectoryPoints[i + 1];
            RaycastHit2D hit = Physics2D.Raycast(start, end - start, Vector3.Distance(start, end), includeLayer);
            if (hit.collider != null)
            {
                return hit;
            }
        }

        return new RaycastHit2D();
    }
    /// <summary>
    ///  Calculates the time it will take for a projectile to hit the ground.
    /// </summary>
    /// <param name="dir">The local direction this projectile is being launched</param>
    /// <param name="pos">The position from where the projectile is being launched from</param>
    /// <param name="velocity">The starting velocity of the projectile</param>
    /// <param name="gravity">Projectile's gravity value</param>
    /// <param name="targetHeight">The Y height of the projectile's projected hit point</param>
    /// <returns></returns>
    public static float CalculateTimeToHitGround(Vector3 dir, Vector3 pos, float velocity, float gravity, float targetHeight)
    { 
        float initialVerticalVelocity = dir.y * velocity;
        float initialHeight = pos.y;

        if (initialVerticalVelocity > 0)
        {
            // Time to reach the highest point
            float timeToHighestPoint = initialVerticalVelocity / gravity;
            float highestPoint = initialHeight + (initialVerticalVelocity * timeToHighestPoint) - (0.5f * gravity * Mathf.Pow(timeToHighestPoint, 2));
            float heightDifference = highestPoint - targetHeight;

            // Time to fall from the highest point to the target height
            float timeToFall = Mathf.Sqrt(2 * heightDifference / gravity);
            return timeToHighestPoint + timeToFall;
        }
        else
        {
            // Time to hit the target height directly
            float heightDifference = initialHeight - targetHeight;
            return Mathf.Sqrt(2 * heightDifference / gravity);
        }
    }
}
