using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TUTORIALSTATE
{
    READING,
    INTERACTDUMPSTER,
    GETSCRAP,
    BUILDLAYERS,
    BUILDCANNON,
    FIRECANNON,
    BUILDENGINE,
    ADDFUEL,
    PUTOUTFIRE,
    REPAIRLAYER,
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

    public static TutorialController Instance;

    public float textSpeed = 30;
    internal float currentTextSpeed;

    internal bool listenForInput;
    internal bool advanceTextDisabled;

    internal TUTORIALSTATE currentTutorialState;

    private float advanceTextCooldown = 0.1f;
    private float currentCooldown;
    private bool canAdvanceText;

    private void Awake()
    {
        Instance = this;
        isDialogActive = false;
        listenForInput = false;
        playerControls = new PlayerControlSystem();
        currentTextSpeed = textSpeed;
        playerControls.Player.AdvanceTutorialText.performed += _ => AdvanceText();
    }

    public void AdvanceText()
    {
        //If the dialog is activated and not in the control / history menu
        if (isDialogActive && canAdvanceText && !advanceTextDisabled)
        {
            //If there is text being written already, write everything
            if (textWriterSingle != null && textWriterSingle.IsActive() && !listenForInput)
                textWriterSingle.WriteAllAndDestroy();

            //If there is no text and there are still seen lines left, check for events needed to display the text
            else if (dialogEvent.HasSeenCutscene() && dialogEvent.GetCurrentLine() < dialogEvent.GetSeenDialogLength() && !listenForInput)
            {
                OnTextAdvance();
            }

            //If there is no text and there are still lines left, check for events needed to display the text
            else if (!dialogEvent.HasSeenCutscene() && dialogEvent.GetCurrentLine() < dialogEvent.GetDialogLength() && !listenForInput)
            {
                OnTextAdvance();
            }

            //If all of the text has been shown, call the event for when the text is complete
            else if(!listenForInput)
            {
                isDialogActive = false;
                dialogEvent.OnEventComplete();
            }

            currentCooldown = 0f;
            canAdvanceText = false;
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
        if (dialogEvent == null)
            return;

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

        currentTutorialState = TUTORIALSTATE.READING;
        AdvanceText();
    }

    /// <summary>
    /// Advances the text automatically with a delay.
    /// </summary>
    /// <param name="delay">The delay in seconds for the text advance.</param>
    public void AutoAdvance(float delay)
    {
        StartCoroutine(AutoAdvanceWait(delay));
    }

    private IEnumerator AutoAdvanceWait(float delay)
    {
        yield return new WaitForSeconds(delay);
        advanceTextDisabled = false;
        AdvanceText();
    }

    private void OnTextAdvance()
    {
        dialogEvent.CheckEvents(ref textWriterSingle);
    }

    private void Update()
    {
        //Global tutorial listener
        /*        if (listenForInput)
                {

                }*/

        if (!canAdvanceText)
        {
            if(currentCooldown > advanceTextCooldown)
                canAdvanceText = true;
            else
                currentCooldown += Time.deltaTime;
        }
    }

    private bool HaveAllPlayersMoved()
    {
/*        //Check to make sure all players have moved
        foreach(var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            //If any player has not moved, return false
            if (!player.GetComponent<PlayerController>().HasPlayerMoved())
                return false;
        }*/

        return true;
    }

    public void CheckForTutorialCompletion(TUTORIALSTATE tutorialState)
    {
        //if (IsTutorialStateActive(tutorialState))
            OnTutorialTaskCompletion();
    }

    //private bool IsTutorialStateActive(TUTORIALSTATE tutorialState) => LevelManager.Instance.levelPhase == GAMESTATE.TUTORIAL && currentTutorialState == tutorialState;
}
