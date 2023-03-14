using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumpsterController : InteractableController
{
    [SerializeField, Tooltip("The GameObject for scrap.")] private GameObject scrapPrefab;
    [SerializeField, Tooltip("Value of scrap piece.")] private int scrapValue = 10;

    private Vector2 randomOffset = new Vector2(1, 1);

    /// <summary>
    /// Tries to grab scrap from the dumpster when used.
    /// </summary>
    public override void OnUseInteractable()
    {
        GrabScrap();
    }

    /// <summary>
    /// Grabs scrap from the dumpster.
    /// </summary>
    private void GrabScrap()
    {
        //If the player can afford to grab scrap, grab scrap
        if (LevelManager.instance.CanPlayerAfford(scrapValue) && currentPlayerLockedIn != null)
        {
            Transform playerScrapHolder = currentPlayerLockedIn.transform.Find("ScrapHolder");
            GameObject newScrap = Instantiate(scrapPrefab, playerScrapHolder);

            //If the player has more than one scrap piece on them, add a small offset
            if (playerScrapHolder.childCount > 1)
            {
                Vector2 scrapPos = newScrap.transform.localPosition;
                scrapPos.x = Random.Range(-randomOffset.x, randomOffset.x);
                scrapPos.y = Random.Range(0, randomOffset.y);
                newScrap.transform.localPosition = scrapPos;
            }

            //Update the resources accordingly
            LevelManager.instance.UpdateResources(-scrapValue);
        }
    }
}
