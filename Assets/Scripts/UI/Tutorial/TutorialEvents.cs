using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEvents : CustomEvent
{
    [SerializeField] private GameObject resourcesObject;
    [SerializeField] private GameObject resourcesArrow;

    [SerializeField] private RectTransform tutorialBoxTransform;
    [SerializeField] private GameObject dumpsterIndicator;

    public override void CheckForCustomEvent(int indexNumber)
    {
        switch (indexNumber)
        {
            //Interact Dumpster Prompt
            case 2:
                foreach(var player in FindObjectsOfType<PlayerController>())
                    player.SetPlayerMove(true);

                dumpsterIndicator.SetActive(true);
                TutorialController.main.currentTutorialState = TUTORIALSTATE.INTERACTDUMPSTER;
                UnlockPlayers();
                break;
            case 3:
                dumpsterIndicator.SetActive(false);
                LockPlayers();
                break;
            //Grab Scrap Prompt
            case 4:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.GETSCRAP;
                UnlockPlayers();
                break;
            //Build Layers Prompt
            case 5:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.BUILDLAYERS;
                UnlockPlayers();
                break;
            case 6:
                ChangeTransformAnchor(new Vector2(0f, 0.5f));
                ChangeBoxPosition(new Vector2(-8f, 0f));
                resourcesObject.SetActive(true);
                resourcesArrow.SetActive(true);
                LockPlayers();
                break;
            //Show all interactable indicators
            case 7:
                resourcesArrow.SetActive(false);
                foreach (var i in FindObjectsOfType<InteractableSpawner>())
                    i.ShowTutorialIndicator(true);
                LockPlayers();
                break;
            //Build Cannon Prompt
            case 9:
                foreach (var i in FindObjectsOfType<InteractableSpawner>())
                    i.ShowTutorialIndicator(false);

                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(1).
                    transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(true);

                TutorialController.main.currentTutorialState = TUTORIALSTATE.BUILDCANNON;
                UnlockPlayers();
                break;
            case 10:
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(1).
                    transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(false);
                LockPlayers();
                break;
            //Fire Cannon Prompt
            case 11:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.FIRECANNON;
                UnlockPlayers();
                break;
            //Build Engine Prompt
            case 13:
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(0).
                    transform.Find("InteractSpawnerLeft").GetComponent<InteractableSpawner>().ShowTutorialIndicator(true);
                TutorialController.main.currentTutorialState = TUTORIALSTATE.BUILDENGINE;
                UnlockPlayers();
                break;
            case 14:
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(0).
                    transform.Find("InteractSpawnerLeft").GetComponent<InteractableSpawner>().ShowTutorialIndicator(false);
                LockPlayers();
                break;
            //Add Fuel Prompt
            case 15:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.ADDFUEL;
                UnlockPlayers();
                break;
            case 16:
                FindObjectOfType<FakeBulletSpawner>().SpawnFakeBullet();
                TutorialController.main.advanceTextDisabled = true;
                LockPlayers();
                break;
            case 17:
                LevelManager.instance.ShowPopup(true);
                LockPlayers();
                break;
            //Put Out Fire Prompt
            case 19:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.PUTOUTFIRE;
                UnlockPlayers();
                break;
            //Build Throttle Prompt
            case 21:
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(0).
                    transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(true);
                TutorialController.main.currentTutorialState = TUTORIALSTATE.BUILDTHROTTLE;
                UnlockPlayers();
                break;
            case 22:
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(0).
                    transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(false);
                LockPlayers();
                break;
            //Move Throttle Prompt
            case 24:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.MOVETHROTTLE;
                UnlockPlayers();
                break;
            default:
                LockPlayers();
                break;
        }
    }

    private void UnlockPlayers()
    {
        TutorialController.main.listenForInput = true;
        LevelManager.instance.readingTutorial = false;
    }

    private void LockPlayers()
    {
        TutorialController.main.listenForInput = false;
        LevelManager.instance.readingTutorial = true;
    }
    
    private void ChangeTransformAnchor(Vector2 newAnchor)
    {
        tutorialBoxTransform.pivot = newAnchor;
        tutorialBoxTransform.anchorMin = newAnchor;
        tutorialBoxTransform.anchorMax = newAnchor;
    }

    private void ChangeBoxPosition(Vector2 newPos)
    {
        tutorialBoxTransform.anchoredPosition = newPos;
    }

    public override void CustomOnEventComplete()
    {
        LevelManager.instance.TransitionGameState();
    }
}
