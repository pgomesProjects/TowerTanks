using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SpriteOverlay : MonoBehaviour
    {
        public Color overlayColor;

        private SpriteRenderer[] affectedSprites;
        private Color[] defaultColors;

        private void Start()
        {
            affectedSprites = GetComponentsInChildren<SpriteRenderer>();
            defaultColors = new Color[affectedSprites.Length];

            for (int i = 0; i < affectedSprites.Length; i++)
            {
                defaultColors[i] = affectedSprites[i].color;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (affectedSprites == null) return;
            if (affectedSprites.Length != 0)
            {
                foreach (SpriteRenderer sprite in affectedSprites)
                {
                    sprite.color = overlayColor;
                }
            }
        }

        public void ResetOverlay()
        {
            if (affectedSprites == null) return;
            if (affectedSprites.Length != 0)
            {
                for (int i = 0; i < affectedSprites.Length; i++)
                {
                    affectedSprites[i].color = defaultColors[i];
                }
            }
        }

        public void OnDisable()
        {
            ResetOverlay();
        }
    }
}
