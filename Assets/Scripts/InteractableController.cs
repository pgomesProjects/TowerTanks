using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableController : MonoBehaviour
{
    public UnityEvent interactEvent;

    [SerializeField] private GameObject hoverInteractable;
    private bool canInteract = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canInteract = true;
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
            hoverInteractable.SetActive(false);
            //Player can no longer interact with this item
            collision.GetComponent<PlayerController>().currentInteractableItem = null;
        }
    }

    public void OnInteraction()
    {
        //If the object is interacted with, grab the function from the inspector that will decide what to do
        if (canInteract)
            interactEvent.Invoke();
    }
}
