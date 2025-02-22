using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class PlayerData : MonoBehaviour
    {
        public PlayerInput playerInput { get; private set; }
        public InputActionMap playerInputMap { get; private set; }
        public InputActionMap playerGameCursorMap { get; private set; }
        public InputActionMap playerUIMap { get; private set; }
        public Vector2 cursorMovementData { get; private set; }
        public Vector2 playerMovementData { get; private set; }

        public enum PlayerState { SettingUp, NameReady, PickingRooms, PickedRooms, IsBuilding, InTank, ReadyForCombat };
        private PlayerState currentPlayerState;
        internal bool undoActionAvailable;

        private NamepadController playerNamepad;
        private string playerName;
        private PlayerMovement currentPlayer;
        private GamepadCursor playerCursor;

        public static Action OnPlayerStateChanged;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerCursor = GetComponent<GamepadCursor>();
            playerInputMap = playerInput.actions.FindActionMap("Player");
            playerGameCursorMap = playerInput.actions.FindActionMap("GameCursor");
            playerUIMap = playerInput.actions.FindActionMap("UI");
            SetDefaultPlayerName();
            currentPlayerState = PlayerState.NameReady;
        }

        private void OnEnable()
        {
            playerInputMap.actionTriggered += OnPlayerInput;
            playerGameCursorMap.actionTriggered += OnGameCursorInput;
            playerUIMap.actionTriggered += OnUIInput;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            playerInputMap.actionTriggered -= OnPlayerInput;
            playerGameCursorMap.actionTriggered -= OnGameCursorInput;
            playerUIMap.actionTriggered -= OnUIInput;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {

        }

        private void OnPlayerInput(InputAction.CallbackContext ctx)
        {
            //Gets the name of the action and calls the appropriate events
            switch (ctx.action.name)
            {
                case "Move": OnMove(ctx); break;
                case "Pause": OnPause(ctx); break;
                case "Rotate": OnRotate(ctx); break;
                case "Mount": OnMount(ctx); break;
                case "ReadyUp": OnReadyUp(ctx); break;
            }
        }

        private void OnGameCursorInput(InputAction.CallbackContext ctx)
        {
            //Gets the name of the action and calls the appropriate events
            switch (ctx.action.name)
            {
                case "Navigate": OnNavigate(ctx); break;
                case "Pause": OnPause(ctx); break;
                case "Submit": OnSubmit(ctx); break;
                case "Cancel": OnCancel(ctx); break;
                case "Rotate": OnRotate(ctx); break;
                case "Mount": OnMount(ctx); break;
            }
        }

        private void OnUIInput(InputAction.CallbackContext ctx)
        {
            //Gets the name of the action and calls the appropriate events
            switch (ctx.action.name)
            {
                case "Submit": OnSubmit(ctx); break;
            }
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            playerMovementData = ctx.ReadValue<Vector2>();
        }

        private void OnPause(InputAction.CallbackContext ctx)
        {
            //If the player is not in the game, ignore
            if (SceneManager.GetActiveScene().name == "Title")
                return;

            //If the player presses the pause button
            if (ctx.started)
            {
                //Toggle the pause menu
                PauseController.Instance?.PauseToggle(playerInput.playerIndex);
            }
        }

        private void OnRotate(InputAction.CallbackContext ctx)
        {
            if (currentPlayerState == PlayerState.IsBuilding && ctx.started)
            {
                BuildSystemManager.Instance.RotateRoom(playerInput);
            }
        }

        private void OnMount(InputAction.CallbackContext ctx)
        {
            if (currentPlayerState == PlayerState.IsBuilding && ctx.started)
            {
                BuildSystemManager.Instance.MountRoom(playerInput);
            }
        }

        private void OnUndo(InputAction.CallbackContext ctx)
        {
/*            if (ctx.performed)
            {
                if (!undoActionAvailable)
                    return;

                if (currentPlayerState == PlayerState.IsBuilding || currentPlayerState == PlayerState.ReadyForCombat)
                    BuildSystemManager.Instance.UndoPlayerAction(playerInput);
            }

            if (ctx.canceled)
                undoActionAvailable = true;*/
        }

        private void OnReadyUp(InputAction.CallbackContext ctx)
        {
            if (currentPlayerState == PlayerState.ReadyForCombat && ctx.started) 
            {
                bool isPlayerReady = !BuildSystemManager.Instance.GetReadyUpManager().IsPlayerReady(playerInput.playerIndex);
                BuildSystemManager.Instance.GetReadyUpManager().ReadyPlayer(playerInput.playerIndex, isPlayerReady);
            }
        }

        private void OnNavigate(InputAction.CallbackContext ctx)
        {
            cursorMovementData = ctx.ReadValue<Vector2>();
            playerNamepad?.OnNavigate(playerInput, cursorMovementData);
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
                playerNamepad?.Cancel();
            }
        }

        public PlayerMovement SpawnPlayerInScene(Vector2 playerPos)
        {
            currentPlayer = Instantiate(GameManager.Instance.MultiplayerManager.GetPlayerPrefab());
            currentPlayer.gameObject.name = playerName;
            currentPlayer.LinkPlayerInput(playerInput);
            currentPlayer.GetComponent<Rigidbody2D>().isKinematic = false;
            currentPlayer.transform.position = playerPos;
            currentPlayer.transform.GetComponentInChildren<Renderer>().material.SetColor("_Color", GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerInput.playerIndex]);
            GameManager.Instance.SetPlayerCursorActive(playerInput.GetComponent<GamepadCursor>(), false);
            GameManager.Instance.AddCharacterHUD(currentPlayer);
            return currentPlayer;
        }

        public void RemovePlayerFromScene()
        {
            if(currentPlayer != null)
                StartCoroutine(RemovePlayerAtEndOfFrame());
        }

        private IEnumerator RemovePlayerAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            Destroy(currentPlayer.gameObject);
            currentPlayer = null;
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

        public void ChangePlayerActionMap(string actionMapName)
        {
            playerInput.currentActionMap.Disable();
            playerInput.SwitchCurrentActionMap(actionMapName);
            playerInput.currentActionMap.Enable();
        }

        public void SetDefaultPlayerName() => SetPlayerName("Player " + (playerInput.playerIndex + 1).ToString());
        public string GetPlayerName() => playerName;
        public void SetPlayerName(string playerName)
        {
            this.playerName = playerName;
            playerInput.name = playerName;
        }
        public PlayerMovement GetCurrentPlayerObject() => currentPlayer;
        public PlayerState GetCurrentPlayerState() => currentPlayerState;
        public void SetPlayerState(PlayerState currentPlayerState)
        {
            this.currentPlayerState = currentPlayerState;
            OnPlayerStateChanged?.Invoke();
        }
        public void SetNamepad(NamepadController namepadController) => playerNamepad = namepadController;
        public GamepadCursor GetGamepadCursor() => playerCursor;
    }
}
