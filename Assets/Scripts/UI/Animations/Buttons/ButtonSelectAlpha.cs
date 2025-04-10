using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    [RequireComponent(typeof(Image))]
    public class ButtonSelectAlpha : MonoBehaviour
    {
        [SerializeField, Tooltip("The duration of the alpha fade.")] private float alphaFadeDuration;
        [SerializeField, Tooltip("The alpha of the button when selected.")] private float alphaSelected = 1f;
        [SerializeField, Tooltip("The alpha of the button when deselected.")] private float alphaDeselected = 0.5f;

        private bool fadeInProgress;
        private float startingAlpha, endingAlpha;
        private float alphaFadeElapsed;

        private Image buttonImage;
        private Color startingColor, endingColor;

        private void Awake()
        {
            buttonImage = GetComponent<Image>();
            startingColor = buttonImage.color;
            endingColor = buttonImage.color;
            startingColor.a = alphaDeselected;
        }

        private void OnEnable() => buttonImage.color = startingColor;

        public void OnSelected()
        {
            startingColor.a = alphaDeselected;
            endingColor.a = alphaSelected;
            fadeInProgress = true;
        }

        public void OnDeselected()
        {
            startingColor.a = alphaSelected;
            endingColor.a = alphaDeselected;
            fadeInProgress = true;
        }

        private void Update()
        {
            if (fadeInProgress)
            {
                if(alphaFadeElapsed < alphaFadeDuration)
                {
                    buttonImage.color = Color.Lerp(startingColor, endingColor, alphaFadeElapsed / alphaFadeDuration);
                    alphaFadeElapsed += Time.unscaledDeltaTime;
                }
                else
                {
                    buttonImage.color = endingColor;
                    fadeInProgress = false;
                }
            }
        }
    }
}
