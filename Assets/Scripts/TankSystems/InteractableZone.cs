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
        protected virtual void Start()
        {
            playerIsColliding = false;
            interactable = GetComponentInParent<TankInteractable>();
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (players.Count > 0) playerIsColliding = true;
            else playerIsColliding = false;
        }

        public virtual void Interact(GameObject playerID) //Try to operate the thing
        {
            if (interactable == null) return;
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
                    //If the player is locked into another interactable, return
                    if (player.currentInteractable != null) return;

                    players.Add(player.gameObject);
                    player.currentZone = this;
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
                    //If the player is locked into another interactable, return
                    if (player.currentInteractable != null) return;

                    players.Add(player.gameObject);
                    player.currentZone = this;
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
                    //If the player is locked into another interactable, return
                    if (player.currentInteractable != null) return;

                    if (players.Contains(player.gameObject))
                    {
                        players.Remove(player.gameObject);
                        if (player.currentZone == this) player.currentZone = null;
                    }
                }
            }
        }
    }
}
