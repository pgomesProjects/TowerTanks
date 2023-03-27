using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostInteractables : MonoBehaviour
{
    private PlayerController currentPlayerBuilding; //The current player that is building on this layer
    private LayerManager currentLayer;              //The current layer

    // Start is called before the first frame update
    void Start()
    {
        currentPlayerBuilding = null;
        currentLayer = GetComponent<LayerManager>();
    }

    /// <summary>
    /// Creates ghost interactables on the layer.
    /// </summary>
    /// <param name="currentPlayer">The current player trying to create the ghost interactables.</param>
    public void CreateGhostInteractables(PlayerController currentPlayer)
    {
        //If there is currently a player trying to build on this layer or the player is not in build mode, return
        if (currentPlayerBuilding != null || !currentPlayer.InBuildMode())
            return;

        Debug.Log("Creating Ghost Interactables On " + gameObject.name);
        currentPlayerBuilding = currentPlayer;

        //Check the interactable spawners
        foreach (var spawner in currentLayer.GetComponentsInChildren<InteractableSpawner>())
        {
            //If there is not an interactable spawned, show the ghost interactables
            if (!spawner.IsInteractableSpawned())
            {
                //If the player build is not holding scrap, only show the dumpster
                if (!currentPlayerBuilding.IsHoldingScrap())
                    spawner.SetCurrentGhostIndex((int)INTERACTABLETYPE.DUMPSTER);
                else
                {
                    //If the spawner is on the left, show the engine on start
                    if (spawner.transform.position.x < 0)
                        spawner.SetCurrentGhostIndex((int)INTERACTABLETYPE.ENGINE);
                }

                FindObjectOfType<InteractableSpawnerManager>().ShowNewGhostInteractable(spawner);
            }
        }
    }


    /// <summary>
    /// Destroys the ghost interactables.
    /// </summary>
    /// <param name="currentPlayer">The current player trying to destroy the ghost interactables.</param>
    public void DestroyGhostInteractables(PlayerController currentPlayer)
    {
        //If the current player trying to destroy the ghost interactables is not the one building, return
        if (currentPlayer != currentPlayerBuilding)
            return;

        Debug.Log("Destroy Ghost Interactables On " + gameObject.name);

        //Check the interactable spawners
        foreach (var spawner in currentLayer.GetComponentsInChildren<InteractableSpawner>())
        {
            //If there is an interactable spawned
            if (spawner.IsInteractableSpawned())
            {
                //If there is a ghost interactable, destroy it
                if (spawner.transform.GetChild(1).CompareTag("GhostObject"))
                {
                    Destroy(spawner.transform.GetChild(1).gameObject);
                }
            }
        }

        currentPlayerBuilding = null;
    }
}
