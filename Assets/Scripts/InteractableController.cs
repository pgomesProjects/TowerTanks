using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableController : MonoBehaviour
{
    public UnityEvent interactEvent;
    public UnityEvent cancelEvent;

    private bool canInteract = false;
    private PlayerController currentPlayerColliding;
    private PlayerController currentPlayerLockedIn;
    private bool interactionActive;
    [SerializeField]private bool lockPlayerIntoInteraction;

    private IEnumerator steeringCoroutine;

    private void Start()
    {
        steeringCoroutine = CheckForSteeringInput();
        UpdateSteerLever();
        interactionActive = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canInteract = true;
            currentPlayerColliding = collision.GetComponent<PlayerController>();
            currentPlayerColliding.DisplayInteractionPrompt("<sprite=30>");
            //Tell the player that this is the item that they can interact with
            collision.GetComponent<PlayerController>().currentInteractableItem = this;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            currentPlayerColliding.HideInteractionPrompt();
            canInteract = false;
            currentPlayerColliding = null;
            //Player can no longer interact with this item
            collision.GetComponent<PlayerController>().currentInteractableItem = null;
        }
    }

    public void OnInteraction()
    {
        //If the object is interacted with and no one is locked in, grab the function from the inspector that will decide what to do
        if (canInteract && currentPlayerLockedIn == null)
        {
            if (lockPlayerIntoInteraction)
            {
                //If there is not an interaction active
                if (!interactionActive)
                {
                    Debug.Log("Locking Player Into Interaction...");
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

    public void ChangeSteering()
    {
        if (interactionActive)
        {
            LevelManager.instance.isSteering = true;
            StartCoroutine(steeringCoroutine);
        }
        else
        {
            LevelManager.instance.isSteering = false;
            StopCoroutine(steeringCoroutine);
        }
    }

    IEnumerator CheckForSteeringInput()
    {
        while (true)
        {
            Debug.Log(currentPlayerColliding.steeringValue);

            //Moving stick left
            if(currentPlayerColliding.steeringValue < -0.01f)
            {
                if (LevelManager.instance.speedIndex > (int)TANKSPEED.REVERSEFAST)
                {
                    LevelManager.instance.UpdateSpeed(-1);
                    UpdateSteerLever();
                    yield return new WaitForSeconds(1);
                }
            }
            //Moving stick right
            else if (currentPlayerColliding.steeringValue > 0.01f)
            {
                if(LevelManager.instance.speedIndex < (int)TANKSPEED.FORWARDFAST)
                {
                    LevelManager.instance.UpdateSpeed(1);
                    UpdateSteerLever();
                    yield return new WaitForSeconds(1);
                }
            }
            yield return null;
        }
    }

    private void UpdateSteerLever()
    {
        Transform leverPivot = transform.Find("LeverPivot");

        if (leverPivot != null)
        {
            leverPivot.localRotation = Quaternion.Euler(0, 0, -(20 * LevelManager.instance.gameSpeed));
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

    public void LockPlayer(bool lockPlayer)
    {
        if (lockPlayer)
        {
            interactionActive = true;
            currentPlayerColliding.SetPlayerMove(false);
            currentPlayerLockedIn = currentPlayerColliding;
        }
        else
        {
            interactionActive = false;
            currentPlayerColliding.SetPlayerMove(true);
            currentPlayerLockedIn = null;
        }
    }

    private void OnDestroy()
    {
        if (currentPlayerColliding != null && lockPlayerIntoInteraction)
        {
            LockPlayer(false);
        }
    }
}
