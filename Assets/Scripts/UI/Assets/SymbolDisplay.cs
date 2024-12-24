using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerTanks.Scripts
{
    public class SymbolDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("The image to display the symbol on.")] private Image symbolImage;
        [SerializeField, Tooltip("The text to accompany the display.")] private TextMeshProUGUI displayText;

        /// <summary>
        /// Initializes the symbol display with a symbol sprite and some text.
        /// </summary>
        /// <param name="symbolSprite">The symbol to show on the sprite.</param>
        /// <param name="display">The initial display text to show.</param>
        public void Init(Sprite symbolSprite, string display)
        {
            symbolImage.sprite = symbolSprite;
            displayText.text = display;
        }
        
        /// <summary>
        /// Updates the display.
        /// </summary>
        /// <param name="symbolSprite">The symbol to show on the sprite.</param>
        public void UpdateDisplay(Sprite symbolSprite)
        {
            symbolImage.sprite = symbolSprite;
        }

        /// <summary>
        /// Updates the display.
        /// </summary>
        /// <param name="display">The text to show on the display.</param>
        public void UpdateDisplay(string display)
        {
            displayText.text = display;
        }

        /// <summary>
        /// Updates the display.
        /// </summary>
        /// <param name="symbolSprite">The symbol to show on the sprite.</param>
        /// <param name="display">The initial display text to show.</param>
        public void UpdateDisplay(Sprite symbolSprite, string display)
        {
            symbolImage.sprite = symbolSprite;
            displayText.text = display;
        }

        /// <summary>
        /// Destroys the display.
        /// </summary>
        public void DestroyDisplay()
        {
            Destroy(gameObject);
        }
    }
}
