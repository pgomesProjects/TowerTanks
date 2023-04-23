using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialDialogHandler : DialogEvent
{
    private CustomEvent tutorialEvents;

    private void Awake()
    {
        tutorialEvents = GetComponent<CustomEvent>();
    }

    public override void OnDialogStart()
    {
        UpdatePrompts();

        //If the continue object has text, update the prompt on that as well
        if (continueObject.GetComponent<TextMeshProUGUI>() != null)
        {
            string continueText = continueObject.GetComponent<TextMeshProUGUI>().text;
            CheckStringForPrompt(ref continueText);
            continueObject.GetComponent<TextMeshProUGUI>().text = continueText;
        }
    }

    private void UpdatePrompts()
    {
        for(int i = 0; i < dialogLines.Length; i++)
        {
            CheckStringForPrompt(ref dialogLines[i]);
        }
    }

    private void CheckStringForPrompt(ref string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '<')
            {
                string replaceCommand = CheckForControlDisplay(line, i);
                string newCommandString = GetComponent<ControlSchemeUIUpdater>().UpdatePrompt(replaceCommand.Substring(1, (replaceCommand.Length - 2)));

                line = line.Replace(replaceCommand, newCommandString);
                continue;
            }
        }
    }

    private string CheckForControlDisplay(string line, int counter)
    {
        int posFrom = counter - 1;
        int posTo = line.IndexOf(">", posFrom + 1);
        if (posTo != -1) //if found char
        {
            return line.Substring(posFrom + 1, posTo - posFrom);
        }

        return string.Empty;
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
        if (!TutorialController.main.listenForInput && !TutorialController.main.advanceTextDisabled)
            continueObject.SetActive(true);
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
