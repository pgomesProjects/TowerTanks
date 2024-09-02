using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;

public class CheatInputField : MonoBehaviour
{
    [SerializeField, Tooltip("The input field for commands.")] private TMP_InputField inputField;
    [SerializeField, Tooltip("The text area for where the logs are written.")] private TextMeshProUGUI logText;

    private DebugTools debugTools;
    public enum MessageType { Log, Warning, Error }

    private PlayerControlSystem playerControls;

    private void Awake()
    {
        ClearLog();
        playerControls = new PlayerControlSystem();
        playerControls.Debug.SubmitCommand.started += _ => SubmitInputField();
        debugTools = FindObjectOfType<DebugTools>();
    }

    private void OnEnable()
    {
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
    }

    public void ClearLog()
    {
        logText.text = string.Empty;
    }

    public void AddToLog(string message, MessageType messageType = MessageType.Log)
    {
        switch (messageType)
        {
            case MessageType.Log:
                logText.text += "<color=white>";
                break;
            case MessageType.Warning:
                logText.text += "<color=yellow>";
                break;
            case MessageType.Error:
                logText.text += "<color=red>";
                break;
        }

        logText.text += message + "</color><br>";
    }

    private void SubmitInputField()
    {
        SubmitCommand(inputField.text);
        inputField.text = string.Empty;
        inputField.Select();
        inputField.ActivateInputField();
    }

    /// <summary>
    /// Takes the command given and parses it to any known commands.
    /// </summary>
    /// <param name="command">The command given through the input text.</param>
    public void SubmitCommand(string command)
    {
        // Split the command into parts based on spaces
        string[] commandParts = command.Split(' ');

        string mainCommand = commandParts[0].ToLower();

        switch (mainCommand)
        {
            case "help":
                CommandExplanation();
                break;
            case "clear":
                ClearLog();
                break;
            case "debug":
                if (commandParts.Length > 1)
                {
                    string subCommand = commandParts[1].ToLower();
                    ToggleDebugMode(subCommand);
                }
                else
                    AddToLog("'debug' command requires additional parameters.", MessageType.Error);
                break;
            default:
                AddToLog("'" + command + "' is an unknown command.", MessageType.Error);
                break;
        }
    }

    private void ToggleDebugMode(string debugMode)
    {
        switch (debugMode)
        {
            case "on":
                debugTools?.ToggleDebugMode(true);
                AddToLog("Debug mode enabled.", MessageType.Log);
                break;
            case "off":
                debugTools?.ToggleDebugMode(false);
                AddToLog("Debug mode disabled.", MessageType.Log);
                break;
            default:
                AddToLog("'" + debugMode + "' is not a valid parameter for 'debug' command.", MessageType.Error);
                break;
        }
    }

    /// <summary>
    /// Provides a list of commands and explanation for the commands.
    /// </summary>
    private void CommandExplanation()
    {
        string helpMessage = "";
        helpMessage += "---LIST OF COMMANDS---<br>";

        helpMessage += "help - Provides a list of commands.<br>";
        helpMessage += "debug [on:off] - Turns debug mode on or off.<br>";
        helpMessage += "clear - Clears the command log.";
        AddToLog(helpMessage);
    }
}
