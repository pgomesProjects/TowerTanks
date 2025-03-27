using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    [RequireComponent(typeof(Image))]
    public class PerlinNoiseGenerator : MonoBehaviour
    {
        private Image targetImage;
        [SerializeField, Tooltip("The dimensions of the noise texture.")] private Vector2Int textureDimensions = new Vector2Int(256, 256);
        public float refreshRate = 0.1f; // Time in seconds between updates


        private Texture2D noiseTexture;
        private Color[] pixels;
        private float elapsedTime;

        private void Start()
        {
            targetImage = GetComponent<Image>();

            //Create a texture for the perlin noise
            noiseTexture = new Texture2D(textureDimensions.x, textureDimensions.y);
            noiseTexture.filterMode = FilterMode.Point;
            noiseTexture.wrapMode = TextureWrapMode.Clamp;

            //Attach the texture to the image
            targetImage.material.mainTexture = noiseTexture;
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= refreshRate)
            {
                GenerateNoise();
                elapsedTime = 0f;
            }
        }

        /// <summary>
        /// Generates a noise texture and applies it to the image.
        /// </summary>
        private void GenerateNoise()
        {
            //For each pixel, change the color to a random grayscale value
            pixels = new Color[textureDimensions.x * textureDimensions.y];
            for (int i = 0; i < pixels.Length; i++)
            {
                float noiseValue = Random.value;
                pixels[i] = new Color(noiseValue, noiseValue, noiseValue, 1f);
            }

            //Apply the texture to the pixels
            noiseTexture.SetPixels(pixels);
            noiseTexture.Apply();
        }
    }
}
