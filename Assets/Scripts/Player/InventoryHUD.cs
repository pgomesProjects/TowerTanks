using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class InventoryHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("The icon image.")] private Image icon;
        [SerializeField, Tooltip("The alt text for when an icon is unavailable.")] private TextMeshProUGUI altText;

        [SerializeField, Tooltip("The progress bar.")] private ProgressBar progressBar;
        [SerializeField, Tooltip("The pip bar.")] private PipBar pipBar;

        [Header("Debug Settings")]
        [InlineButton("DebugAddItem", SdfIconType.PlusSquareFill, "Add To Inventory")]
        [SerializeField, Tooltip("An item used for testing.")] private InventoryItem testItem;
        private void DebugAddItem()
        {
            AddToInventory(testItem);
            if (testItem.barType == BarType.PROGRESS)
                InitializeBar(1f);

            if (testItem.barType == BarType.PIP)
                InitializeBar(10);
        }

        public enum BarType { NONE, PROGRESS, PIP };

        private InventoryItem currentItem;

        private void Start()
        {
            ClearInventory();
        }

        /// <summary>
        /// Adds an item to the inventory.
        /// </summary>
        /// <param name="inventoryItem">The inventory item.</param>
        public void AddToInventory(InventoryItem inventoryItem)
        {
            //If the item is null, returninvent
            if (inventoryItem == null)
                return;

            //Update the alt-text and the icon color
            altText.text = inventoryItem.icon == null ? inventoryItem.name : "";
            icon.color = new Color(1, 1, 1, inventoryItem.icon == null ? 0 : 1);

            //Update the sprite and the item
            icon.sprite = inventoryItem.icon;
            currentItem = inventoryItem;

            //Update the bar shown
            progressBar.gameObject.SetActive(currentItem.barType == BarType.PROGRESS);
            pipBar.gameObject.SetActive(currentItem.barType == BarType.PIP);
        }

        /// <summary>
        /// Initializes the progress bar.
        /// </summary>
        /// <param name="percent">The percent to start with (0 = min, 1 = max).</param>
        public void InitializeBar(float percent)
        {
            //If the bar type shown is not progress, return
            if (currentItem.barType != BarType.PROGRESS)
                return;

            progressBar.UpdateProgressValue(percent);
        }

        /// <summary>
        /// Initializes the pip bar.
        /// </summary>
        /// <param name="maxPips">The number of pips to show.</param>
        public void InitializeBar(int maxPips)
        {
            //If the bar type shown is not pip, return
            if (currentItem.barType != BarType.PIP)
                return;

            pipBar.InitializeBar(maxPips);
        }

        /// <summary>
        /// Updates the item bar's value.
        /// </summary>
        /// <param name="percentage">The item bar's percentage (0 = min, 1 = max).</param>
        public void UpdateItemBar(float percentage)
        {
            switch (currentItem.barType)
            {
                case BarType.PROGRESS:
                    progressBar.UpdateProgressValue(percentage);
                    break;
                case BarType.PIP:
                    pipBar.UpdatePipBar(percentage);
                    break;
            }
        }

        /// <summary>
        /// Clears the inventory.
        /// </summary>
        public void ClearInventory()
        {
            altText.text = "";
            icon.color = new Color(1, 1, 1, 0);

            if(progressBar != null)
                progressBar.gameObject.SetActive(false);
            if(pipBar != null)
                pipBar.gameObject.SetActive(false);
        }
    }
}
