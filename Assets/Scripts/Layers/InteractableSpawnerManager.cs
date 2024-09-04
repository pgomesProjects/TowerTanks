using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum DEPRECATEDINTERACTABLETYPE { CANNON, ENGINE, DUMPSTER, THROTTLE, DRILL };

public class InteractableSpawnerManager : MonoBehaviour
{
    [SerializeField, Tooltip("The cannon GameObject.")]   private GameObject cannon;
    [SerializeField, Tooltip("The engine GameObject.")]   private GameObject engine;
    [SerializeField, Tooltip("The dumpster GameObject.")] private GameObject dumpster;
    [SerializeField, Tooltip("The throttle GameObject.")] private GameObject throttle;
    [SerializeField, Tooltip("The drill GameObject.")]    private GameObject drill;

    [SerializeField, Tooltip("A list of ghost interactables to show in order.")] private GameObject[] ghostInteractables;

    /// <summary>
    /// Creates an interactable based on the type given at the current spawner location.
    /// </summary>
    /// <param name="currentSpawner">The current spawner to create the interactable at.</param>
    /// <param name="currentInteractable">The current interactable to spawn.</param>
    public void CreateInteractable(InteractableSpawner currentSpawner, DEPRECATEDINTERACTABLETYPE currentInteractable)
    {
        switch (currentInteractable)
        {
            case DEPRECATEDINTERACTABLETYPE.CANNON:
                GameObject cannonObject = currentSpawner.SpawnInteractable(cannon);
                //Flip the cannon if on the left side of the tank
                if (cannonObject.transform.position.x < GameObject.FindGameObjectWithTag("PlayerTank").transform.position.x)
                    cannonObject.GetComponent<PlayerCannonDirectionUpdater>().FlipCannonX();

                TutorialController.Instance.CheckForTutorialCompletion(TUTORIALSTATE.BUILDCANNON);
                LevelManager.Instance.currentSessionStats.numberOfCannons += 1;
                break;
            case DEPRECATEDINTERACTABLETYPE.ENGINE:
                currentSpawner.SpawnInteractable(engine);
                //TutorialController.Instance.CheckForTutorialCompletion(TUTORIALSTATE.BUILDENGINE);
                LevelManager.Instance.currentSessionStats.numberOfEngines += 1;
                break;
            case DEPRECATEDINTERACTABLETYPE.DUMPSTER:
                currentSpawner.SpawnInteractable(dumpster);
                LevelManager.Instance.currentSessionStats.numberOfDumpsters += 1;
                break;
            case DEPRECATEDINTERACTABLETYPE.THROTTLE:
                currentSpawner.SpawnInteractable(throttle);
                //TutorialController.Instance.CheckForTutorialCompletion(TUTORIALSTATE.BUILDTHROTTLE);
                LevelManager.Instance.currentSessionStats.numberOfThrottles += 1;
                break;
            case DEPRECATEDINTERACTABLETYPE.DRILL:
                currentSpawner.SpawnInteractable(drill);
                //Tutorial check
                //Session stats update
                break;
        }
    }

    /// <summary>
    /// Display the newest ghost interactable.
    /// </summary>
    /// <param name="currentSpawner">The current spawner to show the newest ghost interactable at.</param>
    public void ShowNewGhostInteractable(InteractableSpawner currentSpawner)
    {
        GameObject newGhost = Instantiate(ghostInteractables[currentSpawner.GetCurrentGhostIndex()], ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.position, currentSpawner.transform.rotation);
        newGhost.transform.parent = currentSpawner.transform;
        newGhost.transform.localPosition = ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.localPosition;

        //If the current spawner is showing the cannon at the left of the tank, flip the cannon
        if(currentSpawner.GetCurrentGhostIndex() == 0 && newGhost.transform.position.x < GameObject.FindGameObjectWithTag("PlayerTank").transform.position.x)
        {
            if (newGhost.TryGetComponent<PlayerCannonDirectionUpdater>(out PlayerCannonDirectionUpdater playerCannonDirectionUpdater))
                playerCannonDirectionUpdater.FlipCannonX();
        }
    }

    /// <summary>
    /// Tells the current spawner to update the ghost interactable at a spawner.
    /// </summary>
    /// <param name="currentSpawner">The current spawner to show the ghost interactable at.</param>
    /// <param name="index">The current ghost interactable index to show.</param>
    public void UpdateGhostInteractable(InteractableSpawner currentSpawner, int index)
    {
        Destroy(currentSpawner.transform.GetChild(1).gameObject);
        currentSpawner.UpdateGhostIndex(index, ghostInteractables.Length);
        ShowNewGhostInteractable(currentSpawner);
    }

    /// <summary>
    /// Flip the ghost interactable preview on the X axis.
    /// </summary>
    /// <param name="currentObject">A reference to the current interactable to flip.</param>
    public void FlipInteractablePreview(ref GameObject currentObject)
    {
        currentObject.transform.localScale = FlipScaleX(currentObject.transform.localScale);

        Transform priceTransform = currentObject.transform.Find("InteractablePrice");

        //If the object has a price, rotate that as well
        if (priceTransform != null)
            priceTransform.localScale = FlipScaleX(priceTransform.localScale);
    }

    private Vector3 FlipScaleX(Vector3 objectScale) => new Vector3(-objectScale.x, objectScale.y, objectScale.z);
}
