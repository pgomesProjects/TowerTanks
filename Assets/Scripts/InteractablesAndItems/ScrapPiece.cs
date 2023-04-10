using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrapPiece : MonoBehaviour
{
    [SerializeField, Tooltip("The time it takes for the scrap piece to return to the resources pile if dropped.")] private float scrapLifeDuration;
    [SerializeField, Tooltip("The amount of damage that the scrap deals if it hits the enemy tank.")] private int damage;

    /// <summary>
    /// Destroys the scrap after a set amount of time.
    /// </summary>
    public void DespawnScrap()
    {
        Invoke("OnDespawned", scrapLifeDuration);
    }

    /// <summary>
    /// The logic for when the scrap piece is despawned.
    /// </summary>
    private void OnDespawned()
    {
        LevelManager.instance.UpdateResources(LevelManager.instance.GetScrapValue());  //Add back to scrap on despawned
        Destroy(gameObject);
    }

    public int GetDamage() => damage;
}
