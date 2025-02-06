using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class TileParallaxLayer : ParallaxLayer
    {
        [Tooltip("The SpriteRenderer for the background.")] public SpriteRenderer parallaxSpriteRenderer;
        private Vector2 textureUnitSize;

        public Vector2 GetTextureUnitSize() => textureUnitSize;
        public void SetTextureUnitSize(Vector2 textureUnitSize) => this.textureUnitSize = textureUnitSize;
    }

    public class TileCameraParallaxController : MultiCameraParallaxController
    {
        [SerializeField, Tooltip("The layers of the parallax background")] private List<TileParallaxLayer> tileParallaxLayers = new List<TileParallaxLayer>();
        protected override List<ParallaxLayer> parallaxLayers => tileParallaxLayers.Cast<ParallaxLayer>().ToList();

        protected override void Start()
        {
            base.Start();
        }

        protected override void SetupLayer(int index)
        {
            TileParallaxLayer tileLayer = tileParallaxLayers[index];

            //Set the texture unit size of all the background pieces
            Sprite sprite = tileLayer.parallaxSpriteRenderer.sprite;
            Texture2D texture = sprite.texture;
            tileLayer.SetTextureUnitSize(new Vector2(texture.width / sprite.pixelsPerUnit, texture.height / sprite.pixelsPerUnit) * tileLayer.parallaxSpriteRenderer.transform.localScale);
        }

        protected override void ColorLayer(int index, Color newColor)
        {
            TileParallaxLayer tileLayer = tileParallaxLayers[index];

            //Color the sprite renderer
            SpriteRenderer renderer = tileLayer.parallaxSpriteRenderer;
            if (renderer != null) renderer.color = newColor;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
        }

        public override void MoveLayer(int index, Vector3 deltaMovement)
        {
            TileParallaxLayer tileLayer = tileParallaxLayers[index];

            //Move the layer
            tileLayer.parallaxSpriteRenderer.transform.position += new Vector3((deltaMovement.x * tileLayer.parallaxSpeed.x) + (tileLayer.automaticSpeed.x * Time.deltaTime), (deltaMovement.y * tileLayer.parallaxSpeed.y) + (tileLayer.automaticSpeed.y * Time.deltaTime), 0);

            //If the layer scrolls infinitely horizontally and has reached the end of the texture, offset the position
            if (tileLayer.infiniteHorizontal && Mathf.Abs(followCamera.transform.position.x - tileLayer.parallaxSpriteRenderer.transform.position.x) >= tileLayer.GetTextureUnitSize().x)
            {
                float offsetPositionX = (followCamera.transform.position.x - tileLayer.parallaxSpriteRenderer.transform.position.x) % tileLayer.GetTextureUnitSize().x;
                tileLayer.parallaxSpriteRenderer.transform.position = new Vector3(followCamera.transform.position.x + offsetPositionX, tileLayer.parallaxSpriteRenderer.transform.position.y);
            }

            //If the layer scrolls infinitely vertically and has reached the end of the texture, offset the position
            if (tileLayer.infiniteVertical && Mathf.Abs(followCamera.transform.position.y - tileLayer.parallaxSpriteRenderer.transform.position.y) >= tileLayer.GetTextureUnitSize().y)
            {
                float offsetPositionY = (followCamera.transform.position.y - tileLayer.parallaxSpriteRenderer.transform.position.y) % tileLayer.GetTextureUnitSize().y;
                tileLayer.parallaxSpriteRenderer.transform.position = new Vector3(tileLayer.parallaxSpriteRenderer.transform.position.x, tileLayer.parallaxSpriteRenderer.transform.position.y + offsetPositionY);
            }
        }
    }
}
