using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrapPiece : MonoBehaviour
{
    [SerializeField, Tooltip("The time it takes for the scrap piece to return to the resources pile if dropped.")] private float scrapLifeDuration;

    /// <summary>
    /// Destroys the scrap after a set amount of time.
    /// </summary>
    public void DespawnScrap()
    {
        Destroy(gameObject, scrapLifeDuration);
    }

    private void OnDestroy()
    {
        if(LevelManager.instance != null)
            LevelManager.instance.UpdateResources(LevelManager.instance.GetScrapValue());  //Add back to scrap on destroy
    }
}
