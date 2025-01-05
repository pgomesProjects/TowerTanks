using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public enum LayerType { TILE, CHUNK };

        [Tooltip("The type of parallax layer.")] public LayerType layerType;
        [Tooltip("The SpriteRenderer for the background.")] public SpriteRenderer parallaxSpriteRenderer;
        [Tooltip("The speed of the parallax movement.")] public Vector2 parallaxSpeed;
        [Tooltip("The innate speed of the parallax movement.")] public Vector2 automaticSpeed;
        [Tooltip("If true, the background infinitely scrolls horizontally.")] public bool infiniteHorizontal;
        [Tooltip("If true, the background infinitely scrolls vertically.")] public bool infiniteVertical;

        private Vector2 textureUnitSize;

        public Vector2 GetTextureUnitSize() => textureUnitSize;
        public void SetTextureUnitSize(Vector2 textureUnitSize) => this.textureUnitSize = textureUnitSize;
    }

    public class MultiCameraParallaxController : MonoBehaviour
    {
        [SerializeField, Tooltip("The camera to follow.")] private Camera followCamera;
        [SerializeField, Tooltip("The layers of the parallax background.")] private List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();

        private Vector3 lastCameraPosition;
        private bool camInitialized;

        public Color[] desertColorPalette;
        public bool useDesertPalette;

        private void Start()
        {
            //Set the texture unit size of all the background pieces
            foreach(ParallaxLayer layer in parallaxLayers)
            {
                Sprite sprite = layer.parallaxSpriteRenderer.sprite;
                Texture2D texture = sprite.texture;
                layer.SetTextureUnitSize(new Vector2(texture.width / sprite.pixelsPerUnit, texture.height / sprite.pixelsPerUnit) * layer.parallaxSpriteRenderer.transform.localScale);
            }

            for (int i = 0; i < parallaxLayers.Count; i++)
            {
                if (useDesertPalette)
                {
                    SpriteRenderer renderer = parallaxLayers[i].parallaxSpriteRenderer;
                    if (renderer != null) renderer.color = desertColorPalette[i];
                }
            }

            InitCamera();
        }

        /// <summary>
        /// Initializes the camera information.
        /// </summary>
        private void InitCamera()
        {
            if (followCamera == null)
            {
                camInitialized = false;
                return;
            }

            lastCameraPosition = followCamera.transform.position;
            camInitialized = true;
        }

        private void LateUpdate()
        {
            //If there is no camera to follow, return
            if (followCamera == null)
            {
                if (camInitialized)
                    camInitialized = false;
                return;
            }

            //If the camera is not initialized, initialize it
            else if (!camInitialized)
                InitCamera();

            //Get the movement of the camera
            Vector3 deltaMovement = followCamera.transform.position - lastCameraPosition;
            lastCameraPosition = followCamera.transform.position;

            //Move all of the layers accordingly
            foreach (ParallaxLayer layer in parallaxLayers)
            {
                switch (layer.layerType)
                {
                    case ParallaxLayer.LayerType.TILE:
                        //Move the layer
                        layer.parallaxSpriteRenderer.transform.position += new Vector3((deltaMovement.x * layer.parallaxSpeed.x) + (layer.automaticSpeed.x * Time.deltaTime), (deltaMovement.y * layer.parallaxSpeed.y) + (layer.automaticSpeed.y * Time.deltaTime), 0);

                        //If the layer scrolls infinitely horizontally and has reached the end of the texture, offset the position
                        if (layer.infiniteHorizontal && Mathf.Abs(followCamera.transform.position.x - layer.parallaxSpriteRenderer.transform.position.x) >= layer.GetTextureUnitSize().x)
                        {
                            float offsetPositionX = (followCamera.transform.position.x - layer.parallaxSpriteRenderer.transform.position.x) % layer.GetTextureUnitSize().x;
                            layer.parallaxSpriteRenderer.transform.position = new Vector3(followCamera.transform.position.x + offsetPositionX, layer.parallaxSpriteRenderer.transform.position.y);
                        }

                        //If the layer scrolls infinitely vertically and has reached the end of the texture, offset the position
                        if (layer.infiniteVertical && Mathf.Abs(followCamera.transform.position.y - layer.parallaxSpriteRenderer.transform.position.y) >= layer.GetTextureUnitSize().y)
                        {
                            float offsetPositionY = (followCamera.transform.position.y - layer.parallaxSpriteRenderer.transform.position.y) % layer.GetTextureUnitSize().y;
                            layer.parallaxSpriteRenderer.transform.position = new Vector3(layer.parallaxSpriteRenderer.transform.position.x, layer.parallaxSpriteRenderer.transform.position.y + offsetPositionY);
                        }
                        break;
                    case ParallaxLayer.LayerType.CHUNK:
                        break;
                }
            }
        }

        public void AddCameraToParallax(Camera newCamera)
        {
            followCamera = newCamera;
        }
    }
}
