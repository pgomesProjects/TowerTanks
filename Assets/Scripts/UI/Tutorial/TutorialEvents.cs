using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
    public class TutorialEvents : CustomEvent
    {
        [SerializeField] private GameObject resourcesObject;
        [SerializeField] private GameObject resourcesArrow;

        [SerializeField] private RectTransform tutorialBoxTransform;
        [SerializeField] private GameObject dumpsterIndicator;

        public override void CheckForCustomEvent(int indexNumber)
        {
            /*
            switch (indexNumber)
            {
                //Interact Dumpster Prompt
                case 2:
                    foreach(var player in FindObjectsOfType<PlayerController>())
                        player.SetPlayerMove(true);

                    dumpsterIndicator.SetActive(true);
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.INTERACTDUMPSTER;
                    UnlockPlayers();
                    break;
                case 3:
                    dumpsterIndicator.SetActive(false);
                    LockPlayers();
                    break;
                //Grab Scrap Prompt
                case 4:
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.GETSCRAP;
                    UnlockPlayers();
                    break;
                //Build Layers Prompt
                case 5:
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.BUILDLAYERS;
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

                    LevelManager.Instance.GetPlayerTank().GetLayerAt(1).
                        transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(true);

                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.BUILDCANNON;
                    UnlockPlayers();
                    break;
                case 10:
                    LevelManager.Instance.GetPlayerTank().GetLayerAt(1).
                        transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(false);
                    LockPlayers();
                    break;
                //Fire Cannon Prompt
                case 11:
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.FIRECANNON;
                    UnlockPlayers();
                    break;
                //Build Engine Prompt
                case 13:
                    LevelManager.Instance.GetPlayerTank().GetLayerAt(0).
                        transform.Find("InteractSpawnerLeft").GetComponent<InteractableSpawner>().ShowTutorialIndicator(true);
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.BUILDENGINE;
                    UnlockPlayers();
                    break;
                case 14:
                    LevelManager.Instance.GetPlayerTank().GetLayerAt(0).
                        transform.Find("InteractSpawnerLeft").GetComponent<InteractableSpawner>().ShowTutorialIndicator(false);
                    LockPlayers();
                    break;
                //Add Fuel Prompt
                case 15:
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.ADDFUEL;
                    UnlockPlayers();
                    break;
                case 16:
                    FindObjectOfType<FakeBulletSpawner>().SpawnFakeBullet();
                    TutorialController.Instance.advanceTextDisabled = true;
                    LockPlayers();
                    break;
                case 17:
                    //LevelManager.Instance.ShowPopup(true);
                    LockPlayers();
                    break;
                //Put Out Fire Prompt
                case 19:
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.PUTOUTFIRE;
                    UnlockPlayers();
                    break;
                //Repair Layer Prompt
                case 21:
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.REPAIRLAYER;
                    UnlockPlayers();
                    break;
                //Build Throttle Prompt
                case 23:
                    LevelManager.Instance.GetPlayerTank().GetLayerAt(0).
                        transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(true);
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.BUILDTHROTTLE;
                    UnlockPlayers();
                    break;
                case 24:
                    LevelManager.Instance.GetPlayerTank().GetLayerAt(0).
                        transform.Find("InteractSpawnerRight").GetComponent<InteractableSpawner>().ShowTutorialIndicator(false);
                    LockPlayers();
                    break;
                //Move Throttle Prompt
                case 26:
                    TutorialController.Instance.currentTutorialState = TUTORIALSTATE.MOVETHROTTLE;
                    UnlockPlayers();
                    break;
                default:
                    LockPlayers();
                    break;
            }
            */
        }

        private void UnlockPlayers()
        {
            TutorialController.Instance.listenForInput = true;
            LevelManager.Instance.readingTutorial = false;
        }

        private void LockPlayers()
        {
            TutorialController.Instance.listenForInput = false;
            LevelManager.Instance.readingTutorial = true;
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
            LevelManager.Instance.TransitionGameState();
        }
    }
}
