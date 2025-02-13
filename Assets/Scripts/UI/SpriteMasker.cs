using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class SpriteMasker : MonoBehaviour
    {
        private SpriteRenderer[] affectedSprites;

        [Button("Test", ButtonSizes.Small), Tooltip("Masks all child sprites.")]
        public void MaskSprites()
        {
            if (affectedSprites != null) return;

            affectedSprites = GetComponentsInChildren<SpriteRenderer>();

            foreach(SpriteRenderer sprite in affectedSprites)
            {
                if (sprite.transform.gameObject.name != "Overlay")
                {
                    string name = sprite.transform.gameObject.name + " (Mask)";
                    GameObject mask = new GameObject(name);
                    mask.transform.position = sprite.transform.position;
                    mask.transform.rotation = sprite.transform.rotation;
                    
                    mask.transform.SetParent(sprite.transform);
                    mask.transform.localScale = new Vector3(1, 1, 1);

                    SpriteMask _mask = mask.AddComponent<SpriteMask>();
                    _mask.sprite = sprite.sprite;
                }
            }
        }

        public void Start()
        {
            MaskSprites();
        }
    }
}
