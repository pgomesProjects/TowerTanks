using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("The tutorial indicator for when the player must purchase an option at this spawner.")] private GameObject tutorialIndicator;
    private int currentGhostIndex;  //The current ghost interactable index

    /// <summary>
    /// Spawns the interactable object.
    /// </summary>
    /// <param name="interactable">The current object to spawn.</param>
    /// <returns>The GameObject that was spawned.</returns>
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

    /// <summary>
    /// Updates the ghost index so that a new interactable can be shown.
    /// </summary>
    /// <param name="index">The number of indices to increment by.</param>
    /// <param name="totalInteractables">The total number of ghost interactables.</param>
    public void UpdateGhostIndex(int index, int totalInteractables)
    {
        currentGhostIndex += index;

        //Check each end of the array so that the index will loop around properly
        if(currentGhostIndex < 0)
            currentGhostIndex = totalInteractables - 1;
        if(currentGhostIndex >= totalInteractables)
            currentGhostIndex = 0;

        //If the current interactable is the cannon and it is on the left side of the tank
        if (currentGhostIndex == 0 && transform.position.x < 0)
        {

        }
    }

    public bool IsGhostInteractableSpawned() => transform.GetChild(1).CompareTag("GhostObject");
    public bool IsInteractableSpawned() => transform.childCount != 1;
    public int GetCurrentGhostIndex() => currentGhostIndex;
    public void SetCurrentGhostIndex(int index) => currentGhostIndex = index;

    public void ShowTutorialIndicator(bool showIndicator) => tutorialIndicator.SetActive(showIndicator);
}
