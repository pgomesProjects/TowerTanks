using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableSpawner : MonoBehaviour
{
    private int currentGhostIndex;

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
        if (transform.GetChild(0).CompareTag("GhostObject"))
            return true;
        return false;
    }

    public bool IsInteractableSpawned()
    {
        if (transform.childCount == 0)
            return false;
        return true;
    }

    public int GetCurrentGhostIndex()
    {
        return currentGhostIndex;
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
    }
}
