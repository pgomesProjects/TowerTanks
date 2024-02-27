using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerData : MonoBehaviour
{
    private PlayerInput currentInput;
    private GamepadCursor currentCursor;

    private void Awake()
    {
        currentInput = GetComponent<PlayerInput>();
        currentCursor = GetComponent<GamepadCursor>();
    }
}
