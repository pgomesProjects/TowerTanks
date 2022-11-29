using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TUTORIALSTATE
{
    READING,
    BUILDFIRSTLAYER,
    BUILDSECONDLAYER,
    BUILDSHELLSTATION,
    BUILDCANNON,
    GETSHELL,
    FIRECANNON,
    BUILDENGINE,
    GIVEENGINEFUEL,
    BUILDTHROTTLE,
    MOVETHROTTLE
}

[RequireComponent(typeof(TextWriter))]
public class TutorialController : MonoBehaviour
{
    internal TextWriter.TextWriterSingle textWriterSingle;
    private PlayerControlSystem playerControls;
    internal DialogEvent dialogEvent;
    private bool isDialogActive;

    public bool playOnStart = false;

    public static TutorialController main;

    public float textSpeed = 30;
    internal float currentTextSpeed;

    internal bool listenForInput;

    internal TUTORIALSTATE currentTutorialState;

    private void Awake()
    {
        main = this;
        isDialogActive = false;
        listenForInput = false;
        playerControls = new PlayerControlSystem();
        currentTextSpeed = textSpeed;
        playerControls.Player.AdvanceTutorialText.performed += _ => AdvanceText();
    }

    public void AdvanceText()
    {
        //If the dialog is activated and not in the control / history menu
        if (isDialogActive)
        {
            //If there is text being written already, write everything
            if (textWriterSingle != null && textWriterSingle.IsActive() && !listenForInput)
                textWriterSingle.WriteAllAndDestroy();

            //If there is no text and there are still seen lines left, check for events needed to display the text
            else if (dialogEvent.HasSeenCutscene() && dialogEvent.GetCurrentLine() < dialogEvent.GetSeenDialogLength() && !listenForInput)
            {
                dialogEvent.CheckEvents(ref textWriterSingle);
            }

            //If there is no text and there are still lines left, check for events needed to display the text
            else if (!dialogEvent.HasSeenCutscene() && dialogEvent.GetCurrentLine() < dialogEvent.GetDialogLength() && !listenForInput)
            {
                dialogEvent.CheckEvents(ref textWriterSingle);
            }

            //If all of the text has been shown, call the event for when the text is complete
            else if(!listenForInput)
            {
                isDialogActive = false;
                dialogEvent.OnEventComplete();
            }
        }
    }

    private void Start()
    {
        //If the dialog is meant to be played at the start, trigger it immediately
        if (playOnStart)
            TriggerDialogEvent();
    }

    public void TriggerDialogEvent()
    {
        //Start text event
        isDialogActive = true;

        //Make the player idle and freeze them

        dialogEvent.OnDialogStart();
        dialogEvent.CheckEvents(ref textWriterSingle);
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    public void OnTutorialTaskCompletion()
    {
        listenForInput = false;

        if (textWriterSingle != null && textWriterSingle.IsActive())
            textWriterSingle.WriteAllAndDestroy();

        AdvanceText();
        currentTutorialState = TUTORIALSTATE.READING;
    }
}
