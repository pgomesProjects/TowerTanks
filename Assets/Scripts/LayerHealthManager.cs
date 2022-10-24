using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerHealthManager : MonoBehaviour
{
    [SerializeField] private int health = 100;
    [SerializeField] private int destroyResourcesValue = 80;

    public void DealDamage(int dmg)
    {
        health -= dmg;

        if (transform.CompareTag("Layer"))
        {
            Debug.Log("Layer " + (GetComponentInChildren<LayerManager>().GetNextLayerIndex() - 1) + " Health: " + health);
        }
        
        //Check to see if the layer will be destryoed
        CheckForDestroy();
    }


    public void CheckForFireSpawn(float chanceToCatchFire)
    {
        Debug.Log("Checking For Fire...");

        //If the layer is already on fire, ignore everything else
        if (GetComponentInChildren<FireBehavior>() != null && GetComponentInChildren<FireBehavior>().IsLayerOnFire())
            return;

        float percent = Random.Range(0, 100);

        //If the chance to catch fire has been met, turn on the fire diegetics
        if(percent < chanceToCatchFire)
        {
            transform.Find("FireDiegetics").gameObject.SetActive(true);
        }
    }

    private void CheckForDestroy()
    {
        //If the health of the layer is less than or equal to 0, destroy self
        if (health <= 0)
        {
            if (transform.CompareTag("Layer"))
            {
                //Remove the layer from the total number of layers
                LevelManager.instance.totalLayers--;

                //Adjust the tank accordingly
                LevelManager.instance.AdjustLayerSystem((GetComponentInChildren<LayerManager>().GetNextLayerIndex() - 1));

                //Add to player resources
                LevelManager.instance.AddResources(destroyResourcesValue);
            }

            //If an enemy layer is destroyed, tell the tank that the layer was destroyed
            else if (transform.CompareTag("EnemyLayer"))
            {
                GetComponentInParent<EnemyController>().EnemyLayerDestroyed();
            }

            //Destroy the layer
            Destroy(gameObject);
        }
    }
}
