using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class InteractableZone : MonoBehaviour
    {
        private TankInteractable interactable;
        public bool playerIsColliding; //true if any player is currently inside the zone
        public List<GameObject> players = new List<GameObject>(); //players currently inside this interaction zone

        // Start is called before the first frame update
        void Start()
        {
            playerIsColliding = false;
            interactable = GetComponentInParent<TankInteractable>();
        }

        // Update is called once per frame
        void Update()
        {
            if (players.Count > 0) playerIsColliding = true;
            else playerIsColliding = false;
        }

        public void Interact(GameObject playerID) //Try to operate the thing
        {
            if (players.Contains(playerID) && interactable.seat != null)
            {
                if (interactable.hasOperator) //Already someone on it
                {
                    interactable.Exit(true);
                }

                interactable.LockIn(playerID);
                players.Remove(playerID);
                PlayerMovement currentPlayer = playerID.GetComponent<PlayerMovement>();
                currentPlayer.currentZone = null;
                currentPlayer.GetCharacterHUD()?.SetButtonPrompt(GameAction.Interact, false);
                //else GameManager.Instance.AudioManager.Play("InvalidAlert");
            }
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                PlayerMovement player = collider.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    //Debug.Log("Found " + player);
                    players.Add(player.gameObject);
                    player.currentZone = this;
                    player.GetCharacterHUD()?.SetButtonPrompt(GameAction.Interact, true);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D collider)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                PlayerMovement player = collider.GetComponent<PlayerMovement>();
                if (player != null && player.currentZone != this && !players.Contains(player.gameObject) && !interactable.hasOperator)
                {
                    players.Add(player.gameObject);
                    player.currentZone = this;
                    player.GetCharacterHUD()?.SetButtonPrompt(GameAction.Interact, true);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collider)
        {
            if (collider.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                PlayerMovement player = collider.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    //Debug.Log("Lost " + player);
                    if (players.Contains(player.gameObject))
                    {
                        players.Remove(player.gameObject);
                        if (player.currentZone == this) player.currentZone = null;
                        player.GetCharacterHUD()?.SetButtonPrompt(GameAction.Interact, false);
                    }
                }
            }
        }
    }
}
