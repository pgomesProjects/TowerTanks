using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class HeatmapPixel
    {
        public Cell cell;

        public Vector2 pos { get; private set; }
        private float percent;

        private Color startingColor;
        private Color endingColor;
        public Color currentColor { get; private set; }

        public HeatmapPixel(Cell cell, Vector2 pos, Color startingColor, Color endingColor)
        {
            this.cell = cell;
            this.pos = pos;

            percent = 1f;

            this.startingColor = startingColor;
            this.endingColor = endingColor;

            RefreshPixel();
        }

        public void UpdatePixelHealth(float percent)
        {
            this.percent = percent;
            RefreshPixel();
        }

        private void RefreshPixel() => currentColor = Color.Lerp(endingColor, startingColor, percent);
    }

    public class HeatmapGenerator : MonoBehaviour
    {
        [SerializeField, Tooltip("The container for the heatmap.")] private RectTransform heatMapContainer;
        [SerializeField, Tooltip("The raw image for the heatmap.")] private RawImage heatMapImage;
        [SerializeField, Tooltip("The padding values for the heatmap.")] private Vector2 padding;

        [SerializeField, Tooltip("The color to represent a cell at full health.")] private Color startingColor = Color.white;
        [SerializeField, Tooltip("The color to represent a cell at zero health.")] private Color endingColor = Color.red;

        private TankController tankDesign;
        private List<HeatmapPixel> heatMapPixels = new List<HeatmapPixel>();

        private Texture2D heatMapTexture;
        private int pixelSize;

        [Button]
        public void BuildTankHeatmap() => BuildHeatmap();

        /// <summary>
        /// Assigns a tank to the heatmap.
        /// </summary>
        /// <param name="tankDesign">The tank to build.</param>
        public void AssignTank(TankController tankDesign)
        {
            this.tankDesign = tankDesign;
            BuildHeatmap();
        }

        /// <summary>
        /// Generates the heatmap of the tank.
        /// </summary>
        public void BuildHeatmap()
        {
            //If there is no design, return
            if (tankDesign == null)
                return;

            Cell[] tankCells = tankDesign.GetComponentsInChildren<Cell>();

            //If no cells are found, return
            if (tankCells.Length == 0)
                return;

            // Find bounds of the heatmap
            Vector2 containerSize = heatMapContainer.sizeDelta - padding;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (Cell cell in tankCells)
            {
                Vector2 pos = cell.transform.position;

                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;

                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            //Get the width of the heatmap
            Vector2 mapSize = new Vector2(Mathf.Abs(minX - maxX) + 1, Mathf.Abs(minY - maxY) + 1);

            // Calculate the pixel size that will allow the level to fit within the RectTransform
            pixelSize = Mathf.FloorToInt(Mathf.Min(containerSize.x / mapSize.x, containerSize.y / mapSize.y));

            // Create a texture based on the map size
            int textureWidth = Mathf.CeilToInt(containerSize.x + padding.x);
            int textureHeight = Mathf.CeilToInt(containerSize.y + padding.y);
            heatMapTexture = new Texture2D(textureWidth, textureHeight);

            ClearMap();

            //Calculate the offset for the map to center horizontally and vertically
            Vector2 offset = new Vector2((minX + (maxX - minX) / 2) * pixelSize, (minY + (maxY - minY) / 2) * pixelSize);

            //Get each cell in the tank
            foreach (Cell cell in tankCells)
            {
                //Get the position and adjust based on the pixel size
                Vector2 pixelPosition = cell.transform.position;
                int posX = Mathf.RoundToInt(pixelPosition.x * pixelSize - offset.x);
                int posY = Mathf.RoundToInt(pixelPosition.y * pixelSize - offset.y);

                //Adjust the x and y values, since the pivot point of the Texture2D is the bottom left instead of the center
                float imageX = posX + ((containerSize.x + padding.x) / 2) - 0.5f;
                float imageY = posY + ((containerSize.y + padding.y) / 2) - 0.5f;

                //Create the block on the heatmap
                DrawBlock(new Vector2(imageX, imageY), pixelSize, startingColor);
                heatMapPixels.Add(new HeatmapPixel(cell, new Vector2(imageX, imageY), startingColor, endingColor));

                //Subscribe to the OnCellUpdated action
                cell.OnCellHealthUpdated += OnCellHealthUpdated;
            }

            RefreshHeatMapTexture();
        }

        /// <summary>
        /// Clears the heatmap.
        /// </summary>
        public void ClearMap()
        {
            //Clear the heatmap
            for (int x = 0; x < heatMapTexture.width; x++)
                for (int y = 0; y < heatMapTexture.height; y++)
                    heatMapTexture.SetPixel(x, y, Color.clear);

            heatMapPixels.Clear();
        }

        /// <summary>
        /// Creates a block on a texture 2D.
        /// </summary>
        /// <param name="pos">The position of the block.</param>
        /// <param name="pixelSize">The size of the block.</param>
        /// <param name="color">The color of the block.</param>
        private void DrawBlock(Vector2 pos, int pixelSize, Color color)
        {
            // Get half the width to calculate the radius of the block
            int halfWidth = Mathf.FloorToInt((pixelSize - 1) / 2);

            // Calculate the bounds of the rectangle
            int startX = Mathf.Max(0, Mathf.FloorToInt(pos.x - halfWidth));
            int endX = Mathf.Min(heatMapTexture.width, Mathf.CeilToInt(pos.x + halfWidth));
            int startY = Mathf.Max(0, Mathf.FloorToInt(pos.y - halfWidth));
            int endY = Mathf.Min(heatMapTexture.height, Mathf.CeilToInt(pos.y + halfWidth));

            // Loop through each pixel in the block and set its color
            for (int i = startX; i <= endX; i++)
                for (int j = startY; j <= endY; j++)
                    heatMapTexture.SetPixel(i, j, color);
        }

        /// <summary>
        /// Refreshes the heatmap texture.
        /// </summary>
        private void RefreshHeatMapTexture()
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
            //Get the pixel and update its health
            HeatmapPixel currentPixel = GetPixel(cell);

            //If there is no pixel found, return
            if (currentPixel == null)
                return;

            currentPixel.UpdatePixelHealth(percent);
            DrawBlock(currentPixel.pos, pixelSize, currentPixel.currentColor);

            //If it has no more health, unsubscribe from the cell and remove it from the list
            if (percent <= 0)
            {
                cell.OnCellHealthUpdated -= OnCellHealthUpdated;
                DrawBlock(currentPixel.pos, pixelSize, Color.clear);
                heatMapPixels.Remove(currentPixel);
            }

            RefreshHeatMapTexture();
        }

        /// <summary>
        /// Get the heatmap pixel at a specific location.
        /// </summary>
        /// <param name="cell">The cell to find on the heatmap.</param>
        /// <returns>The heatmap pixel associated with the cell. Null if not found.</returns>
        public HeatmapPixel GetPixel(Cell cell)
        {
            foreach (HeatmapPixel heatmapPixel in heatMapPixels)
                //If the cell given matches the current cell, return it
                if (heatmapPixel.cell == cell)
                    return heatmapPixel;

            //Return null if not found
            return null;
        }
    }
}
