using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class PipBar : MonoBehaviour
    {
        [SerializeField, Tooltip("The color of the pips.")] private Color pipColor;
        [SerializeField, Tooltip("The pip container.")] private RectTransform pipContainer;
        [Space()]

        [Header("Debug Settings")]
        [InlineButton("DebugCreateBar", SdfIconType.PlayBtn, "Initialize Bar")]
        [SerializeField, Tooltip("The amount of pips to test with.")] private int testPipAmount = 10;
        private void DebugCreateBar() => InitializeBar(testPipAmount);
        [InlineButton("DebugChangePercent", SdfIconType.ArrowClockwise, "Refresh")]
        [SerializeField, Tooltip("The percentage of the pip bar to test with.")] private float testPercent;
        private void DebugChangePercent() => UpdatePipBar(testPercent);

        private int maxPips;
        private double percentage;
        private List<Image> pipList = new List<Image>();

        private void OnValidate()
        {
            testPipAmount = Mathf.Max(1, testPipAmount);
            testPercent = Mathf.Clamp01(testPercent);
        }

        /// <summary>
        /// Initializes the pip bar.
        /// </summary>
        /// <param name="pips">The number of pips to have on the bar.</param>
        public void InitializeBar(int pips)
        {
            ClearPipBar();
            maxPips = Mathf.Max(1, pips);
            percentage = 1;

            //Create pips for the bar
            for(int i = 0; i < maxPips; i++)
            {
                GameObject newPip = new GameObject("Pip");
                Image pipImage = newPip.AddComponent<Image>();
                pipImage.color = pipColor;
                newPip.transform.SetParent(pipContainer, false);
                pipList.Add(pipImage);
            }
        }

        /// <summary>
        /// Updates the pip bar.
        /// </summary>
        /// <param name="percentage">The percentage of the pip bar filled (0 = empty, 1 = filled).</param>
        public void UpdatePipBar(float percentage)
        {
            //If there are no pips, return
            if (pipList.Count == 0)
                return;

            this.percentage = Mathf.Clamp01(percentage);

            //Update the alpha of the pips based on the percentage
            for (int i = 0; i < pipList.Count; i++)
            {
                float currentPipPercentage = (i + 1) / (float)maxPips;
                pipList[i].color = new Color(pipColor.r, pipColor.g, pipColor.b, currentPipPercentage <= this.percentage ? 1 : 0);
            }
        }

        /// <summary>
        /// Clears the pip bar.
        /// </summary>
        private void ClearPipBar()
        {
            //Destroy any pips that exist in the container
            foreach (Transform trans in pipContainer)
            {
#if UNITY_EDITOR
                DestroyImmediate(trans.gameObject);
#else
                Destroy(trans.gameObject);
#endif
            }

            //Clear the pip list
            pipList.Clear();
        }
    }
}
