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

    protected PlayerController currentPlayer;
    private void Start()
    {
        interactionActive = false;
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController currentInteractingPlayer = collision.GetComponent<PlayerController>();

            if (currentPlayerLockedIn == null)
            {
                playersColliding.Add(currentInteractingPlayer.GetPlayerIndex());
                currentInteractingPlayer.DisplayInteractionPrompt("<sprite=30>");
                //Tell the player that this is the item that they can interact with
                currentInteractingPlayer.currentInteractableItem = this;
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

    public void OnInteraction(PlayerController playerInteracting)
    {
        //If the object is interacted with and no one else is locked in, grab the function from the inspector that will decide what to do
        if (currentPlayerLockedIn == null || playerInteracting == currentPlayerLockedIn)
        {
            Debug.Log("Current Player Interacting: " + playerInteracting.name);
            SetCurrentActivePlayer(playerInteracting);

            if (lockPlayerIntoInteraction)
            {
                Debug.Log("Interaction Active: " + interactionActive);
                //If there is not an interaction active
                if (!interactionActive)
                {
                    playersColliding.Clear();
                    LockPlayer(playerInteracting, true);
                    interactEvent.Invoke();
                }
                else
                {
                    LockPlayer(playerInteracting, false);
                    playersColliding.Add(playerInteracting.GetPlayerIndex());
                    interactEvent.Invoke();
                }
            }
            else
            {
                interactEvent.Invoke();
            }
        }
    }

    /// <summary>
    /// Function called when the use button is pressed when locked into the interactable.
    /// </summary>
    public virtual void OnUseInteractable()
    {

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
            Debug.Log("Locking Player...");
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
            Debug.Log("Unlocking Player...");
            interactionActive = false;
            if(currentPlayer != null)
                currentPlayer.SetPlayerMove(true);
            currentPlayerLockedIn = null;

            highlight.gameObject.SetActive(false);
            lockedInteractionCanvas.SetActive(false);
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

    private PlayerController GetCollidingPlayer(int playerIndex)
    {
        foreach(var player in FindObjectsOfType<PlayerController>())
        {
            if(player.GetPlayerIndex() == playerIndex)
                return player;
        }

        return null;
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
