using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerHealthManager : MonoBehaviour
{
    [SerializeField] private int health = 100;

    public void DealDamage(int dmg)
    {
        health -= dmg;

        Debug.Log("Layer " + (GetComponentInChildren<LayerManager>().GetNextLayerIndex() - 1) + " Health: " + health);

        CheckForDestroy();
    }

    private void CheckForDestroy()
    {
        //If the health of the layer is less than or equal to 0, destroy self
        if (health <= 0)
        {
            //Remove the layer from the total number of layers
            LevelManager.instance.totalLayers--;

            //Adjust the tank accordingly
            LevelManager.instance.AdjustLayerSystem((GetComponentInChildren<LayerManager>().GetNextLayerIndex() - 1));

            //Destroy the layer
            Destroy(gameObject);
        }
    }
}
