using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteRandomizer : MonoBehaviour
{
    [Header("Randomize Sprite")]
    public SpriteRenderer renderer;
    public Sprite[] possibleSprites;

    [Header("Randomize Scale")]
    public float minScale = 1f;
    public float maxScale = 1f;

    private void Awake()
    {
        if (possibleSprites.Length > 0)
        {
            Sprite sprite = possibleSprites[(int)Random.Range(0, possibleSprites.Length)];
            renderer.sprite = sprite;
        }

        if (minScale != maxScale)
        {
            float random = Random.Range(minScale, maxScale);
            transform.localScale *= random;
        }
    }
}
