using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableController : MonoBehaviour
{
    public UnityEvent interactEvent;
    public UnityEvent cancelEvent;

    protected bool canInteract = false;
    protected PlayerController currentPlayerColliding;
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
            if(currentPlayerLockedIn == null)
            {
                canInteract = true;
                currentPlayerColliding = collision.GetComponent<PlayerController>();
                currentPlayerColliding.DisplayInteractionPrompt("<sprite=30>");
                //Tell the player that this is the item that they can interact with
                collision.GetComponent<PlayerController>().currentInteractableItem = this;
            }
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(currentPlayerLockedIn == null)
            {
                if(currentPlayerColliding != null)
                    currentPlayerColliding.HideInteractionPrompt();
                canInteract = false;
                currentPlayerColliding = null;
                //Player can no longer interact with this item
                collision.GetComponent<PlayerController>().currentInteractableItem = null;
            }
        }
    }

    public void OnInteraction(PlayerController playerInteracting)
    {
        //If the object is interacted with and no one else is locked in, grab the function from the inspector that will decide what to do
        if (canInteract && (currentPlayerLockedIn == null || playerInteracting == currentPlayerLockedIn))
        {
            Debug.Log("Current Player Interacting: " + playerInteracting.name);
            SetCurrentActivePlayer(playerInteracting);

            if (lockPlayerIntoInteraction)
            {
                Debug.Log("Interaction Active: " + interactionActive);
                //If there is not an interaction active
                if (!interactionActive)
                {
                    LockPlayer(true);
                    interactEvent.Invoke();
                }
                else
                {
                    LockPlayer(false);
                    interactEvent.Invoke();
                }
            }
            else
            {
                interactEvent.Invoke();
            }
        }
    }

    public void OnCancel()
    {
        //If the object is interacted with, grab the function from the inspector that will decide what to do when canceling
        if (canInteract)
        {
            if (lockPlayerIntoInteraction)
            {
                //If there is an interaction active
                if (interactionActive)
                {
                    Debug.Log("Canceling Interaction");
                    interactionActive = false;
                    currentPlayerColliding.SetPlayerMove(true);
                    cancelEvent.Invoke();
                }
            }
        }
    }

    public bool CanInteract()
    {
        return canInteract;
    }

    public PlayerController GetCurrentPlayer()
    {
        return currentPlayerColliding;
    }

    public virtual void LockPlayer(bool lockPlayer)
    {
        if (lockPlayer)
        {
            Debug.Log("Locking Player...");
            interactionActive = true;
            if (currentPlayerColliding != null)
                currentPlayerColliding.SetPlayerMove(false);
            currentPlayerLockedIn = currentPlayerColliding;

            highlight.color = currentPlayerLockedIn.GetPlayerColor();
            highlight.gameObject.SetActive(true);
            lockedInteractionCanvas.SetActive(true);
        }
        else
        {
            Debug.Log("Unlocking Player...");
            interactionActive = false;
            if(currentPlayerColliding != null)
                currentPlayerColliding.SetPlayerMove(true);
            currentPlayerLockedIn = null;

            highlight.gameObject.SetActive(false);
            lockedInteractionCanvas.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (currentPlayerColliding != null && lockPlayerIntoInteraction)
        {
            LockPlayer(false);
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
