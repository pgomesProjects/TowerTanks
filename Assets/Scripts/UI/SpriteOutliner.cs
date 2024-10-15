using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    [ExecuteInEditMode]
    public class SpriteOutliner : MonoBehaviour
    {
        [SerializeField, Tooltip("Enables / Disables the outline.")] private bool isVisible = false;
        [SerializeField, Tooltip("The width of the outline.")] private float outlineWidth;
        [SerializeField, Tooltip("The color of the outline.")] private Color outlineColor;
        [SerializeField, Tooltip("The material for the outline.")] private Material outlineMat;
        [SerializeField, Tooltip("The sorting order for the outline.")] private int sortingOrder = -1;
        [SerializeField, Tooltip("The scale multiplier for the LineRenderer.")] private float scaleMultiplier = 1f;
        [SerializeField, Tooltip("The smoothness of the lines.")] private int smoothness;

        private List<GameObject> currentOutlines = new List<GameObject>();
        private Transform outlineContainer;
        private Vector3 center;

        private bool currentVisibility;

        [Button("Generate Outline")]
        public void DebugGenerateOutline()
        {
            ClearOutline();
            center = CalculateBounds().center;
            currentVisibility = isVisible;
            outlineContainer = new GameObject("Outlines").GetComponent<Transform>();
            outlineContainer.transform.SetParent(transform.parent);

            if (isVisible)
            {
                //Generate an outline for all SpriteRenderers childed to the object
                foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
                    GenerateOutline(spriteRenderer);
            }
        }

        private void OnValidate()
        {
            //DebugGenerateOutline();
        }

        [Button("Remove Outline")]
        public void DebugRemoveOutlines()
        {
            ClearOutline();
        }

        private Bounds CalculateBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

            //If there are no renderers in the object, return null
            if (renderers.Length == 0)
                return new Bounds();

            Bounds bounds = renderers[0].bounds;

            foreach (var renderer in renderers)
            {
                //Increases the bounds of the main renderer to include all renderers in the GameObject
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Generates a LineRenderer that circles around a SpriteRendere.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer.</param>
        public void GenerateOutline(SpriteRenderer spriteRenderer, int numPointsBetween = 0)
        {
            //If the SpriteRenderer or the sprite is null, return
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            //Clamp the number of points
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
                    break;
            }

            AddStroke(spriteRenderer, outlinePositions, outlineWidth);
            //CreateLineRenderer(spriteRenderer, outlinePositions);
        }

        /// <summary>
        /// Creates an outline of a SpriteRenderer using a LineRenderer.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer to make the outline for.</param>
        /// <param name="outlinePositions">The points that create the outline.</param>
        private void CreateLineRenderer(SpriteRenderer spriteRenderer, List<Vector3> outlinePositions)
        {
            // Create line renderer
            LineRenderer currentOutline = new GameObject("Line").AddComponent<LineRenderer>();

            //Reset position
            currentOutline.transform.parent = spriteRenderer.transform;
            currentOutline.transform.localPosition = Vector3.zero;
            currentOutline.transform.localEulerAngles = Vector3.zero;
            currentOutline.transform.localScale = Vector3.one;
            currentOutline.useWorldSpace = false;
            currentOutlines.Add(currentOutline.gameObject);

            //Take all of the points and set them on the line renderer
            currentOutline.positionCount = outlinePositions.Count;
            for (int i = 0; i < outlinePositions.Count; i++)
                currentOutline.SetPosition(i, outlinePositions[i]);

            AdjustOutlineColor(currentOutline.gameObject);
            AdjustOutlineThickness(currentOutline.gameObject);
        }

        /// <summary>
        /// Adjusts the color of the outline.
        /// </summary>
        /// <param name="outline">The current outline GameObject.</param>
        private void AdjustOutlineColor(GameObject outline)
        {
            //Adjust the material, color, and sorting order
            LineRenderer outlineLineRenderer;
            if(outline.TryGetComponent(out outlineLineRenderer))
            {
                outlineLineRenderer.material = outlineMat;
                outlineLineRenderer.startColor = outlineLineRenderer.endColor = outlineColor;
                outlineLineRenderer.sortingOrder = sortingOrder;
            }
        }

        /// <summary>
        /// Adjust the thickness of the outline.
        /// </summary>
        /// <param name="outline">The current outline GameObject.</param>
        private void AdjustOutlineThickness(GameObject outline)
        {
            outline.transform.localPosition = Vector3.zero;

            //Adjust the position and scale based on the multiplier
            Vector3 directionFromCenter = outline.transform.position - center;
            Vector3 scaledPosition = center + directionFromCenter * scaleMultiplier;
            outline.transform.position = scaledPosition;
            outline.transform.localScale = Vector3.one * scaleMultiplier;
        }

        private void AddStroke(SpriteRenderer spriteRenderer, List<Vector3> outlinePositions, float outlineWidth)
        {
            // Create a new child GameObject for the stroke
            GameObject strokeObject = new GameObject("Stroke");
            strokeObject.transform.SetParent(spriteRenderer.transform);
            strokeObject.transform.localPosition = Vector3.zero;
            strokeObject.transform.localRotation = Quaternion.identity;
            strokeObject.transform.localScale = Vector3.one;
            currentOutlines.Add(strokeObject);

            // Add Mesh components to the child object
            MeshFilter meshFilter = strokeObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = strokeObject.AddComponent<MeshRenderer>();

            Material strokeMaterial = new Material(Shader.Find("Unlit/Color"));
            strokeMaterial.color = outlineColor;
            meshRenderer.material = strokeMaterial;

            Mesh originalMesh = CreateStroke(SpriteToMesh(spriteRenderer.sprite), outlinePositions);

            meshFilter.mesh = originalMesh;

            AdjustOutlineThickness(strokeObject);

            strokeObject.transform.SetParent(outlineContainer);
            Vector3 newZPos = strokeObject.transform.localPosition;
            newZPos.z = 0f;
            strokeObject.transform.localPosition = newZPos;
        }

        private Mesh SpriteToMesh(Sprite sprite)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[sprite.vertices.Length];

            // Get the center of the sprite to calculate normals
            Vector3 spriteCenter = Vector3.zero;
            foreach (var vertex in sprite.vertices)
            {
                spriteCenter += (Vector3)vertex;
            }
            spriteCenter /= sprite.vertices.Length;

            // Convert the Sprite's vertices from Vector2 to Vector3 and offset them by strokeWidth
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = sprite.vertices[i];
            }

            // Assign vertices, triangles, and UVs from the sprite
            mesh.vertices = vertices;
            mesh.triangles = Array.ConvertAll(sprite.triangles, i => (int)i);  // Convert ushort to int
            mesh.uv = sprite.uv;

            // Recalculate normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private Mesh CreateStroke(Mesh mesh, List<Vector3> outlinePositions)
        {
            //Create a copy of the list
            List<Vector3> newOutlinePositions = new List<Vector3>(outlinePositions.Count);
            for (int i = 0; i < outlinePositions.Count; i++)
            {
                // Get the current, previous, and next points
                Vector3 currentPoint = outlinePositions[i];
                Vector3 prevPoint = outlinePositions[(i - 1 + outlinePositions.Count) % outlinePositions.Count];
                Vector3 nextPoint = outlinePositions[(i + 1) % outlinePositions.Count];

                // Calculate the vectors between the points
                Vector2 dirToPrev = (currentPoint - prevPoint).normalized;
                Vector2 dirToNext = (nextPoint - currentPoint).normalized;

                // Find the bisector of the angle between the two vectors
                Vector2 bisector = (dirToPrev + dirToNext).normalized;

                // Ensure the bisector points outward
                Vector2 outwardNormal = new Vector2(-bisector.y, bisector.x).normalized;

                // Move the point along the bisector
                Vector3 newPoint = currentPoint + (Vector3)outwardNormal * outlineWidth;

                // Store the new position
                newOutlinePositions.Add(newPoint);
            }

            //Move the vertices in the mesh
            for (int i = 0; i < newOutlinePositions.Count; i++)
                mesh = MoveVertex(mesh, outlinePositions[i], newOutlinePositions[i]);

            return mesh;
        }

        public Mesh MoveVertex(Mesh mesh, Vector3 targetVertexPosition, Vector3 newVertexPosition)
        {
            Vector3[] vertices = mesh.vertices;

            int vertexIndex = -1;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Vector3.Distance(vertices[i], targetVertexPosition) < 0.01f)
                {
                    vertexIndex = i;
                    break;
                }
            }

            if (vertexIndex != -1)
            {
                vertices[vertexIndex] = newVertexPosition;

                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
            else
            {
                Debug.LogError("Vertex not found at position " + targetVertexPosition);
            }

            return mesh;
        }


        /// <summary>
        /// Clears all of the outlines created.
        /// </summary>
        private void ClearOutline()
        {
            foreach(GameObject outline in currentOutlines)
            {
                if (outline != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(outline);
#else
                    Destroy(outline);
#endif
                }
            }

            currentOutlines.Clear();

            if(outlineContainer != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(outlineContainer.gameObject);
#else
            Destroy(outlineContainer.gameObject);
#endif
            }
        }
    }
}
