using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [ExecuteInEditMode]
    public class SpriteOutliner : MonoBehaviour
    {
        [SerializeField, Tooltip("The width of the outline.")] private float outlineWidth;
        [SerializeField, Tooltip("The smoothness of the outline.")] private int smoothness;
        [SerializeField, Tooltip("The color of the outline.")] private Color outlineColor;
        [SerializeField, Tooltip("The material for the outline.")] private Material outlineMat;


        private List<LineRenderer> currentOutlines = new List<LineRenderer>();

        [Button]
        public void DebugGenerateOutline()
        {
            ClearOutline();

            //Generate an outline for all SpriteRenderers childed to the object
            foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
                GenerateOutline(spriteRenderer, smoothness);
        }

        [Button]
        public void DebugRemoveOutlines()
        {
            ClearOutline();
        }

        /// <summary>
        /// Generates a LineRenderer that circles around a SpriteRendere.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer.</param>
        /// <param name="numPointsBetween">The number of points between each edge (higher numbers used for smoothness).</param>
        public void GenerateOutline(SpriteRenderer spriteRenderer, int numPointsBetween = 0)
        {
            //If the SpriteRenderer or the sprite is null, return
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            //Clamp the value of the points
            numPointsBetween = Mathf.Max(0, numPointsBetween);

            //Get the vertices from the sprite
            Vector2[] spriteVerts = spriteRenderer.sprite.vertices;
            Vector3[] vertices = new Vector3[spriteVerts.Length];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = spriteVerts[i];

            //Get the triangles from the sprite
            ushort[] spriteTris = spriteRenderer.sprite.triangles;
            int[] triangles = new int[spriteTris.Length];
            for (int i = 0; i < triangles.Length; i++)
                triangles[i] = spriteTris[i];

            //Get the outer edges of the shape
            Dictionary<string, KeyValuePair<int, int>> edges = new Dictionary<string, KeyValuePair<int, int>>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                //Check each vertex of the triangle
                for (int j = 0; j < 3; j++)
                {
                    int vertexOne = triangles[i + j];
                    int vertexTwo = triangles[i + (j + 1) % 3];
                    string edge = Mathf.Min(vertexOne, vertexTwo) + ":" + Mathf.Max(vertexOne, vertexTwo);

                    //If the edge already exists, remove it
                    if (edges.ContainsKey(edge))
                        edges.Remove(edge);
                    else
                        edges.Add(edge, new KeyValuePair<int, int>(vertexOne, vertexTwo));
                }
            }

            //Create a dictionary with all unique vertices
            Dictionary<int, int> allVertices = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> edge in edges.Values)
            {
                if (!allVertices.ContainsKey(edge.Key))
                    allVertices.Add(edge.Key, edge.Value);
            }

            // Create line renderer
            LineRenderer currentOutline = new GameObject("Line").AddComponent<LineRenderer>();

            //Reset position
            currentOutline.transform.parent = spriteRenderer.transform;
            currentOutline.transform.localPosition = Vector3.zero;
            currentOutline.transform.localEulerAngles = Vector3.zero;
            currentOutline.transform.localScale = Vector3.one;
            currentOutline.useWorldSpace = false;

            //Add material, width, and color
            currentOutline.material = outlineMat;
            currentOutline.startWidth = currentOutline.endWidth = outlineWidth;
            currentOutline.startColor = currentOutline.endColor = outlineColor;

            //Draw behind the shape
            currentOutline.sortingOrder = -1;

            //Add to the list of current outlines for the object
            currentOutlines.Add(currentOutline);

            // Loop through edge vertices in order
            int startingVertex = 0;
            int currentVertex = startingVertex;

            List<Vector3> outlinePositions = new List<Vector3>();

            while (true)
            {
                // Add current vertex position
                outlinePositions.Add(vertices[currentVertex]); // Adjust with position

                int nextVertex = allVertices[currentVertex];


                //Add some points between the current vertex and the next, equally spaced from each other to make it smoother
                Vector3 step = (vertices[nextVertex] - vertices[currentVertex]) / (numPointsBetween + 1);
                for (int i = 1; i <= numPointsBetween; i++)
                    outlinePositions.Add(vertices[currentVertex] + step * i);

                // Get next vertex
                currentVertex = nextVertex;

                //Close the outline and break the loop if reaching the end
                if (currentVertex == startingVertex)
                {
                    outlinePositions.Add(vertices[currentVertex]);
                    break;
                }
            }

            //Take all of the points and set them on the line renderer
            currentOutline.positionCount = outlinePositions.Count;
            for (int i = 0; i < outlinePositions.Count; i++)
                currentOutline.SetPosition(i, outlinePositions[i]);
        }

        /// <summary>
        /// Clears all of the outlines created.
        /// </summary>
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
