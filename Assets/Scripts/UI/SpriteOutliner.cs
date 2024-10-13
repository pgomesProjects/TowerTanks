using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [ExecuteInEditMode]
    public class SpriteOutliner : MonoBehaviour
    {
        [SerializeField, Tooltip("The width of the outline.")] private float outlineWidth;
        [SerializeField, Tooltip("The color of the outline.")] private Color outlineColor;
        [SerializeField, Tooltip("The material for the outline.")] private Material outlineMat;


        private List<LineRenderer> currentOutlines = new List<LineRenderer>();

        [Button]
        public void DebugGenerateOutline()
        {
            ClearOutline();
            foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
                GenerateOutline(spriteRenderer);
        }

        [Button]
        public void DebugRemoveOutlines()
        {
            ClearOutline();
        }

        public void GenerateOutline(SpriteRenderer spriteRenderer, int numPointsBetween = 20)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            // Get the vertices and triangles from the sprite
            Vector2[] spriteVerts = spriteRenderer.sprite.vertices;
            Vector3[] vertices = new Vector3[spriteVerts.Length];

            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = (Vector3)(spriteVerts[i] * spriteRenderer.transform.localScale);

            ushort[] spriteTris = spriteRenderer.sprite.triangles;
            int[] triangles = new int[spriteTris.Length];

            for (int i = 0; i < triangles.Length; i++)
                triangles[i] = spriteTris[i];

            // Extract the outer edges from the sprite's triangles (ignoring shared edges)
            Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int vert1 = triangles[i + e];
                    int vert2 = triangles[i + (e + 1) % 3]; // Wrap around to form a triangle
                    string edge = Mathf.Min(vert1, vert2) + ":" + Mathf.Max(vert1, vert2);
                    if (edges.ContainsKey(edge))
                    {
                        edges.Remove(edge);
                    }
                    else
                    {
                        edges.Add(edge, new KeyValuePair<int, int>(vert1, vert2));
                    }
                }
            }

            // Create edge lookup Dictionary
            Dictionary<int, int> lookup = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> edge in edges.Values)
            {
                if (!lookup.ContainsKey(edge.Key))
                {
                    lookup.Add(edge.Key, edge.Value);
                }
            }

            // Create line prefab
            LineRenderer currentOutline = new GameObject("Line").AddComponent<LineRenderer>();
            currentOutline.useWorldSpace = false;
            currentOutline.transform.parent = spriteRenderer.transform;
            currentOutline.material = outlineMat;
            currentOutline.startWidth = currentOutline.endWidth = outlineWidth;
            currentOutline.startColor = currentOutline.endColor = outlineColor;
            currentOutline.sortingOrder = -1;
            currentOutline.loop = true;
            currentOutlines.Add(currentOutline);

            // Loop through edge vertices in order
            int startVert = 0;
            int nextVert = startVert;
            int highestVert = startVert;

            currentOutline.positionCount = 0; // Initialize position count

            // This will store the outline positions including interpolated points
            List<Vector3> outlinePositions = new List<Vector3>();

            while (true)
            {
                // Add current vertex position
                outlinePositions.Add(vertices[nextVert] + spriteRenderer.transform.position); // Adjust with position

                // Interpolate between the current and next vertex
                int nextVertLookup = lookup[nextVert];
                for (int i = 1; i <= numPointsBetween; i++)
                {
                    // Calculate the interpolated position
                    Vector3 interpolatedPos = Vector3.Lerp(vertices[nextVert], vertices[nextVertLookup], (float)i / (numPointsBetween + 1));
                    outlinePositions.Add(interpolatedPos + spriteRenderer.transform.position); // Adjust with position
                }

                // Get next vertex
                nextVert = nextVertLookup;

                // Store highest vertex
                if (nextVert > highestVert)
                {
                    highestVert = nextVert;
                }

                // Shape complete
                if (nextVert == startVert)
                {
                    // Close the shape by adding the starting position again
                    outlinePositions.Add(vertices[nextVert] + spriteRenderer.transform.position); // Adjust with position
                    break;
                }
            }

            // Convert Euler angles to Quaternion
            Quaternion rotation = Quaternion.Euler(spriteRenderer.transform.eulerAngles);

            Vector3[] finalPoints = new Vector3[outlinePositions.Count];

            for (int i = 0; i < outlinePositions.Count; i++)
            {
                // Translate the point to the origin based on the center point
                Vector3 translatedPoint = outlinePositions[i] - spriteRenderer.transform.position;

                // Apply the rotation
                Vector3 rotatedPoint = rotation * translatedPoint;

                // Translate the point back to its original position
                finalPoints[i] = rotatedPoint + spriteRenderer.transform.position;
            }

            // Set the final positions in the LineRenderer
            currentOutline.positionCount = finalPoints.Length;
            for (int i = 0; i < finalPoints.Length; i++)
            {
                currentOutline.SetPosition(i, finalPoints[i]);
            }
        }


        private void ClearOutline()
        {
            foreach(LineRenderer line in currentOutlines)
            {
                if (line != null)
                    DestroyImmediate(line.gameObject);
            }

            currentOutlines.Clear();
        }
    }
}
