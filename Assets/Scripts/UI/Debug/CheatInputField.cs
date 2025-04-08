using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.EventSystems;

namespace TowerTanks.Scripts.DebugTools
{
    public class CheatInputField : MonoBehaviour
    {
        [SerializeField, Tooltip("The input field for commands.")] private TMP_InputField inputField;
        [SerializeField, Tooltip("The text area for where the logs are written.")] private TextMeshProUGUI logText;

        private DebugTools debugTools;
        public enum MessageType { Log, Warning, Error }

        private PlayerControlSystem playerControls;

        private void Awake()
        {
            playerControls = new PlayerControlSystem();
            playerControls.Debug.SubmitCommand.started += _ => SubmitInputField();
            debugTools = FindObjectOfType<DebugTools>();
        }

        private void OnEnable()
        {
            playerControls?.Enable();
            GameManager.Instance.MultiplayerManager.EnableDebugInput();
            logText.text = debugTools.logHistory;
            GameManager.Instance.inDebugMenu = true;
        }

        private void OnDisable()
        {
            playerControls?.Disable();
            inputField.text = string.Empty;
            GameManager.Instance.MultiplayerManager?.DisableDebugInput();
            GameManager.Instance.inDebugMenu = false;
        }

        public void ClearLog()
        {
            logText.text = string.Empty;
            debugTools.logHistory = string.Empty;
        }

        public void AddToLog(string message, MessageType messageType = MessageType.Log)
        {
            string logMessage = "";

            switch (messageType)
            {
                case MessageType.Log:
                    logMessage += "<color=white>";
                    break;
                case MessageType.Warning:
                    logMessage += "<color=yellow>";
                    break;
                case MessageType.Error:
                    logMessage += "<color=red>";
                    break;
            }

            logMessage += message + "</color><br>";

            debugTools.logHistory += logMessage;
            logText.text = logMessage;
        }

        public void ForceActivateInput()
        {
            inputField.Select();
            inputField.ActivateInputField();
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
                case "tutorials":
                    if (commandParts.Length > 1)
                    {
                        string subCommand = commandParts[1].ToLower();
                        ToggleTutorials(subCommand);
                    }
                    else
                        AddToLog("'tutorials' command requires additional parameters.", MessageType.Error);
                    break;
                case "scene":
                    if (commandParts.Length > 1)
                    {
                        string sceneCommand = commandParts[1].ToLower();
                        HandleSceneTransition(sceneCommand);
                    }
                    else
                        AddToLog("'scene' command requires 'combat' or 'build' as parameters.", MessageType.Error);
                    break;
                default:
                    AddToLog("'" + command + "' is an unknown command.", MessageType.Error);
                    break;
            }
        }

        private void HandleSceneTransition(string sceneCommand)
        {
            switch (sceneCommand)
            {
                case "combat":
                    if (BuildSystemManager.Instance != null)
                        BuildSystemManager.Instance?.GetReadyUpManager().StartReadySequence();
                    else
                        GameManager.Instance.LoadScene("HotteScene", LevelTransition.LevelTransitionType.GATE, true, true, false);
                    AddToLog("Transitioning to Combat Scene...", MessageType.Log);
                    break;
                case "build":
                    if (LevelManager.Instance != null)
                        LevelManager.Instance?.CompleteMission();
                    else
                        GameManager.Instance.LoadScene("BuildTankScene", LevelTransition.LevelTransitionType.GATE, true, true, false);
                    AddToLog("Transitioning to Build Scene...", MessageType.Log);
                    break;
                default:
                    AddToLog("'" + sceneCommand + "' is not a valid scene. Use 'combat' or 'build'.", MessageType.Error);
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

        private void ToggleTutorials(string skipTutorials)
        {
            switch (skipTutorials)
            {
                case "on":
                    if (!GameSettings.skipTutorials)
                        AddToLog("Tutorials already enabled.", MessageType.Warning);
                    else
                    {
                        GameSettings.skipTutorials = false;
                        AddToLog("Tutorials enabled.", MessageType.Log);
                    }
                    break;
                case "off":
                    if (GameSettings.skipTutorials)
                        AddToLog("Tutorials already disabled.", MessageType.Warning);
                    else
                    {
                        GameSettings.skipTutorials = true;
                        AddToLog("Tutorials disabled.", MessageType.Log);
                    }
                    break;
                default:
                    AddToLog("'" + skipTutorials + "' is not a valid parameter for 'tutorials' command.", MessageType.Error);
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
            helpMessage += "tutorials [on:off] - Turns tutorials on or off.<br>";
            helpMessage += "scene [combat:build] - Switches between the combat and build scenes.<br>";
            helpMessage += "clear - Clears the command log.";
            AddToLog(helpMessage);
        }
    }
}
