using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTools : MonoBehaviour
{
    [SerializeField, Tooltip("The main debug display.")] private CanvasGroup debugCanvasGroup;
    [SerializeField, Tooltip("The command menu to spawn.")] private CheatInputField commandMenuPrefab;
    private PlayerControlSystem playerControlSystem;
    private CheatInputField currentDebugMenu;

    internal string logHistory;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
        playerControlSystem.Debug.ToggleGamepadCursors.performed += _ => OnToggleGamepadCursors();
        playerControlSystem.Debug.ToggleCommandMenu.performed += _ => ToggleCommandMenu();
        playerControlSystem.Debug.Screenshot.performed += _ => GameManager.Instance.SystemEffects.TakeScreenshot();
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
        if(currentDebugMenu == null)
        {
            currentDebugMenu = Instantiate(commandMenuPrefab, GameObject.FindGameObjectWithTag("CursorCanvas").transform);
            currentDebugMenu.transform.SetAsLastSibling();
        }
        else
        {
            currentDebugMenu.gameObject.SetActive(currentDebugMenu.gameObject.activeInHierarchy ? false: true);
        }

        if(currentDebugMenu.gameObject.activeInHierarchy)
            currentDebugMenu.ForceActivateInput();
    }
}
