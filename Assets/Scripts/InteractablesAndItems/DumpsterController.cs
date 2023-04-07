using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumpsterController : InteractableController
{
    [SerializeField, Tooltip("The GameObject for scrap.")] private GameObject scrapPrefab;

    private float scrapXOffset = 0.2f;  //The slight x offset of each scrap piece
    private float scrapHeight = 0.5f;   //The height of a scrap piece

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
        if (LevelManager.instance.CanPlayerAfford(LevelManager.instance.GetScrapValue()) && currentPlayerLockedIn != null)
        {
            Transform playerScrapHolder = currentPlayerLockedIn.transform.Find("ScrapHolder");

            //If the player has less than the max amount of scrap on them, allow them to grab more scrap
            if(playerScrapHolder.childCount < currentPlayerLockedIn.MaxScrapAmount())
            {
                GameObject newScrap = Instantiate(scrapPrefab, playerScrapHolder);
                newScrap.GetComponent<Rigidbody2D>().isKinematic = true;

                Vector2 scrapPos = newScrap.transform.localPosition;
                
                //Adds a slight x offset to any pieces that are added to the scrap tower
                if(playerScrapHolder.childCount > 1)
                    scrapPos.x = Random.Range(-scrapXOffset, scrapXOffset);

                scrapPos.y = scrapHeight * (playerScrapHolder.childCount - 1);
                newScrap.transform.localPosition = scrapPos;

                //Update the resources accordingly
                LevelManager.instance.UpdateResources(-LevelManager.instance.GetScrapValue());
                currentPlayerLockedIn.OnScrapUpdated();
            }
        }
    }
}
