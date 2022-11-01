using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    [SerializeField] private int nextLayerIndex;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController currentPlayer = collision.GetComponent<PlayerController>();
            int changeIndex = 0;

            //If player is coming from above, decrease the layer number
            if (currentPlayer.transform.position.y > transform.position.y)
            {
                changeIndex = nextLayerIndex - 1;
            }
            //If player is coming from below, increase the layer number
            else if (currentPlayer.transform.position.y < transform.position.y)
            {
                changeIndex = nextLayerIndex;
            }

            currentPlayer.currentLayer = changeIndex;
            Debug.Log("Player On Layer: " + currentPlayer.currentLayer);

            if (currentPlayer.currentLayer > LevelManager.instance.totalLayers && currentPlayer.IsPlayerHoldingHammer())
            {
                LevelManager.instance.AddGhostLayer();
            }
            else if(currentPlayer.currentLayer <= LevelManager.instance.totalLayers && currentPlayer.IsPlayerHoldingHammer())
            {
                LevelManager.instance.CheckInteractablesOnLayer(currentPlayer.currentLayer);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController currentPlayer = collision.GetComponent<PlayerController>();
            int changeIndex = 0;

            //If player is above, make sure the layer number is the same
            if (currentPlayer.transform.position.y > transform.position.y)
            {
                changeIndex = nextLayerIndex;
            }
            //If player is below, decrease the layer number
            if (currentPlayer.transform.position.y < transform.position.y)
            {
                changeIndex = nextLayerIndex - 1;
            }

            currentPlayer.currentLayer = changeIndex;
            Debug.Log("Player On Layer: " + currentPlayer.currentLayer);

            //If there are no players outside of the tank with a hammer, hide the ghost build layer
            if (!AnyPlayersOutsideWithHammer())
                LevelManager.instance.HideGhostLayer();
        }
    }

    private bool AnyPlayersOutsideWithHammer()
    {
        foreach(var i in FindObjectsOfType<PlayerController>())
        {
            //If there are any players outside with a hammer
            if (i.currentLayer > LevelManager.instance.totalLayers && i.IsPlayerHoldingHammer())
                return true;
        }

        return false;
    }

    public int GetNextLayerIndex()
    {
        return nextLayerIndex;
    }

    public void SetLayerIndex(int index)
    {
        nextLayerIndex = index;
    }
}
