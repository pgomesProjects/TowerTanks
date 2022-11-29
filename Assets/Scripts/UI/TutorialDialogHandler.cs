using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialDialogHandler : DialogEvent
{
    private CustomEvent tutorialEvents;

    private void Awake()
    {
        tutorialEvents = GetComponent<CustomEvent>();
    }

    public override void OnDialogStart()
    {
        //Any events to happen on the start of the tutorial go here
    }

    public override void CheckEvents(ref TextWriter.TextWriterSingle textWriterObj)
    {
        string message = "";
        if (hasSeen)
        {
            message = hasSeenLines[currentLine];
        }
        else
        {
            message = dialogLines[currentLine];
        }

        //Check for custom events if present
        if (tutorialEvents != null)
            tutorialEvents.CheckForCustomEvent(currentLine);

        //Hide the continue object while the text is being displayed
        continueObject.SetActive(false);

        //Debug.Log("Current Text Speed: " + TutorialController.main.currentTextSpeed);

        //Use the text writer class to write each character one by one
        textWriterObj = TextWriter.AddWriter_Static(null, messageText, message, 1 / TutorialController.main.currentTextSpeed, true, true, OnTextComplete);
        //Move to the next line in the dialog
        currentLine++;
    }

    public void OnTextComplete()
    {
        if (!TutorialController.main.listenForInput)
        {
            continueObject.SetActive(true);
        }
    }

    public override void OnEventComplete()
    {
        //Hide the dialog box and continue object
        continueObject.SetActive(false);

        //Reset lines
        currentLine = 0;

        //Check for custom events if present
        if (tutorialEvents != null)
            tutorialEvents.CustomOnEventComplete();
    }
}
