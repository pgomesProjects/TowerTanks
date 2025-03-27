using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class HeatmapPixel
    {
        public Cell cell;
        public RawImage rawImage;

        private float percent;

        private Color startingColor;
        private Color endingColor;

        public HeatmapPixel(RawImage rawImage, Cell cell, Color startingColor, Color endingColor)
        {
            this.rawImage = rawImage;
            this.cell = cell;

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

        private void RefreshPixel() => rawImage.color = Color.Lerp(endingColor, startingColor, percent);
    }

    public class HeatmapGenerator : MonoBehaviour
    {
        [SerializeField, Tooltip("The container of the heatmap.")] private RectTransform heatMapContainer;
        [SerializeField, Tooltip("The pixel prefab.")] private RawImage pixel;
        [SerializeField, Tooltip("The size of the pixels.")] private float pixelSize = 100f;
        [SerializeField, Tooltip("The lost connection screen.")] private RectTransform lostConnectionScreen;

        [SerializeField, Tooltip("The color to represent a cell at full health.")] private Color startingColor = Color.white;
        [SerializeField, Tooltip("The color to represent a cell at zero health.")] private Color endingColor = Color.red;

        private TankController tankDesign;
        private List<HeatmapPixel> heatMapPixels = new List<HeatmapPixel>();

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
            //Clear the map
            ClearMap();

            //If there is no design, return
            if (tankDesign == null)
                return;

            lostConnectionScreen.gameObject.SetActive(false);

            Cell[] tankCells = tankDesign.GetComponentsInChildren<Cell>();

            //If no cells are found, return
            if (tankCells.Length == 0)
                return;

            // Find bounds of the heatmap
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

            // Calculate the center offset
            Vector2 centerOffset = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            centerOffset *= pixelSize;

            //Get each cell in the tank
            foreach (Cell cell in tankCells)
            {
                //Create a pixel, set the position and the size
                RectTransform pixelTransform = Instantiate(pixel, heatMapContainer).GetComponent<RectTransform>();

                //Get the position and adjust based on the pixel size
                Vector2 pixelPosition = cell.transform.position;
                pixelPosition *= pixelSize;
                pixelPosition -= centerOffset;

                //Set the position and the pixel size
                pixelTransform.anchoredPosition = pixelPosition;
                pixelTransform.sizeDelta = new Vector2(pixelSize, pixelSize);

                //Get the image and add it to the list
                RawImage currentPixelImage = pixelTransform.GetComponent<RawImage>();
                currentPixelImage.color = startingColor;
                heatMapPixels.Add(new HeatmapPixel(currentPixelImage, cell, startingColor, endingColor));

                //Subscribe to the OnCellUpdated action
                cell.OnCellHealthUpdated += OnCellHealthUpdated;
            }
        }

        /// <summary>
        /// Clears the heatmap.
        /// </summary>
        private void ClearMap()
        {
            //Clear the heatmap container
            foreach (Transform trans in heatMapContainer)
                Destroy(trans.gameObject);
            heatMapPixels.Clear();

            lostConnectionScreen.gameObject.SetActive(true);
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

            //If it has no more health, unsubscribe from the cell and remove it from the list
            if(percent <= 0)
            {
                cell.OnCellHealthUpdated -= OnCellHealthUpdated;
                Destroy(currentPixel.rawImage);
                heatMapPixels.Remove(currentPixel);

                //If there are no more pixels, show the lost connection screen
                if (heatMapPixels.Count == 0)
                    lostConnectionScreen.gameObject.SetActive(true);
            }
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
