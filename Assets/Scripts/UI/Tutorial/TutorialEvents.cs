using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEvents : CustomEvent
{
    [SerializeField] private GameObject resourcesObject;
    [SerializeField] private GameObject resourcesArrow;

    public override void CheckForCustomEvent(int indexNumber)
    {
        switch (indexNumber)
        {
            //Player Movement Prompt
            case 2:
                foreach(var player in FindObjectsOfType<PlayerController>())
                    player.SetPlayerMove(true);

                TutorialController.main.currentTutorialState = TUTORIALSTATE.PLAYERMOVEMENT;
                UnlockPlayers();
                break;
            //Pick Up Hammer Prompt
            case 3:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.PICKUPHAMMER;
                UnlockPlayers();
                break;
            //Build Layers Prompt
            case 5:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.BUILDLAYERS;
                UnlockPlayers();
                break;
            //Show all interactable indicators
            case 6:
                foreach(var i in FindObjectsOfType<InteractableSpawner>())
                    i.ShowTutorialIndicator(true);
                break;
            //Build Cannon Prompt
            case 8:
                foreach (var i in FindObjectsOfType<InteractableSpawner>())
                    i.ShowTutorialIndicator(false);

                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(1).
                    transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(true);

                TutorialController.main.currentTutorialState = TUTORIALSTATE.BUILDCANNON;
                UnlockPlayers();
                break;
            case 9:
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().GetLayerAt(1).
                    transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(false);

                resourcesObject.SetActive(true);
                resourcesArrow.SetActive(true);
                break;
            case 10:
                resourcesArrow.SetActive(false);
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
                break;
            //Add Fuel Prompt
            case 15:
                TutorialController.main.currentTutorialState = TUTORIALSTATE.ADDFUEL;
                UnlockPlayers();
                break;
            case 16:
                FindObjectOfType<FakeBulletSpawner>().SpawnFakeBullet();
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

    public override void CustomOnEventComplete()
    {
        LevelManager.instance.TransitionGameState();
    }
}
