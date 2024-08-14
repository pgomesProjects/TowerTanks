using System.Collections;
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

        float g = Physics2D.gravity.magnitude * gravityScale;

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
}
