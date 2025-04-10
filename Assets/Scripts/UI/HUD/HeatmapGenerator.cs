using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class HeatmapPixel
    {
        public Vector2 pos { get; protected set; }
        protected float percent;

        protected Color healthyColor, damagedColor, deadColor;
        public Color currentColor { get; protected set; }

        public HeatmapPixel(Vector2 pos, Color healthyColor, Color damagedColor, Color deadColor)
        {
            this.pos = pos;
            percent = 1f;
            this.healthyColor = healthyColor;
            this.damagedColor = damagedColor;
            this.deadColor = deadColor;

            RefreshPixel();
        }

        public HeatmapPixel(Vector2 pos, float percent, Color healthyColor, Color damagedColor, Color deadColor)
        {
            this.pos = pos;
            this.percent = percent;
            this.healthyColor = healthyColor;
            this.damagedColor = damagedColor;
            this.deadColor = deadColor;

            RefreshPixel();
        }

        public void UpdatePixelHealth(float percent)
        {
            this.percent = percent;
            RefreshPixel();
        }

        protected void RefreshPixel() => currentColor = Mathf.Approximately(percent, 1f) ? healthyColor : Color.Lerp(deadColor, damagedColor, percent);
    }

    public class CellHeatmapPixel : HeatmapPixel
    {
        public Cell cell { get; private set; }
        public CellHeatmapPixel(Cell cell, Vector2 pos, Color healthyColor, Color damagedColor, Color deadColor) : base(pos, healthyColor, damagedColor, deadColor)
        {
            this.cell = cell;
        }

        public CellHeatmapPixel(Cell cell, Vector2 pos, float percent, Color healthyColor, Color damagedColor, Color deadColor) : base(pos, percent, healthyColor, damagedColor, deadColor)
        {
            this.cell = cell;
        }
    }

    public class HeatmapGenerator : MonoBehaviour
    {
        [Header("Container Settings")]
        [SerializeField, Tooltip("The container for the heatmap.")] private RectTransform heatMapContainer;
        [SerializeField, Tooltip("The raw image for the heatmap.")] private RawImage heatMapImage;
        [SerializeField, Tooltip("The padding values for the heatmap.")] private Vector2 padding;
        [Space()]
        [Header("Cell Settings")]
        [SerializeField, Tooltip("The border radius of the cell visual.")] private float cellRadius;
        [Header("Tread Settings")]
        [SerializeField, Tooltip("The size of the treads (in pixel units).")] private Vector2Int treadSize = new Vector2Int(6, 2);
        [SerializeField, Tooltip("The spacing of the treads from the tank.")] private float treadSpacing = 0.5f;
        [SerializeField, Tooltip("The border radius of the treads visual.")] private float treadRadius = 0.5f;
        [Space()]
        [Header("Color Settings")]
        [SerializeField, Tooltip("The color to represent a cell at full health.")] private Color healthyColor = Color.green;
        [SerializeField, Tooltip("The color to represent a damaged cell.")] private Color damagedColor = Color.yellow;
        [SerializeField, Tooltip("The color to represent a cell at zero health.")] private Color deadColor = Color.red;
        [Space()]

        private TankController assignedTank;
        private HashSet<CellHeatmapPixel> cellHeatmapPixels = new HashSet<CellHeatmapPixel>();
        private HeatmapPixel treadPixel;

        private Texture2D heatMapTexture;
        private int pixelSize;

        [Button]
        public void RefreshHeatmap() => BuildHeatmap();

        /// <summary>
        /// Assigns a tank to the heatmap.
        /// </summary>
        /// <param name="assignedTank">The tank to build.</param>
        public void AssignTank(TankController assignedTank)
        {
            //If there is already an assigned tank, unsubscribe the treads from the action
            if (this.assignedTank != null)
                assignedTank.treadSystem.OnTreadHealthUpdated -= OnTreadHealthUpdated;

            this.assignedTank = assignedTank;
            BuildHeatmap();
        }

        /// <summary>
        /// Builds the heatmap.
        /// </summary>
        private async void BuildHeatmap()
        {
            //Get the cells of the assigned tank
            Cell[] cells = assignedTank.GetComponentsInChildren<Cell>();

            //If there are no cells, return
            if (cells.Length == 0) return;

            //Cache the positions of each cell
            List<Vector2> cellPositions = cells.Select(c => (Vector2)assignedTank.transform.Find("TowerJoint").InverseTransformPoint(c.transform.position)).ToList();

            //Get the bounds of the container
            Vector2 containerSize = heatMapContainer.sizeDelta - padding;

            // Create a texture based on the map size
            int textureWidth = Mathf.CeilToInt(heatMapContainer.sizeDelta.x);
            int textureHeight = Mathf.CeilToInt(heatMapContainer.sizeDelta.y);
            heatMapTexture = new Texture2D(textureWidth, textureHeight);

            //Clear the map
            ClearMap();

            //Generate the pixel data in a separate thread
            await Task.Run(() => GeneratePixelData(cells, assignedTank.treadSystem, cellPositions, containerSize));

            //For each heatmap pixel, draw the block and subscribe the corresponding cell to it
            foreach(CellHeatmapPixel pixel in cellHeatmapPixels)
            {
                DrawBlock(pixel.pos, pixelSize, healthyColor, cellRadius);
                pixel.cell.OnCellHealthUpdated += OnCellHealthUpdated;
            }

            //Draw the treads and subscribe the treads to the heatmap pixel
            DrawBlock(treadPixel.pos, treadSize * pixelSize, treadPixel.currentColor, treadRadius);
            assignedTank.treadSystem.OnTreadHealthUpdated += OnTreadHealthUpdated;

            //Applies the heatmap texture to the image
            ApplyHeatmapTexture();
        }

        /// <summary>
        /// Creates the data for the heatmap.
        /// </summary>
        /// <param name="cells">The list of cells to draw.</param>
        /// <param name="treads">The treads of the tank.</param>
        /// <param name="cellPositions">The positions of each cell.</param>
        /// <param name="containerSize">The size of the container.</param>
        private void GeneratePixelData(Cell[] cells, TreadSystem treads, List<Vector2> cellPositions, Vector2 containerSize)
        {
            //Get the minimums and maximums of the tank
            float minX = cellPositions.Min(p => p.x), maxX = cellPositions.Max(p => p.x);
            float minY = cellPositions.Min(p => p.y), maxY = cellPositions.Max(p => p.y);

            //Get the width of the heatmap
            Vector2 tankSize = new Vector2(Mathf.Abs(minX - maxX) + 1, Mathf.Abs(minY - maxY) + 1);
            Vector2 mapSize = new Vector2(Mathf.Max(tankSize.x, treadSize.x + treadSpacing), tankSize.y + treadSize.y + treadSpacing);

            // Calculate the pixel size that will allow the level to fit within the RectTransform
            pixelSize = Mathf.FloorToInt(Mathf.Min(containerSize.x / mapSize.x, containerSize.y / mapSize.y));
            //Calculate the offset for the map to center horizontally and vertically
            Vector2 offset = new Vector2((minX + (maxX - minX) / 2) * pixelSize, (minY + (maxY - minY) / 2 - treadSize.y / 2f) * pixelSize);

            //Get each cell in the tank
            for (int i = 0; i < cells.Length; i++)
            {
                //Get the position and adjust based on the pixel size
                Vector2 pixelPosition = cellPositions[i];
                float posX = pixelPosition.x * pixelSize - offset.x;
                float posY = pixelPosition.y * pixelSize - offset.y;

                //Adjust the x and y values, since the pivot point of the Texture2D is the bottom left instead of the center (round to the nearest hundredth for precision)
                float imageX = (float)System.Math.Round(posX + ((containerSize.x + padding.x) / 2) - 0.5f, 2, System.MidpointRounding.AwayFromZero);
                float imageY = (float)System.Math.Round(posY + ((containerSize.y + padding.y) / 2) - 0.5f, 2, System.MidpointRounding.AwayFromZero);

                //Add the pixel to the cell heatmap
                cellHeatmapPixels.Add(new CellHeatmapPixel(cells[i], new Vector2(imageX, imageY), cells[i].GetCellHealthPercentage(), healthyColor, damagedColor, deadColor));
            }

            //Create the tread pixels below the tank
            float treadPosX = 0f;
            float treadPosY = (minY - 1 - treadSpacing) * pixelSize - offset.y;

            float treadImageX = treadPosX + ((containerSize.x + padding.x) / 2f) - 0.5f;
            float treadImageY = treadPosY + ((containerSize.y + padding.y) / 2f) - 0.5f;

            //Assign the tread pixel
            treadPixel = new HeatmapPixel(new Vector2(treadImageX, treadImageY), treads.GetTreadHealthPercentage(), healthyColor, damagedColor, deadColor);
        }

        private void OnDisable()
        {
            //If there is already an assigned tank, unsubscribe the treads from the action
            if (this.assignedTank != null)
                assignedTank.treadSystem.OnTreadHealthUpdated -= OnTreadHealthUpdated;

            ClearMap();
        }

        /// <summary>
        /// Clears the heatmap.
        /// </summary>
        public void ClearMap()
        {
            //Clear the heatmap if the texture exists
            if(heatMapTexture != null)
            {
                Color32[] clearColors = new Color32[heatMapTexture.width * heatMapTexture.height];
                for (int i = 0; i < clearColors.Length; i++)
                    clearColors[i] = Color.clear;

                heatMapTexture.SetPixels32(clearColors);
            }

            //Unsubscribe all cells from the map
            foreach (CellHeatmapPixel pixel in cellHeatmapPixels)
                pixel.cell.OnCellHealthUpdated -= OnCellHealthUpdated;

            cellHeatmapPixels.Clear();
            treadPixel = null;
        }

        /// <summary>
        /// Creates a block on a texture 2D.
        /// </summary>
        /// <param name="pos">The position of the block.</param>
        /// <param name="pixelSize">The size of the block.</param>
        /// <param name="color">The color of the block.</param>
        /// <param name="radius">The corner radius of the block.</param>
        private void DrawBlock(Vector2 pos, int pixelSize, Color color, float radius = 0f) => DrawBlock(pos, new Vector2(pixelSize, pixelSize), color, radius);

        /// <summary>
        /// Creates a block on a texture 2D.
        /// </summary>
        /// <param name="pos">The position of the block.</param>
        /// <param name="size">The width and height of the block.</param>
        /// <param name="color">The color of the block.</param>
        /// <param name="radius">The corner radius of the block.</param>
        private void DrawBlock(Vector2 pos, Vector2 size, Color color, float radius = 0f)
        {
            // Get half the width/height for easier calculations
            float halfWidthX = (size.x - 1) / 2f;
            float halfWidthY = (size.y - 1) / 2f;

            // Rectangle bounds
            int startX = Mathf.Max(0, Mathf.FloorToInt(pos.x - halfWidthX));
            int endX = Mathf.Min(heatMapTexture.width, Mathf.CeilToInt(pos.x + halfWidthX));
            int startY = Mathf.Max(0, Mathf.FloorToInt(pos.y - halfWidthY));
            int endY = Mathf.Min(heatMapTexture.height, Mathf.CeilToInt(pos.y + halfWidthY));

            // Loop through each pixel
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    //Get the position from the center of the rectangle
                    float localX = Mathf.Abs(x - pos.x);
                    float localY = Mathf.Abs(y - pos.y);

                    //Get the distance from each corner
                    float distanceX = Mathf.Max(localX - halfWidthX + radius, 0f);
                    float distanceY = Mathf.Max(localY - halfWidthY + radius, 0f);

                    //If the distance is outside of the radius bounds, don't draw the pixel
                    if ((distanceX * distanceX + distanceY * distanceY) > (radius * radius))
                        continue;

                    //Set the pixel on the heatmap texture
                    heatMapTexture.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>
        /// Refreshes the heatmap texture.
        /// </summary>
        private void ApplyHeatmapTexture()
        {
            heatMapTexture.filterMode = FilterMode.Point;
            heatMapTexture.Apply();
            heatMapImage.texture = heatMapTexture;
        }

        /// <summary>
        /// The function called when a cell's health is updated.
        /// </summary>
        /// <param name="cell">The cell to update.</param>
        /// <param name="percent">The percentage of the cell's health (0 = destroyed, 1 = full).</param>
        private void OnCellHealthUpdated(Cell cell, float percent)
        {
            //Get the pixel
            CellHeatmapPixel currentPixel = GetPixel(cell);

            //If there is no pixel found, return
            if (currentPixel == null)
                return;

            Debug.Log("Cell Health Updated To " + percent);

            //Update the pixel's health
            currentPixel.UpdatePixelHealth(percent);

            //If it has no more health, unsubscribe from the cell and remove it from the list
            if (percent <= 0)
            {
                cell.OnCellHealthUpdated -= OnCellHealthUpdated;
                DrawBlock(currentPixel.pos, pixelSize, Color.clear, cellRadius);
                cellHeatmapPixels.Remove(currentPixel);
            }
            //Otherwise, recolor the block as needed
            else
                DrawBlock(currentPixel.pos, pixelSize, currentPixel.currentColor, cellRadius);

            //Apply the updated texture
            ApplyHeatmapTexture();
        }

        private void OnTreadHealthUpdated(float percent)
        {
            //If there are no tread pixels, return
            if (treadPixel == null)
                return;

            //Update the pixel's health
            treadPixel.UpdatePixelHealth(percent);

            //Recolor the block
            DrawBlock(treadPixel.pos, treadSize * pixelSize, treadPixel.currentColor, treadRadius);

            //Apply the updated texture
            ApplyHeatmapTexture();
        }

        /// <summary>
        /// Get the heatmap pixel at a specific location.
        /// </summary>
        /// <param name="cell">The cell to find on the heatmap.</param>
        /// <returns>The heatmap pixel associated with the cell. Null if not found.</returns>
        public CellHeatmapPixel GetPixel(Cell cell)
        {
            foreach (CellHeatmapPixel heatmapPixel in cellHeatmapPixels)
                //If the cell given matches the current cell, return it
                if (heatmapPixel.cell == cell)
                    return heatmapPixel;

            //Return null if not found
            return null;
        }
    }
}
