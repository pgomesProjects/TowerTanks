using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractableSpawnerManager : MonoBehaviour
{
    [SerializeField] private GameObject cannon;
    [SerializeField] private GameObject engine;
    [SerializeField] private GameObject shellStation;
    [SerializeField] private GameObject throttle;

    [SerializeField] private GameObject[] ghostInteractables;

    public void SpawnCannon(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(cannon);
/*        if(cannonObject.transform.position.x < GameObject.FindGameObjectWithTag("PlayerTank").transform.position.x)
        {
            //cannonObject.GetComponent<CannonController>().SetCannonDirection(CannonController.CANNONDIRECTION.LEFT);
            GameObject pivot = cannonObject.transform.Find("CannonPivot").gameObject;
            RotateObject(ref pivot);
        }*/

        if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDCANNON)
            {
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }
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
    }

    public void SpawnShellStation(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(shellStation);

        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if (TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDAMMOCRATE)
            {
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }
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
    }

    public void ShowNewGhostInteractable(InteractableSpawner currentSpawner)
    {
        GameObject newGhost = Instantiate(ghostInteractables[currentSpawner.GetCurrentGhostIndex()], ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.position, currentSpawner.transform.rotation);
        newGhost.transform.parent = currentSpawner.transform;
        newGhost.transform.localPosition = ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.localPosition;

        if(currentSpawner.GetCurrentGhostIndex() == 0 && newGhost.transform.position.x < GameObject.FindGameObjectWithTag("PlayerTank").transform.position.x)
        {
            RotateObject(ref newGhost);
        }
    }

    public void UpdateGhostInteractable(InteractableSpawner currentSpawner, int index)
    {
        Destroy(currentSpawner.transform.GetChild(1).gameObject);
        currentSpawner.UpdateGhostIndex(index, ghostInteractables.Length);
        ShowNewGhostInteractable(currentSpawner);
    }

    public void RotateObject(ref GameObject currentObject)
    {
        currentObject.transform.rotation = Quaternion.Euler(0, 0, -180);

        //If the object has a price, rotate that as well
        if (currentObject.transform.Find("InteractablePrice"))
        {
            currentObject.transform.Find("InteractablePrice").transform.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, 0);
            currentObject.transform.Find("InteractablePrice").transform.GetComponent<RectTransform>().localPosition =
                new Vector3(currentObject.transform.Find("InteractablePrice").transform.GetComponent<RectTransform>().localPosition.x,
                -(currentObject.transform.Find("InteractablePrice").transform.GetComponent<RectTransform>().localPosition.y),
                currentObject.transform.Find("InteractablePrice").transform.GetComponent<RectTransform>().localPosition.z);
        }
    }
}
