using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [ExecuteInEditMode]
    public class OutlineCreator : MonoBehaviour
    {
        [SerializeField, Tooltip("The size of the outline.")] private float outlineThickness;

        [Button]
        public void CreateOutline()
        {
            SpawnChildren(outlineThickness);
        }

        [Button]
        public void RemoveOutline()
        {
            ClearOutlines();
        }


        private List<Transform> allOutlines = new List<Transform>();

        private void SpawnChildren(float outline)
        {
            ClearOutlines();

            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                Transform newParent = Instantiate(new GameObject(), spriteRenderer.transform).transform;
                newParent.name = "Outlines";
                allOutlines.Add(newParent);

                CreateNewOutlinePiece(spriteRenderer, newParent, new Vector2(outlineThickness, 0));
                CreateNewOutlinePiece(spriteRenderer, newParent, new Vector2(-outlineThickness, 0));
                CreateNewOutlinePiece(spriteRenderer, newParent, new Vector2(0, -outlineThickness));
                CreateNewOutlinePiece(spriteRenderer, newParent, new Vector2(0, outlineThickness));
            }
        }

        private void CreateNewOutlinePiece(SpriteRenderer spriteRenderer, Transform newParent, Vector2 position)
        {
            SpriteRenderer newSprite = Instantiate(spriteRenderer, newParent);
            newSprite.transform.localScale = Vector3.one;
            newSprite.transform.localPosition = Vector3.zero;
            newSprite.transform.localEulerAngles = Vector3.zero;
            newSprite.sortingOrder = -1;
            newSprite.name = newSprite.name.Replace("(Clone)", "_Outline");
            newSprite.color = Color.white;
            newSprite.transform.localPosition += (Vector3)position;
        }

        private void ClearOutlines()
        {
            foreach (Transform trans in allOutlines)
            {
                if(trans != null)
                    DestroyImmediate(trans.gameObject);
            }

            allOutlines.Clear();
        }
    }
}
