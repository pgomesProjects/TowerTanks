using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableController : MonoBehaviour
{
    public UnityEvent interactEvent;
    public UnityEvent cancelEvent;

    protected List<int> playersColliding = new List<int>();
    protected PlayerController currentPlayerLockedIn;
    protected bool interactionActive;
    [SerializeField]protected bool lockPlayerIntoInteraction;

    [SerializeField] protected SpriteRenderer highlight;
    [SerializeField] protected GameObject lockedInteractionCanvas;

    [SerializeField] protected bool interactOnStart = true;
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

            if (currentPlayerLockedIn == null)
            {
                if (playersColliding.Contains(currentInteractingPlayer.GetPlayerIndex()))
                {
                    playersColliding.Remove(currentInteractingPlayer.GetPlayerIndex());
                    currentInteractingPlayer.HideInteractionPrompt();

                    //Player can no longer interact with this item
                    currentInteractingPlayer.currentInteractableItem = null;
                }
            }
            //If the locked player leaves the trigger for the interactable, unlock them
            else
                LockPlayer(currentInteractingPlayer, false);
        }
    }

    /// <summary>
    /// Show the current player trying to interact that they can interact with the interactable.
    /// </summary>
    /// <param name="currentInteractingPlayer">The player trying to interact with the interactable.</param>
    private void ShowPlayerInteraction(PlayerController currentInteractingPlayer)
    {
        if (currentPlayerLockedIn == null && !currentInteractingPlayer.InBuildMode())
        {
            playersColliding.Add(currentInteractingPlayer.GetPlayerIndex());
            currentInteractingPlayer.DisplayInteractionPrompt("<sprite=27 tint=1>");
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
        if(!firstInteractionComplete)
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
            if(currentPlayer != null)
                currentPlayer.SetPlayerMove(true);
            currentPlayerLockedIn = null;

            highlight.gameObject.SetActive(false);
            lockedInteractionCanvas.SetActive(false);
            firstInteractionComplete = false;
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

    private void OnDestroy()
    {
        if (currentPlayerLockedIn != null && lockPlayerIntoInteraction)
        {
            LockPlayer(currentPlayerLockedIn, false);
        }
    }

    public bool IsInteractionActive()
    {
         return interactionActive;
    }

    public void SetCurrentActivePlayer(PlayerController player)
    {
        currentPlayer = player;
    }
}
