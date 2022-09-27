using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableController : MonoBehaviour
{
    public UnityEvent interactEvent;

    [SerializeField] private GameObject hoverInteractable;
    private bool canInteract = false;
    private PlayerController currentPlayer;
    private bool interactionActive;
    [SerializeField]private bool lockPlayerIntoInteraction;

    private IEnumerator steeringCoroutine;

    private void Start()
    {
        steeringCoroutine = CheckForSteeringInput();
        interactionActive = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canInteract = true;
            currentPlayer = collision.GetComponent<PlayerController>();
            hoverInteractable.SetActive(true);
            //Tell the player that this is the item that they can interact with
            collision.GetComponent<PlayerController>().currentInteractableItem = this;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canInteract = false;
            currentPlayer = null;
            hoverInteractable.SetActive(false);
            //Player can no longer interact with this item
            collision.GetComponent<PlayerController>().currentInteractableItem = null;
        }
    }

    public void OnInteraction()
    {
        //If the object is interacted with, grab the function from the inspector that will decide what to do
        if (canInteract)
        {
            if (lockPlayerIntoInteraction)
            {
                //If there is an interaction active
                if (!interactionActive)
                {
                    interactionActive = true;
                    currentPlayer.SetPlayerMove(false);
                    interactEvent.Invoke();
                }
                else
                {
                    interactionActive = false;
                    currentPlayer.SetPlayerMove(true);
                    interactEvent.Invoke();
                }
            }
            else
            {
                interactEvent.Invoke();
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
            Debug.Log(currentPlayer.steeringValue);

            //Moving stick left
            if(currentPlayer.steeringValue < -0.01f)
            {
                if (LevelManager.instance.speedIndex > (int)TANKSPEED.REVERSEFAST)
                {
                    LevelManager.instance.UpdateSpeed(-1);
                    yield return new WaitForSeconds(1);
                }
            }
            //Moving stick right
            else if (currentPlayer.steeringValue > 0.01f)
            {
                if(LevelManager.instance.speedIndex < (int)TANKSPEED.FORWARDFAST)
                {
                    LevelManager.instance.UpdateSpeed(1);
                    yield return new WaitForSeconds(1);
                }
            }
            yield return null;
        }
    }

    public bool CanInteract()
    {
        return canInteract;
    }

    public PlayerController GetCurrentPlayer()
    {
        return currentPlayer;
    }
}
