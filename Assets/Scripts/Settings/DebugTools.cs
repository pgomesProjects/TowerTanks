using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.DebugTools
{
    public class DebugTools : MonoBehaviour
    {
        [SerializeField, Tooltip("The main debug display.")] private CanvasGroup debugCanvasGroup;
        [SerializeField, Tooltip("The bug report screen.")] private BugReportForm bugReportScreenPrefab;
        [SerializeField, Tooltip("The command menu to spawn.")] private CheatInputField commandMenuPrefab;
        private PlayerControlSystem playerControlSystem;
        private CheatInputField currentDebugMenu;
        private BugReportForm currentBugReportForm;

        internal string logHistory;

        private void Awake()
        {
            playerControlSystem = new PlayerControlSystem();
            playerControlSystem.Debug.ToggleGamepadCursors.performed += _ => OnToggleGamepadCursors();
            playerControlSystem.Debug.ToggleCommandMenu.performed += _ => ToggleCommandMenu();
            playerControlSystem.Debug.Screenshot.performed += _ => GameManager.Instance.SystemEffects.TakeScreenshot();
            playerControlSystem.Debug.BugReport.performed += _ => ToggleBugReportScreen();
        }

        private void OnEnable()
        {
            playerControlSystem?.Enable();
        }

        private void OnDisable()
        {
            playerControlSystem?.Disable();
        }

        public void ToggleDebugMode(bool isDebugMode)
        {
            GameSettings.debugMode = isDebugMode;

            if (isDebugMode)
            {
                GameManager.Instance.AudioManager.Play("DebugBeep");
            }

            debugCanvasGroup.alpha = isDebugMode ? 1 : 0;
        }

        private void OnToggleGamepadCursors()
        {
            if (!GameSettings.debugMode)
                return;

            GameManager.Instance.SetGamepadCursorsActive(!GameSettings.showGamepadCursors);
        }

        private void ToggleCommandMenu()
        {
            //If the user is in the debug menu already, disable it
            if (GameManager.Instance.inDebugMenu)
                currentDebugMenu.gameObject.SetActive(false);
            else
            {
                //If the game has another menu up, return
                if (GameManager.Instance.InGameMenu)
                    return;

                if (currentDebugMenu == null)
                {
                    currentDebugMenu = Instantiate(commandMenuPrefab, GameObject.FindGameObjectWithTag("CursorCanvas").transform);
                    currentDebugMenu.transform.SetAsLastSibling();
                }
                else
                    currentDebugMenu.gameObject.SetActive(true);

                currentDebugMenu.ForceActivateInput();
            }
        }

        private void ToggleBugReportScreen()
        {
            //If the user is in the bug report screen already, disable it
            if (GameManager.Instance.inBugReportMenu)
                currentBugReportForm.gameObject.SetActive(false);
            else
            {
                //If the game has another menu up, return
                if (GameManager.Instance.InGameMenu)
                    return;

                if (currentBugReportForm == null)
                {
                    currentBugReportForm = Instantiate(bugReportScreenPrefab, GameObject.FindGameObjectWithTag("CursorCanvas").transform);
                    currentBugReportForm.transform.SetAsLastSibling();
                }
                else
                    currentBugReportForm.gameObject.SetActive(true);
            }
        }
    }
}
