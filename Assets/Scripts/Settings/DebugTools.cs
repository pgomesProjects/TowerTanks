using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTools : MonoBehaviour
{
    private PlayerControlSystem playerControlSystem;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
        playerControlSystem.Debug.ToggleGamepadCursors.performed += _ => OnToggleGamepadCursors();
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
}
