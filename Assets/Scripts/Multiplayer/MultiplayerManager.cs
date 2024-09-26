using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

[RequireComponent(typeof(PlayerInputManager))]
public class MultiplayerManager : SerializedMonoBehaviour
{
    internal PlayerInputManager playerInputManager { get; private set; }
    internal static bool[] connectedControllers;

    [SerializeField, Tooltip("The list of colors indicating the player number.")] public Color[] playerColors { get; private set; } = { Color.red, Color.blue, Color.yellow, Color.green };
    [SerializeField, Tooltip("The player character prefab.")] private PlayerMovement playerPrefab;

    public Action<PlayerInput> OnPlayerConnected;
    public Action<int> OnPlayerLost, OnPlayerRegained;

    [Button(ButtonSizes.Medium)]
    private void ToggleMultiplayerDebug()
    {
        MultiplayerEnabled = !MultiplayerEnabled;

        if(MultiplayerEnabled)
            playerInputManager?.EnableJoining();
        if (!MultiplayerEnabled)
            playerInputManager?.DisableJoining();
    }
    public bool MultiplayerEnabled;

    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();

        connectedControllers = new bool[playerInputManager.maxPlayerCount];
        for (int i = 0; i < connectedControllers.Length; i++)
            connectedControllers[i] = false;
        playerInputManager.EnableJoining();

        if (MultiplayerEnabled)
            playerInputManager?.EnableJoining();
        if (!MultiplayerEnabled)
            playerInputManager?.DisableJoining();
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
        Debug.Log("Connecting Player " + (playerIndex + 1).ToString() + "...");
        connectedControllers[playerIndex] = true;

        //Change the color of the player's gamepad cursor
        playerInput.GetComponent<GamepadCursor>()?.CreateGamepadCursor(playerColors[playerIndex]);

        playerInput.name = "Player " + (playerIndex + 1).ToString();

        playerInput.onDeviceLost += OnDeviceLost;
        playerInput.onDeviceRegained += OnDeviceRegained;

        //Disable game input if the cheats menu is active
        if (GameManager.Instance.CheatsMenuActive)
            SetPlayerGameInputActive(playerInput, false);

        //Call OnPlayerConnected on the next frame
        StartCoroutine(InvokeOnConnected(playerInput));
    }

    private IEnumerator InvokeOnConnected(PlayerInput playerInput)
    {
        yield return null;
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

    public void EnableDebugInput()
    {
        foreach (PlayerInput player in GetPlayerInputs())
            SetPlayerGameInputActive(player, false);

        GameManager.Instance.SetCheatsMenuActive(true);
    }

    private void SetPlayerGameInputActive(PlayerInput player, bool isActive)
    {
        if (isActive)
        {
            // Enable the Player, GameCursor, and UI action maps
            player.actions.FindActionMap("Player").Enable();
            player.actions.FindActionMap("GameCursor").Enable();
            player.actions.FindActionMap("UI").Enable();

            // Disable the Debug action map
            player.actions.FindActionMap("Debug").Disable();
        }
        else
        {
            // Disable the Player, GameCursor, and UI action maps
            player.actions.FindActionMap("Player").Disable();
            player.actions.FindActionMap("GameCursor").Disable();
            player.actions.FindActionMap("UI").Disable();

            // Enable the Debug action map
            player.actions.FindActionMap("Debug").Enable();
        }
    }

    public void DisableDebugInput()
    {
        foreach (PlayerInput player in GetPlayerInputs())
            SetPlayerGameInputActive(player, true);

        GameManager.Instance.SetCheatsMenuActive(false);
    }

    public Color[] GetPlayerColors() => playerColors;
    public PlayerInput[] GetPlayerInputs() => transform.GetComponentsInChildren<PlayerInput>();
    public PlayerData[] GetAllPlayers() => transform.GetComponentsInChildren<PlayerData>();
    public PlayerMovement GetPlayerPrefab() => playerPrefab;
}
