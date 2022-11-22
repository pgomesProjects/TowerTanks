using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
abstract public class DialogEvent : MonoBehaviour
{
    protected int currentLine;

    //The basic objects that any dialog event will need
    [Header("Dialog Objects")]
    [SerializeField] [Tooltip("The lines of dialog shown in order.")]
    protected string[] dialogLines;
    protected bool dialogWrittenInHistory;
    [SerializeField]
    [Tooltip("The lines of dialog (if the player has seen the cutscene) shown in order.")]
    protected string[] hasSeenLines;
    protected bool hasSeen;
    protected bool hasSeenWrittenInHistory;
    [SerializeField] [Tooltip("The object that holds the message text.")]
    protected TextMeshProUGUI messageText;
    [SerializeField] [Tooltip("The object that holds whatever will be used to tell the player to continue the dialog.")]
    protected GameObject continueObject;

    //Template for checking for events in the dialog
    public abstract void CheckEvents(ref TextWriter.TextWriterSingle textWriterObj);

    public int GetCurrentLine() { return this.currentLine; }
    public int GetDialogLength() { return this.dialogLines.Length; }
    public bool HasSeenCutscene() { return hasSeen; }
    public int GetSeenDialogLength() { return this.hasSeenLines.Length; }
    public abstract void OnDialogStart();
    public abstract void OnEventComplete();
}
