using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumpsterController : InteractableController
{
    [SerializeField, Tooltip("The GameObject for scrap.")] private GameObject scrapPrefab;
    [SerializeField, Tooltip("The offset the player position has when they lock into the dumpster.")] private Vector2 dumpsterPositionOffset;

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
        if (LevelManager.Instance.CanPlayerAfford(LevelManager.Instance.GetScrapValue()) && currentPlayerLockedIn != null)
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
                LevelManager.Instance.UpdateResources(-LevelManager.Instance.GetScrapValue());
                currentPlayerLockedIn.OnScrapUpdated();
            }
        }
    }

    public override void LockPlayer(PlayerController currentPlayer, bool lockPlayer)
    {
        base.LockPlayer(currentPlayer, lockPlayer);
        
        //If the player is unlocked from the scrap holder and is holding scrap, automatically put them in build mode
        if (!lockPlayer)
        {
            if (currentPlayer.IsHoldingScrap())
            {
                currentPlayer.SetBuildMode(true);
                //currentPlayer.HideInteractionPrompt();
            }
        }

        currentPlayer.GetComponent<Animator>().SetBool("IsGathering", lockPlayer); //Adjust animation state
        AdjustPlayerPositionOnInteract(currentPlayer, lockPlayer);
    }

    public override void UnlockAllPlayers()
    {
        base.UnlockAllPlayers();
        currentPlayer.GetComponent<Animator>().SetBool("IsGathering", false); //Adjust animation state
        AdjustPlayerPositionOnInteract(currentPlayer, false);
    }

    /// <summary>
    /// Adjusts the position of the player depending on if they're locking into / unlocking from the dumpster.
    /// </summary>
    /// <param name="currentPlayer">The player interacting with the dumpster.</param>
    /// <param name="isMovingToDumpster">If true, the player is locked into the dumpster. If false, they are leaving.</param>
    private void AdjustPlayerPositionOnInteract(PlayerController currentPlayer, bool isMovingToDumpster)
    {
        if (isMovingToDumpster)
        {
            //Remove gravity so that the player stays still
            currentPlayer.GetComponent<Rigidbody2D>().gravityScale = 0f;

            currentPlayer.transform.position = new Vector2(transform.position.x + dumpsterPositionOffset.x, transform.position.y + dumpsterPositionOffset.y);
        }
        else
        {
            currentPlayer.GetComponent<Rigidbody2D>().gravityScale = currentPlayer.GetDefaultGravity();
        }
    }
}
