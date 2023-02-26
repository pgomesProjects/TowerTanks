using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableSpawner : MonoBehaviour
{
    private int currentGhostIndex;
    [SerializeField] private GameObject tutorialIndicator;

    public GameObject SpawnInteractable(GameObject interactable)
    {
        GameObject newInteractable = null;
        //If there is no existing interactable, spawn one
        if (!IsInteractableSpawned() || IsGhostInteractableSpawned())
        {
            newInteractable = Instantiate(interactable, transform.position, interactable.transform.rotation);
            newInteractable.transform.parent = transform;
            newInteractable.transform.localPosition = interactable.transform.localPosition;
        }

        return newInteractable;
    }

    public bool IsGhostInteractableSpawned()
    {
        if (transform.GetChild(1).CompareTag("GhostObject"))
            return true;
        return false;
    }

    public bool IsInteractableSpawned()
    {
        if (transform.childCount == 1)
            return false;
        return true;
    }

    public int GetCurrentGhostIndex()
    {
        return currentGhostIndex;
    }

    public void SetCurrentGhostIndex(int index)
    {
        currentGhostIndex = index;
    }

    public void UpdateGhostIndex(int index, int totalInteractables)
    {
        currentGhostIndex += index;

        if(currentGhostIndex < 0)
            currentGhostIndex = totalInteractables - 1;

        if(currentGhostIndex >= totalInteractables)
        {
            currentGhostIndex = 0;
        }

        //If the current interactable is the cannon and it is on the left side of the tank
        if (currentGhostIndex == 0 && transform.position.x < 0)
        {

        }
    }

    public void ShowTutorialIndicator(bool showIndicator)
    {
        tutorialIndicator.SetActive(showIndicator);
    }
}
