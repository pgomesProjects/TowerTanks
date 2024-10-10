using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TowerTanks.Scripts.Deprecated
{
    public class InteractableController : MonoBehaviour
    {
        public UnityEvent interactEvent;
        public UnityEvent cancelEvent;

        [SerializeField, Tooltip("If true, the player is locked into place when interacting.")] protected bool lockPlayerIntoInteraction;
        [SerializeField, Tooltip("The highlight on the interactable that indicates which player is interacting with it.")] protected SpriteRenderer highlight;
        [SerializeField, Tooltip("The canvas that indicates whether a player is locked into it.")] protected GameObject lockedInteractionCanvas;
        [SerializeField, Tooltip("If true, the player immediately uses the interactable on start.")] protected bool interactOnStart = true;

        [SerializeField, Tooltip("The percent increase on every use of the item that it will break.")] protected float breakPercentIncreaseOnUse;
        [SerializeField, Tooltip("The value of the interactable")] private int value;
        [SerializeField, Tooltip("The percentage gained back when sold.")] private float sellPercent;
        [SerializeField, Tooltip("If true, the interactable can be sold.")] protected bool canBeSold = true;

        protected List<int> playersColliding = new List<int>();
        protected PlayerController currentPlayerLockedIn;

        protected bool interactionActive;
        protected bool firstInteractionComplete = false;

        protected PlayerController currentPlayer;


        private void Start()
        {
            interactionActive = false;
        }

        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                ShowPlayerInteraction(collision.GetComponent<PlayerController>());
            }
        }

        protected void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                PlayerController currentInteractingPlayer = collision.GetComponent<PlayerController>();

                //If the player is not in the list of interactable players 
                if (!playersColliding.Contains(currentInteractingPlayer.GetPlayerIndex()))
                {
                    ShowPlayerInteraction(currentInteractingPlayer);
                }
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                PlayerController currentInteractingPlayer = collision.GetComponent<PlayerController>();

                if (currentPlayerLockedIn != null && currentPlayerLockedIn == currentInteractingPlayer)
                {
                    //If the locked player leaves the trigger for the interactable, unlock them
                    LockPlayer(currentInteractingPlayer, false);
                }
                else
                {
                    if (playersColliding.Contains(currentInteractingPlayer.GetPlayerIndex()))
                    {
                        playersColliding.Remove(currentInteractingPlayer.GetPlayerIndex());
                        currentInteractingPlayer.DisplayPlayerAction(PlayerController.PlayerActions.NONE);

                        //Player can no longer interact with this item
                        currentInteractingPlayer.currentInteractableItem = null;
                    }
                }
            }
        }

        /// <summary>
        /// Show the current player trying to interact that they can interact with the interactable.
        /// </summary>
        /// <param name="currentInteractingPlayer">The player trying to interact with the interactable.</param>
        private void ShowPlayerInteraction(PlayerController currentInteractingPlayer)
        {
            if (currentPlayerLockedIn == null && (!currentInteractingPlayer.InBuildMode() || transform.CompareTag("Dumpster")))
            {
                playersColliding.Add(currentInteractingPlayer.GetPlayerIndex());
                currentInteractingPlayer.DisplayPlayerAction(PlayerController.PlayerActions.INTERACTING);
                //Tell the player that this is the item that they can interact with
                currentInteractingPlayer.currentInteractableItem = this;
            }
        }

        public void OnInteraction(PlayerController playerInteracting)
        {
            //If the object is interacted with and no one else is locked in, grab the function from the inspector that will decide what to do
            if (currentPlayerLockedIn == null)
            {
                //Debug.Log("Current Player Interacting: " + playerInteracting.name);
                SetCurrentActivePlayer(playerInteracting);

                if (lockPlayerIntoInteraction)
                {
                    //Debug.Log("Interaction Active: " + interactionActive);
                    //If there is not an interaction active
                    if (!interactionActive)
                    {
                        playersColliding.Clear();
                        LockPlayer(playerInteracting, true);
                        interactEvent.Invoke();
                    }
                }
                else
                {
                    interactEvent.Invoke();
                }
            }
        }

        public void OnEndInteraction(PlayerController playerInteracting)
        {
            //If the interactable locks the player and the player trying to end the interaction is the player locked in
            if (lockPlayerIntoInteraction && playerInteracting == currentPlayerLockedIn)
            {
                //If the interaction is currently active, release the player
                if (interactionActive)
                {
                    LockPlayer(playerInteracting, false);
                    playersColliding.Add(playerInteracting.GetPlayerIndex());
                    interactEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// Function called when the use button is pressed when locked into the interactable.
        /// </summary>
        public virtual void OnUseInteractable()
        {
            if (!firstInteractionComplete)
                Invoke("OnFirstInteractionComplete", 0.1f);
        }

        private void OnFirstInteractionComplete()
        {
            firstInteractionComplete = true;
        }

        public void OnCancel()
        {
            //If the object is interacted with, grab the function from the inspector that will decide what to do when canceling
            if (lockPlayerIntoInteraction)
            {
                //If there is an interaction active
                if (interactionActive)
                {
                    Debug.Log("Canceling Interaction");
                    interactionActive = false;
                    currentPlayerLockedIn.SetPlayerMove(true);
                    cancelEvent.Invoke();
                }
            }
        }

        public virtual void LockPlayer(PlayerController currentPlayer, bool lockPlayer)
        {
            if (lockPlayer)
            {
                //Debug.Log("Locking Player...");
                interactionActive = true;
                if (currentPlayer != null)
                    currentPlayer.SetPlayerMove(false);
                currentPlayerLockedIn = currentPlayer;

                highlight.color = currentPlayerLockedIn.GetPlayerColor();
                highlight.gameObject.SetActive(true);
                lockedInteractionCanvas.SetActive(true);
            }
            else
            {
                //Debug.Log("Unlocking Player...");
                interactionActive = false;
                if (currentPlayer != null)
                    currentPlayer.SetPlayerMove(true);
                currentPlayerLockedIn = null;

                highlight.gameObject.SetActive(false);
                lockedInteractionCanvas.SetActive(false);
                firstInteractionComplete = false;

                currentPlayer.CancelProgressBar();
            }
        }

        public virtual void UnlockAllPlayers()
        {
            Debug.Log("Unlocking Player...");
            interactionActive = false;
            if (currentPlayer != null)
                currentPlayer.SetPlayerMove(true);
            currentPlayerLockedIn = null;

            highlight.gameObject.SetActive(false);
            lockedInteractionCanvas.SetActive(false);
        }

        public PlayerController GetLockedInPlayer() => currentPlayerLockedIn;

        public void Sell()
        {
            if (currentPlayerLockedIn == null)
            {
                LevelManager.Instance.UpdateResources(Mathf.CeilToInt(value * (sellPercent / 100f)));
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (currentPlayerLockedIn != null && lockPlayerIntoInteraction)
            {
                LockPlayer(currentPlayerLockedIn, false);
            }
        }

        public bool IsInteractionActive() => interactionActive;
        public bool AnyPlayersLockedIn() => currentPlayerLockedIn != null;
        public bool CanBeSold() => canBeSold;

        public void SetCurrentActivePlayer(PlayerController player)
        {
            currentPlayer = player;
        }
    }
}
