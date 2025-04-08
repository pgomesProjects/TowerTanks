using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
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

                // Debug.Log("Previous Layer: " + (currentPlayer.previousLayer + 1));
                // Debug.Log("Player On Layer: " + (currentPlayer.currentLayer + 1));

                if (currentPlayer.IsPlayerOutsideTank() && currentPlayer.InBuildMode() && currentPlayer.IsHoldingScrap())
                {
                    LevelManager.Instance.AddGhostLayer();
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

                if (changeIndex != currentPlayer.currentLayer)
                    currentPlayer.previousLayer = currentPlayer.currentLayer;

                currentPlayer.currentLayer = changeIndex;
                //Debug.Log("Previous Layer: " + (currentPlayer.previousLayer + 1));
                //Debug.Log("Player On Layer: " + (currentPlayer.currentLayer + 1));

                if (!AnyPlayersOutsideInBuildMode())
                    LevelManager.Instance.HideGhostLayer();

                //Destroy the ghost interactables from the previous layer, if any
                if (currentPlayer.previousLayer + 1 <= LevelManager.Instance.totalLayers) { }
                //LevelManager.Instance.GetPlayerTank().GetLayerAt(currentPlayer.previousLayer).GetComponent<GhostInteractables>().DestroyGhostInteractables(currentPlayer);

                //If the player is not on the top of the tank, create ghost interactables
                if (currentPlayer.currentLayer + 1 <= LevelManager.Instance.totalLayers) { }
                //LevelManager.Instance.GetPlayerTank().GetLayerAt(currentPlayer.currentLayer).GetComponent<GhostInteractables>().CreateGhostInteractables(currentPlayer);
            }
        }

        private bool AnyPlayersOutsideInBuildMode()
        {
            foreach (var player in FindObjectsOfType<PlayerController>())
            {
                //If there are any players outside in build mode
                if (player.IsPlayerOutsideTank() && player.InBuildMode())
                    return true;
            }
            return false;
        }

        public int GetNextLayerIndex() => nextLayerIndex;
        public void SetLayerIndex(int index) => nextLayerIndex = index;
    }
}
