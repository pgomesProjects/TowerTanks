using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TowerTanks.Scripts
{
    public class PlayerData : MonoBehaviour
    {
        //Player name
        internal string playerName { get; private set; }

        //Player components
        public PlayerInput playerInput { get; private set; }
        public PlayerAnalyticsTracker playerAnalyticsTracker { get; private set; }
        public enum PlayerState { SettingUp, NameReady, PickingRooms, PickedRooms, IsBuilding, InTank, ReadyForCombat };
        private PlayerState currentPlayerState;
        private GamepadCursor playerCursor;
        private PlayerMovement currentPlayer;


        //Player input action maps
        public InputActionMap playerInputMap { get; private set; }
        public InputActionMap playerGameCursorMap { get; private set; }
        public InputActionMap playerUIMap { get; private set; }

        //Movement data
        public Vector2 playerMovementData { get; private set; }

        private NamepadController playerNamepad;
        internal bool undoActionAvailable;

        public static Action OnPlayerStateChanged;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            playerCursor = GetComponent<GamepadCursor>();
            playerInputMap = playerInput.actions.FindActionMap("Player");
            playerGameCursorMap = playerInput.actions.FindActionMap("GameCursor");
            playerUIMap = playerInput.actions.FindActionMap("UI");
            playerAnalyticsTracker = GetComponent<PlayerAnalyticsTracker>();
            SetDefaultPlayerName();
            currentPlayerState = PlayerState.NameReady;
        }

        private void OnEnable()
        {
            playerInputMap.actionTriggered += OnPlayerInput;
            playerGameCursorMap.actionTriggered += OnGameCursorInput;
            playerUIMap.actionTriggered += OnUIInput;
        }

        private void OnDisable()
        {
            playerInputMap.actionTriggered -= OnPlayerInput;
            playerGameCursorMap.actionTriggered -= OnGameCursorInput;
            playerUIMap.actionTriggered -= OnUIInput;
        }

        private void OnPlayerInput(InputAction.CallbackContext ctx)
        {
            //Gets the name of the action and calls the appropriate events
            switch (ctx.action.name)
            {
                case "Move": OnMove(ctx); break;
                case "Pause": OnPause(ctx); break;
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
            playerMovementData = ctx.ReadValue<Vector2>();
            playerNamepad?.OnNavigate(playerInput, playerMovementData);
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

        /// <summary>
        /// Spawns a player object in the scene.
        /// </summary>
        /// <param name="playerPos">The world position of the player.</param>
        /// <returns>The player movement component of the player that spawned.</returns>
        public PlayerMovement SpawnPlayerInScene(Vector2 playerPos)
        {
            //Instantiate the player in the scene
            currentPlayer = Instantiate(GameManager.Instance.MultiplayerManager.GetPlayerPrefab());
            currentPlayer.gameObject.name = playerName;
            currentPlayer.GetComponent<Rigidbody2D>().isKinematic = false;
            currentPlayer.transform.position = playerPos;
            currentPlayer.LinkPlayerInput(playerInput);

            //Add the character HUD to the scene
            GameManager.Instance.AddCharacterHUD(currentPlayer);
            //Change the player color
            currentPlayer.transform.GetComponentInChildren<Renderer>().material.SetColor("_Color", GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerInput.playerIndex]);
            //Hide the player cursor
            GameManager.Instance.SetPlayerCursorActive(playerInput.GetComponent<GamepadCursor>(), false);

            //Change the action map
            ChangePlayerActionMap("Player");
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

        /// <summary>
        /// Changes the action map of the player.
        /// </summary>
        /// <param name="actionMapName">The name of the action map.</param>
        public void ChangePlayerActionMap(string actionMapName)
        {
            //Try to switch the action map
            try
            {
                playerInput.currentActionMap.Disable();
                playerInput.SwitchCurrentActionMap(actionMapName);
                playerInput.currentActionMap.Enable();
            }
            catch
            {
                Debug.LogError("Action Map '" + actionMapName + "' could not be found");
            }
        }

        public void SetDefaultPlayerName() => SetPlayerName("Player " + (playerInput.playerIndex + 1).ToString());
        public void SetPlayerName(string playerName)
        {
            this.playerName = playerName;
            playerInput.name = playerName;
        }
        public string GetPlayerHexCode() => "#" + ColorUtility.ToHtmlStringRGBA(GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerInput.playerIndex]);
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
