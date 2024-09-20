using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerData : MonoBehaviour
{
    public PlayerInput playerInput { get; private set; }
    public InputActionMap playerInputMap { get; private set; }
    public InputActionMap playerUIMap { get; private set; }
    public Vector2 movementData { get; private set; }

    public enum PlayerState { SettingUp, NameReady, PickingRooms, PickedRooms, IsBuilding, ReadyForCombat };
    private PlayerState currentPlayerState;

    private NamepadController playerNamepad;
    private string playerName;

    public static Action OnPlayerStateChanged;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerInputMap = playerInput.actions.FindActionMap("Player");
        playerUIMap = playerInput.actions.FindActionMap("GameCursor");
        SetDefaultPlayerName();
        currentPlayerState = PlayerState.NameReady;
    }

    private void OnEnable()
    {
        playerInputMap.actionTriggered += OnPlayerInput;
        playerUIMap.actionTriggered += OnPlayerUIInput;
    }

    private void OnDisable()
    {
        playerInputMap.actionTriggered -= OnPlayerInput;
        playerUIMap.actionTriggered -= OnPlayerUIInput;
    }

    private void OnPlayerInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "Move": OnMove(ctx); break;
            case "Rotate": OnRotate(ctx); break;
            case "Mount": OnMount(ctx); break;
            case "ReadyUp": OnReadyUp(ctx); break;
        }
    }

    private void OnPlayerUIInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "Navigate": OnNavigate(ctx); break;
            case "Submit": OnSubmit(ctx); break;
            case "Cancel": OnCancel(ctx); break;
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        movementData = ctx.ReadValue<Vector2>();
    }

    private void OnRotate(InputAction.CallbackContext ctx)
    {
        if (currentPlayerState == PlayerState.IsBuilding && ctx.started)
        {
            BuildingManager.Instance.RotateRoom(playerInput);
        }
    }

    private void OnMount(InputAction.CallbackContext ctx)
    {
        if (currentPlayerState == PlayerState.IsBuilding && ctx.started)
        {
            BuildingManager.Instance.MountRoom(playerInput);
        }
    }

    private void OnReadyUp(InputAction.CallbackContext ctx)
    {
        if (currentPlayerState == PlayerState.ReadyForCombat && ctx.started)
        {
            BuildingManager.Instance.GetReadyUpManager().ReadyPlayer(playerInput.playerIndex, !BuildingManager.Instance.GetReadyUpManager().IsPlayerReady(playerInput.playerIndex));
        }
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        playerNamepad?.OnNavigate(playerInput, ctx.ReadValue<Vector2>());
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            playerNamepad?.SelectCurrentButton(playerInput);
        }
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            playerNamepad?.HideGamepad();
        }
    }

    public static PlayerData ToPlayerData(PlayerInput playerInput)
    {
        foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
        {
            if (player.playerInput == playerInput)
                return player;
        }

        Debug.Log("No Player Data Found.");

        return null;
    }

    public void SetDefaultPlayerName() => SetPlayerName("Player " + (playerInput.playerIndex + 1).ToString());

    public string GetPlayerName() => playerName;
    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;
        playerInput.name = playerName;
    }

    public PlayerState GetCurrentPlayerState() => currentPlayerState;
    public void SetPlayerState(PlayerState currentPlayerState)
    {
        this.currentPlayerState = currentPlayerState;
        OnPlayerStateChanged?.Invoke();
    }
    public void SetNamepad(NamepadController namepadController) => playerNamepad = namepadController;
}
