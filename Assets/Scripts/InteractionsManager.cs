using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionsManager : MonoBehaviour
{

    /// <summary>
    /// Test Interaction
    /// </summary>
    public void TestInteraction()
    {
        Debug.Log("This Is A Test Interaction!");
    }

    /// <summary>
    /// Fires a cannon in the direction of the enemies
    /// </summary>
    public void StartCannonFire()
    {
        Debug.Log("BAM! Weapon has been fired!");

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
