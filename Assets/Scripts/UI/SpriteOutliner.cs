using Sirenix.OdinInspector;
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
        private Vector3 center;

        private bool currentVisibility;

        [Button("Generate Outline")]
        public void DebugGenerateOutline()
        {
            ClearOutline();
            center = CalculateBounds().center;
            currentVisibility = isVisible;

            if (isVisible)
            {
                //Generate an outline for all SpriteRenderers childed to the object
                foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
                    GenerateOutline(spriteRenderer);
            }
        }

        [Button("Refresh")]
        public void DebugRefreshOutline()
        {
            //If the visibility has changed, regenerate the outline
            if (currentVisibility != isVisible)
                DebugGenerateOutline();

            //Adjust the color and thickness of the outline
            foreach (GameObject outline in currentOutlines)
            {
                AdjustOutlineColor(outline);
                AdjustOutlineThickness(outline);
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

            CreateLineRenderer(spriteRenderer, numPointsBetween, vertices, allVertices);
            //CreateSpriteRenderer(spriteRenderer);
        }

        /// <summary>
        /// Creates an outline of a SpriteRenderer using a LineRenderer.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer to make the outline for.</param>
        /// <param name="numPointsBetween">The number of points in between each vertex.</param>
        /// <param name="vertices">A list of all vertices in the SpriteRenderer.</param>
        /// <param name="allVertices">A dictionary of all unique vertices and their edge information.</param>
        private void CreateLineRenderer(SpriteRenderer spriteRenderer, int numPointsBetween, Vector3[] vertices, Dictionary<int, int> allVertices)
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

            AdjustOutlineColor(currentOutline.gameObject);
            AdjustOutlineThickness(currentOutline.gameObject);
        }

        /// <summary>
        /// Creates a SpriteRenderer childed to the original with a color and thickness.
        /// </summary>
        /// <param name="spriteRenderer">The SpriteRenderer to create an outline for.</param>
        private void CreateSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            GameObject newOutline = Instantiate(spriteRenderer.gameObject, spriteRenderer.transform);
            newOutline.name = "Outline";
            newOutline.transform.localPosition = Vector3.zero;
            newOutline.transform.localEulerAngles = Vector3.zero;
            newOutline.transform.localScale = Vector3.one;
            currentOutlines.Add(newOutline);

            AdjustOutlineColor(newOutline);
            AdjustOutlineThickness(newOutline);
        }

        /// <summary>
        /// Adjusts the color of the outline.
        /// </summary>
        /// <param name="outline">The current outline GameObject.</param>
        private void AdjustOutlineColor(GameObject outline)
        {
            //Adjust the color and the sorting order
            SpriteRenderer spriteRenderer;
            if(outline.TryGetComponent(out spriteRenderer))
            {
                spriteRenderer.color = outlineColor;
                spriteRenderer.material = outlineMat;
                spriteRenderer.sortingOrder = sortingOrder;
            }

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

            SpriteRenderer spriteRenderer;
            if(outline.TryGetComponent(out spriteRenderer))
            {
                SpriteRenderer parentRenderer = spriteRenderer.transform.parent.GetComponent<SpriteRenderer>();

                Vector2 parentSize = parentRenderer.transform.localScale;
                float extraWorldUnits = outlineWidth / parentRenderer.sprite.pixelsPerUnit;

                Vector2 targetSize = new Vector2(parentSize.x + 2 * extraWorldUnits,parentSize.y + 2 * extraWorldUnits);
                spriteRenderer.transform.localScale = new Vector3(targetSize.x / parentSize.x, targetSize.y / parentSize.y, spriteRenderer.transform.localScale.z);
            }

            LineRenderer outlineLineRenderer;
            if (outline.TryGetComponent(out outlineLineRenderer))
            {
                //Adjust the position and scale based on the multiplier
                Vector3 directionFromCenter = outline.transform.position - center;
                Vector3 scaledPosition = center + directionFromCenter * scaleMultiplier;
                outline.transform.position = scaledPosition;
                outline.transform.localScale = Vector3.one * scaleMultiplier;
                outlineLineRenderer.startWidth = outlineLineRenderer.endWidth = outlineWidth;
            }
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
        }
    }
}
