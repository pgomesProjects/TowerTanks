using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionsManager : MonoBehaviour
{


    [SerializeField] private GameObject cannonShell;
    [SerializeField] private Vector2 spawnPos;
    internal PlayerController currentPlayer;

    /// <summary>
    /// Test Interaction
    /// </summary>
    public void TestInteraction()
    {
        Debug.Log("This Is A Test Interaction!");
    }

    public void SpawnShell(){
        if (!ShellMachine.IsShellActive())
        {
            GameObject newShell = Instantiate(cannonShell, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
            float randomX = Random.Range(0.25f, 0.4f);
            newShell.GetComponent<Rigidbody2D>().AddForce(new Vector2(randomX, 1) * 1500);

            ShellMachine.SetShellActive(true);
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
                if(currentPlayer.GetPlayerItem().GetComponent<ShellController>() != null)
                {

                    //Get rid of the player's item
                    Destroy(currentPlayer.GetPlayerItem());

                    Debug.Log("BAM! Weapon has been fired!");

                    //Fire the cannon
                    if (cannon != null)
                    {
                        Debug.Log("Cannon Found!");
                        cannon.Fire();
                        ShellMachine.SetShellActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Fills coal so that the tank can continue moving
    /// </summary>
    public void StartCoalFill()
    {
        Debug.Log("Coal Has Been Added To The Furnace!");
    }
}
