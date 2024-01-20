using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class MultiplayerManager : MonoBehaviour
{
    internal PlayerInputManager playerInputManager { get; private set; }
    internal static bool[] connectedControllers;

    [SerializeField, Tooltip("The list of colors indicating the player number.")] public Color[] playerColors { get; private set; } = { Color.red, Color.blue, Color.yellow, Color.green };
    [SerializeField, Tooltip("The player character prefab.")] private PlayerController playerPrefab;

    public Action<PlayerInput> OnPlayerConnected;
    public Action<int> OnPlayerLost, OnPlayerRegained;

    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();

        connectedControllers = new bool[playerInputManager.maxPlayerCount];
        for (int i = 0; i < connectedControllers.Length; i++)
            connectedControllers[i] = false;
        playerInputManager.EnableJoining();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Delegate join function
        playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    /// <summary>
    /// Behavior for when a player joins the game
    /// </summary>
    /// <param name="playerInput">The player input component created when joining.</param>
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.transform.SetParent(transform);
        playerInput.defaultControlScheme = playerInput.currentControlScheme;

        //Generate the new player's index
        int playerIndex = ConnectionController.CheckForIndex();
        connectedControllers[playerIndex] = true;

        //Change the color of the player's gamepad cursor
        playerInput.GetComponent<GamepadCursor>().CreateGamepadCursor(playerColors[playerIndex]);

        playerInput.name = "Player " + (playerIndex + 1).ToString();

        playerInput.onDeviceLost += OnDeviceLost;
        playerInput.onDeviceRegained += OnDeviceRegained;

        OnPlayerConnected?.Invoke(playerInput);
    }

    public void SetUIControlScheme()
    {
        GameSettings.controlSchemeUI = transform.GetComponentInChildren<PlayerInput>().currentControlScheme;
    }

    private void OnDeviceLost(PlayerInput playerInput)
    {
        playerInput.gameObject.SetActive(false);
        OnPlayerLost?.Invoke(playerInput.playerIndex);
    }

    private void OnDeviceRegained(PlayerInput playerInput)
    {
        playerInput.gameObject.SetActive(true);
        OnPlayerRegained?.Invoke(playerInput.playerIndex);
    }

    public Color[] GetPlayerColors() => playerColors;
    public PlayerInput[] GetPlayerInputs() => transform.GetComponentsInChildren<PlayerInput>();
    public PlayerController GetPlayerPrefab() => playerPrefab;
}
