using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableSpawner : MonoBehaviour
{
    public void SpawnInteractable(GameObject interactable)
    {
        //If there is no existing interactable, spawn one
        if (!IsInteractableSpawned())
        {
            GameObject newInteractable = Instantiate(interactable, transform.position, interactable.transform.rotation);
            newInteractable.transform.parent = transform;
            newInteractable.transform.localPosition = interactable.transform.localPosition;
        }
    }

    public bool IsInteractableSpawned()
    {
        if (transform.childCount == 0)
            return false;
        return true;
    }
}
