using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageDiegeticController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    [SerializeField, Tooltip("The state of the damage diegetic in order from least to most damaged.")] private Sprite[] damageSprites;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();   
    }

    /// <summary>
    /// Updates the diegetic of the sprite based on the damage they have.
    /// </summary>
    /// <param name="damagePercent">The amount of damage the object has.</param>
    public void UpdateDiegetic(float damagePercent)
    {
        //Debug.Log("Current Damage Index: " + Mathf.FloorToInt(damageSprites.Length * damagePercent).ToString());
        int currentIndex = damageSprites.Length - Mathf.FloorToInt(damageSprites.Length * damagePercent);

        currentIndex = Mathf.Clamp(currentIndex, 0, damageSprites.Length - 1);

        spriteRenderer.sprite = damageSprites[currentIndex];
    }
}
