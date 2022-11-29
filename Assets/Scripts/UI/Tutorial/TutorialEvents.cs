using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEvents : CustomEvent
{
    public override void CheckForCustomEvent(int indexNumber)
    {
        switch (indexNumber)
        {
            //Move Throttle prompt
            case 7:
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
