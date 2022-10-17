using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionsManager : MonoBehaviour
{

    [SerializeField] private GameObject cannonShell;
    internal PlayerController currentPlayer;

    /// <summary>
    /// Test Interaction
    /// </summary>
    public void TestInteraction()
    {
        Debug.Log("This Is A Test Interaction!");
    }

    public void SpawnShell(GameObject shellStation){
        //If there is a player
        if (currentPlayer != null)
        {
            //If the player does not have an item
            if (currentPlayer.GetPlayerItem() == null)
            {
                //Spawn a shell at the spawn point
                Transform spawnPoint = shellStation.transform.Find("SpawnPoint");
                GameObject newShell = Instantiate(cannonShell, new Vector3(spawnPoint.position.x, spawnPoint.position.y, 0), Quaternion.identity);

                //Force the player to pick up the item
                currentPlayer.MarkClosestItem(newShell.GetComponent<Item>());
                currentPlayer.PickupItem();
            }
        }
    }

    /// <summary>
    /// Fires a cannon in the direction of the enemies
    /// </summary>
    public void StartCannonFire(CannonController cannon)
    {
        //If there is a player
        if(currentPlayer != null)
        {
            //If the player has an item
            if (currentPlayer.GetPlayerItem() != null)
            {
                //If the player's item is a shell
                if(currentPlayer.GetPlayerItem().CompareTag("Shell"))
                {
                    //Fire the cannon
                    if (cannon != null)
                    {
                        //Get rid of the player's item
                        Debug.Log("BAM! Weapon has been fired!");
                        currentPlayer.DestroyItem();
                        cannon.Fire();

                        //Shake the camera
                        CameraEventController.instance.ShakeCamera(5f, 0.1f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Fills coal so that the tank can continue moving
    /// </summary>
    public void StartCoalFill(CoalController coalController)
    {
        //If there is a player
        if (currentPlayer != null)
        {
            float timeToFillCoal = 5;

            //If there is no coal in the furnace, make the task of refilling it twice as long
            if (!coalController.HasCoal())
                timeToFillCoal *= 2;

            //Start a progress bar on the player
            currentPlayer.StartProgressBar(timeToFillCoal, () => FillCoal(coalController, 50));
        }
    }

    private void FillCoal(CoalController coalController, float percent)
    {
        //Add a percentage of the necessary coal to the furnace
        Debug.Log("Coal Has Been Added To The Furnace!");
        coalController.AddCoal(percent);
    }

    /// <summary>
    /// Cancels the filling of coal when the player lets go of the interact button
    /// </summary>
    public void CancelCoalFill(CoalController coalController)
    {
        //If there is a player
        if (currentPlayer != null)
        {
            currentPlayer.CancelProgressBar();
        }
    }
}
