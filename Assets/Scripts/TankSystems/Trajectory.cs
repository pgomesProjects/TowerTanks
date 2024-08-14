using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    private const int N_TRAJECTORY_POINTS = 10; //The number of points to show on the line renderer

    public static List<Vector3> GetTrajectory(Vector3 start, Vector2 initialVelocity, float gravityScale)
    {
        List<Vector3> trajectoryPoints = new List<Vector3>(N_TRAJECTORY_POINTS);

        float g = Physics2D.gravity.magnitude * gravityScale;

        float velocity = initialVelocity.magnitude;
        float angle = Mathf.Atan2(initialVelocity.y, initialVelocity.x);

        float timeStep = 0.1f;
        float fTime = 0f;
        for (int i = 0; i < N_TRAJECTORY_POINTS; i++)
        {
            float dx = velocity * fTime * Mathf.Cos(angle);
            float dy = velocity * fTime * Mathf.Sin(angle) - (g * fTime * fTime / 2f);
            Vector3 pos = new Vector3(start.x + dx, start.y + dy, 0);
            trajectoryPoints.Add(pos);
            fTime += timeStep;
        }

        return trajectoryPoints;
    }
}
