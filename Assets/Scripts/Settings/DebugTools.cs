using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTools : MonoBehaviour
{
    [SerializeField, Tooltip("The command menu to spawn.")] private CheatInputField commandMenuPrefab;
    private PlayerControlSystem playerControlSystem;
    private CheatInputField currentDebugMenu;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
        playerControlSystem.Debug.ToggleGamepadCursors.performed += _ => OnToggleGamepadCursors();
        playerControlSystem.Debug.ToggleCommandMenu.performed += _ => ToggleCommandMenu();
    }

    private void OnEnable()
    {
        playerControlSystem?.Enable();
    }

    private void OnDisable()
    {
        playerControlSystem?.Disable();
    }

    private void OnToggleGamepadCursors()
    {
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
    }
}
