using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
    public class GhostInteractables : MonoBehaviour
    {
        private List<PlayerController> playersBuilding = new List<PlayerController>();
        private LayerManager currentLayer;              //The current layer

        // Start is called before the first frame update
        void Awake()
        {
            currentLayer = GetComponent<LayerManager>();
        }

        /// <summary>
        /// Creates ghost interactables on the layer.
        /// </summary>
        /// <param name="currentPlayer">The current player trying to create the ghost interactables.</param>
        public void CreateGhostInteractables(PlayerController currentPlayer)
        {
            //If there is currently a player trying to build on this layer or the player is not in build mode, return
            if (!currentPlayer.InBuildMode())
                return;

            //If the player is already building in this layer, return
            if (playersBuilding.Contains(currentPlayer))
                return;

            //Debug.Log("Creating Ghost Interactables On " + gameObject.name);
            playersBuilding.Add(currentPlayer);

            //If there are already players building in this area, don't do anything extra
            if (playersBuilding.Count > 1)
                return;

            //Check the interactable spawners
            foreach (var spawner in currentLayer.GetComponentsInChildren<InteractableSpawner>())
            {
                //If there is not an interactable spawned, show the ghost interactables
                if (!spawner.IsInteractableSpawned())
                {
                    //If the player build is not holding scrap, only show the dumpster
                    if (!currentPlayer.IsHoldingScrap())
                        spawner.SetCurrentGhostIndex((int)DEPRECATEDINTERACTABLETYPE.DUMPSTER);
                    else
                    {
                        //If the spawner is on the left, show the engine on start
                        if (spawner.transform.position.x < 0)
                            spawner.SetCurrentGhostIndex((int)DEPRECATEDINTERACTABLETYPE.ENGINE);
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
            if (!playersBuilding.Contains(currentPlayer))
                return;

            //Debug.Log("Destroy Ghost Interactables On " + gameObject.name);

            playersBuilding.Remove(currentPlayer);

            Debug.Log("Players Building Count: " + playersBuilding.Count);

            //If there are still players building here, don't destroy the interactables
            if (playersBuilding.Count > 0)
                return;

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
        }

        public bool CurrentPlayerIsBuilding(PlayerController currentPlayer) => playersBuilding.Contains(currentPlayer);
    }
}
