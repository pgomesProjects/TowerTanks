using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionsManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void TestInteraction()
    {
        Debug.Log("This Is A Test Interaction!");
    }

    public void StartCannonFire()
    {
        Debug.Log("BAM! Weapon has been fired!");
    }

    public void StartCoalFill()
    {
        Debug.Log("Coal Has Been Added To The Furnace!");
    }
}
