using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionsManager : MonoBehaviour
{


    [SerializeField] private GameObject cannonShell;
    [SerializeField] private Vector2 spawnPos;

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
    public void StartCannonFire()
    {
        Debug.Log("BAM! Weapon has been fired!");

        if(FindObjectOfType<CannonController>() != null){
            Debug.Log("Cannon Found!");
            FindObjectOfType<CannonController>().Fire();
        }

        //Example: deal 25 damage when firing weapon at enemy
        if(FindObjectOfType<EnemyController>() != null)
        {
            FindObjectOfType<EnemyController>().DealDamage(25);
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
