using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractableSpawnerManager : MonoBehaviour
{
    [SerializeField] private GameObject cannon;
    [SerializeField] private GameObject engine;
    [SerializeField] private GameObject throttle;

    [SerializeField] private GameObject[] ghostInteractables;

    public void SpawnCannon(InteractableSpawner currentSpawner)
    {
        GameObject cannonObject = currentSpawner.SpawnInteractable(cannon);
        if(cannonObject.transform.position.x < GameObject.FindGameObjectWithTag("PlayerTank").transform.position.x)
            cannonObject.GetComponent<PlayerCannonDirectionUpdater>().FlipCannonX();

        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDCANNON)
            {
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }

        LevelManager.instance.currentSessionStats.numberOfCannons += 1;
    }

    public void SpawnEngine(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(engine);

        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if (TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDENGINE)
            {
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }

        LevelManager.instance.currentSessionStats.numberOfEngines += 1;
    }

    public void SpawnThrottle(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(throttle);

        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if (TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDTHROTTLE)
            {
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }

        LevelManager.instance.currentSessionStats.numberOfThrottles += 1;
    }

    public void ShowNewGhostInteractable(InteractableSpawner currentSpawner)
    {
        GameObject newGhost = Instantiate(ghostInteractables[currentSpawner.GetCurrentGhostIndex()], ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.position, currentSpawner.transform.rotation);
        newGhost.transform.parent = currentSpawner.transform;
        newGhost.transform.localPosition = ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.localPosition;

        if(currentSpawner.GetCurrentGhostIndex() == 0 && newGhost.transform.position.x < GameObject.FindGameObjectWithTag("PlayerTank").transform.position.x)
        {
            if (newGhost.TryGetComponent<PlayerCannonDirectionUpdater>(out PlayerCannonDirectionUpdater playerCannonDirectionUpdater))
                playerCannonDirectionUpdater.FlipCannonX();
        }
    }

    public void UpdateGhostInteractable(InteractableSpawner currentSpawner, int index)
    {
        Destroy(currentSpawner.transform.GetChild(1).gameObject);
        currentSpawner.UpdateGhostIndex(index, ghostInteractables.Length);
        ShowNewGhostInteractable(currentSpawner);
    }

    public void FlipInteractablePreview(ref GameObject currentObject)
    {
        currentObject.transform.localScale = FlipScaleX(currentObject.transform.localScale);

        Transform priceTransform = currentObject.transform.Find("InteractablePrice");

        //If the object has a price, rotate that as well
        if (priceTransform != null)
        {
            priceTransform.localScale = FlipScaleX(priceTransform.localScale);
        }
    }

    private Vector3 FlipScaleX(Vector3 objectScale) => new Vector3(-objectScale.x, objectScale.y, objectScale.z);
}
