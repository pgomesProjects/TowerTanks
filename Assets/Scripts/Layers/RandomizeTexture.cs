using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeTexture : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;

    private SpriteRenderer spriteRenderer;

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        int randomTexture = Random.Range(0, sprites.Length);
        spriteRenderer.sprite = sprites[randomTexture];
    }
}
