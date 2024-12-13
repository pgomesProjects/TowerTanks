using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class AOERenderer : MonoBehaviour
    {
        public LineRenderer circleRenderer;

        public void UpdateAOE(int segments, float radius)
        {
            circleRenderer.positionCount = segments;

            for (int i = 0; i < segments; i++)
            {
                float circumferenceSegment = (float)i / segments;

                float radian = circumferenceSegment * 2 * Mathf.PI;

                float xScale = Mathf.Cos(radian);
                float yScale = Mathf.Sin(radian);

                float x = xScale * radius;
                float y = yScale * radius;

                Vector3 currentPos = new Vector3(x, y, 0);

                circleRenderer.SetPosition(i, currentPos);
            }
        }
    }
}
