using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SpriteMaterialAssigner : MonoBehaviour
{
    private SpriteRenderer[] affectedSprites;
    public Material material;

    [Button("Test", ButtonSizes.Small), Tooltip("Assigns indicated material to all child sprites.")]
    public void MatSprites()
    {
        if (affectedSprites != null) return;
        if (material == null) return;

        affectedSprites = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sprite in affectedSprites)
        {
            sprite.material = material;
        }
    }
}
