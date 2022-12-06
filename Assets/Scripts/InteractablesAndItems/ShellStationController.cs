using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellStationController : InteractableController
{
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform spawnPoint;

    /// <summary>
    /// Spawns a shell and puts it in the player's hands
    /// </summary>
    public void SpawnShell()
    {
        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.GRABSHELL)
            {
                //If there is a player
                if (currentPlayer != null)
                {
                    //If the player does not have an item
                    if (currentPlayer.GetPlayerItem() == null)
                    {
                        //Spawn a shell at the spawn point
                        GameObject newShell = Instantiate(shellPrefab, new Vector3(spawnPoint.position.x, spawnPoint.position.y, 0), Quaternion.identity);

                        //Force the player to pick up the item
                        currentPlayer.MarkClosestItem(newShell.GetComponent<Item>());
                        currentPlayer.PickupItem();
                    }
                }
            }
        }
        else
        {
            //If there is a player
            if (currentPlayer != null)
            {
                //If the player does not have an item
                if (currentPlayer.GetPlayerItem() == null)
                {
                    //Spawn a shell at the spawn point
                    GameObject newShell = Instantiate(shellPrefab, new Vector3(spawnPoint.position.x, spawnPoint.position.y, 0), Quaternion.identity);

                    //Force the player to pick up the item
                    currentPlayer.MarkClosestItem(newShell.GetComponent<Item>());
                    currentPlayer.PickupItem();
                }
            }
        }
    }
}
