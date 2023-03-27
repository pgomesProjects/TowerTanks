using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerTransitionManager : MonoBehaviour
{
    [SerializeField, Tooltip("The index of the layer above the current one.")] private int nextLayerIndex;

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

            currentPlayer.previousLayer = currentPlayer.currentLayer;
            currentPlayer.currentLayer = changeIndex;

            Debug.Log("Previous Layer: " + (currentPlayer.previousLayer + 1));
            Debug.Log("Player On Layer: " + (currentPlayer.currentLayer + 1));

            if (currentPlayer.currentLayer > LevelManager.instance.totalLayers && currentPlayer.InBuildMode())
            {
                LevelManager.instance.AddGhostLayer();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController currentPlayer = collision.GetComponent<PlayerController>();
            int changeIndex = 0;

            //If player is above, keep the same layer number
            if (currentPlayer.transform.position.y > transform.position.y)
            {
                changeIndex = nextLayerIndex;
            }

            //If player is below, decrease the layer number
            if (currentPlayer.transform.position.y < transform.position.y)
            {
                changeIndex = nextLayerIndex - 1;
            }

            if(changeIndex != currentPlayer.currentLayer)
                currentPlayer.previousLayer = currentPlayer.currentLayer;

            currentPlayer.currentLayer = changeIndex;
            Debug.Log("Previous Layer: " + (currentPlayer.previousLayer + 1));
            Debug.Log("Player On Layer: " + (currentPlayer.currentLayer + 1));

            //If there are no players outside of the tank with a hammer, hide the ghost build layer
            if (!AnyPlayersOutsideWithHammer())
                LevelManager.instance.HideGhostLayer();

            //Destroy the ghost interactables from the previous layer, if any
            if (currentPlayer.previousLayer + 1 <= LevelManager.instance.totalLayers)
                LevelManager.instance.GetPlayerTank().GetLayerAt(currentPlayer.previousLayer).GetComponent<GhostInteractables>().DestroyGhostInteractables(currentPlayer);

            //If the player is not on the top of the tank, create ghost interactables
            if (currentPlayer.currentLayer + 1 <= LevelManager.instance.totalLayers)
                LevelManager.instance.GetPlayerTank().GetLayerAt(currentPlayer.currentLayer).GetComponent<GhostInteractables>().CreateGhostInteractables(currentPlayer);
        }
    }

    private bool AnyPlayersOutsideWithHammer()
    {
        foreach(var i in FindObjectsOfType<PlayerController>())
        {
            //If there are any players outside with a hammer
            if (i.currentLayer > LevelManager.instance.totalLayers && i.PlayerHasItem("Hammer"))
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
