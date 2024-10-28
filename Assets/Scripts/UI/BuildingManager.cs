using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class BuildingManager : SerializedMonoBehaviour
    {
        private class WorldRoom
        {
            public PlayerRoomSelection playerSelector { get; private set; }
            public RectTransform cursorTransform { get; private set; }
            public Room roomObject { get; private set; }
            public Transform roomTransform { get; private set; }

            public enum RoomState { FLOATING, MOVING, ONCOOLDOWN, MOUNTED };
            public RoomState currentRoomState { get; private set; }
            public bool isMovementRepeating { get; private set; }

            private float elapsedTime;
            private float cooldownTimer;

            public WorldRoom(PlayerRoomSelection playerSelector, Room roomObject, Transform roomTransform)
            {
                this.playerSelector = playerSelector;
                this.roomObject = roomObject;
                cursorTransform = playerSelector.GetCurrentPlayerInput().GetComponent<GamepadCursor>().GetCursorTransform();
                this.roomTransform = roomTransform;
            }

            public Room Mount()
            {
                bool mounted = roomObject.Mount();

                if (!mounted)
                    return null;

                Debug.Log("Room Mounted!");
                GameManager.Instance.AudioManager.Play("ConnectRoom");

                playerSelector.MountRoom();
                if (playerSelector.AllRoomsMounted())
                    currentRoomState = RoomState.MOUNTED;

                return roomObject;
            }

            public void UpdateTick()
            {
                elapsedTime += Time.deltaTime;

                if (elapsedTime >= cooldownTimer)
                {
                    currentRoomState = RoomState.MOVING;
                    if (!isMovementRepeating)
                        isMovementRepeating = true;
                }
            }

            public void SetRoomObject(Room roomObject) => this.roomObject = roomObject;

            public void ResetCooldown(float cooldownTimer)
            {
                currentRoomState = RoomState.ONCOOLDOWN;
                elapsedTime = 0f;
                this.cooldownTimer = cooldownTimer;
            }
            public void SetRoomState(RoomState currentRoomState) => this.currentRoomState = currentRoomState;
            public void SetIsMovementRepeating(bool isMovementRepeating) => this.isMovementRepeating = isMovementRepeating;
        }

        public static BuildingManager Instance;

        [SerializeField, Tooltip("Building canvas.")] private Canvas buildingCanvas;
        [SerializeField, Tooltip("The UI that shows the transition between game phases.")] private GamePhaseUI gamePhaseUI;
        [SerializeField, Tooltip("The transform for all of the room pieces.")] private Transform roomParentTransform;
        [SerializeField, Tooltip("The Spawn Point for all players in the build scene.")] private Transform spawnPoint;
        [SerializeField, Tooltip("The player action container.")] private RectTransform playerActionContainer;
        [SerializeField, Tooltip("The player action prefab.")] private GameObject playerActionPrefab;
        [SerializeField, Tooltip("The color for the most recent action.")] private Color mostRecentActionColor;
        [SerializeField, Tooltip("The Ready Up Manager that lets players display that they are ready.")] private ReadyUpManager readyUpManager;
        [SerializeField, Tooltip("The delay between the first input made for room movement and repeated tick movement.")] private float roomMoveDelay = 0.5f;
        [SerializeField, Tooltip("The tick rate for moving a room.")] private float roomMoveTickRate = 0.35f;

        private TankController defaultPlayerTank;
        private Color defaultPlayerActionColor;

        private List<WorldRoom> worldRoomObjects;
        private Stack<PlayerAction> tankBuildHistory;
        private RectTransform historyParentTransform;

        [Button(ButtonSizes.Medium)]
        private void DebugUndo()
        {
            UndoPlayerAction(tankBuildHistory.Peek().playerInput);
        }

        private struct PlayerAction
        {
            public PlayerInput playerInput;
            public Room room;

            public PlayerAction(PlayerInput playerInput, Room room)
            {
                this.playerInput = playerInput;
                this.room = room;
            }
        }

        private void Awake()
        {
            Instance = this;
            worldRoomObjects = new List<WorldRoom>();
            tankBuildHistory = new Stack<PlayerAction>();
            defaultPlayerTank = FindObjectOfType<TankController>();
            defaultPlayerActionColor = playerActionPrefab.GetComponentInChildren<Image>().color;
            historyParentTransform = playerActionContainer.parent.GetComponent<RectTransform>();
        }

        // Start is called before the first frame update
        void Start()
        {
            GameManager.Instance?.SetGamepadCursorsActive(true);
            gamePhaseUI?.ShowPhase(GAMESTATE.BUILDING);

            if (GameManager.Instance.tankDesign != null)
                defaultPlayerTank.Build(GameManager.Instance.tankDesign);
        }

        private void OnEnable()
        {
            ReadyUpManager.OnAllReady += FinishTank;
        }

        private void OnDisable()
        {
            ReadyUpManager.OnAllReady -= FinishTank;
        }

        /// <summary>
        /// Spawns a room in the world that the players can move around.
        /// </summary>
        /// <param name="roomToSpawn">The room to spawn into the world.</param>
        /// <param name="playerSelector">The player to follow.</param>
        public void SpawnRoom(RoomInfo roomToSpawn, PlayerRoomSelection playerSelector)
        {
            Room roomObject = Instantiate(roomToSpawn.roomObject, roomParentTransform);
            WorldRoom room = new WorldRoom(playerSelector, roomObject, roomObject.transform);
            worldRoomObjects.Add(room);
            MoveRoomInScene(room, Vector2.zero);
        }

        private void Update()
        {
            foreach (WorldRoom room in worldRoomObjects)
            {
                if (!(room.currentRoomState == WorldRoom.RoomState.MOUNTED))
                    MoveRoom(room);
            }
        }

        private void MoveRoom(WorldRoom room)
        {
            PlayerData player = PlayerData.ToPlayerData(room.playerSelector.GetCurrentPlayerInput());
            Vector2 playerMovement = player.movementData;

            if (playerMovement != Vector2.zero && room.currentRoomState == WorldRoom.RoomState.FLOATING)
                room.SetRoomState(WorldRoom.RoomState.MOVING);
            else if (playerMovement == Vector2.zero && room.currentRoomState != WorldRoom.RoomState.FLOATING)
            {
                room.SetRoomState(WorldRoom.RoomState.FLOATING);
                room.SetIsMovementRepeating(false);
            }

            switch (room.currentRoomState)
            {
                case WorldRoom.RoomState.MOVING:
                    MoveRoomInScene(room, playerMovement * 0.25f);
                    room.ResetCooldown(room.isMovementRepeating ? roomMoveTickRate : roomMoveDelay);
                    break;
                case WorldRoom.RoomState.ONCOOLDOWN:
                    room.UpdateTick();
                    break;
            }
        }

        private void MoveRoomInScene(WorldRoom room, Vector2 distance)
        {
            room.cursorTransform.position = Camera.main.WorldToScreenPoint(
                room.roomObject.SnapMove((Vector2)Camera.main.ScreenToWorldPoint(room.cursorTransform.position) + distance));
        }

        public void RotateRoom(PlayerInput playerInput)
        {
            WorldRoom playerRoom = GetPlayerRoom(playerInput);

            if (!(playerRoom.currentRoomState == WorldRoom.RoomState.MOUNTED))
            {
                GameManager.Instance.AudioManager.Play("RotateRoom");
                playerRoom.roomObject.Rotate();
            }
        }

        public bool MountRoom(PlayerInput playerInput)
        {
            WorldRoom playerRoom = GetPlayerRoom(playerInput);
            Room mountedRoom = playerRoom.Mount();

            if (mountedRoom == null)
                return false;

            AddToPlayerActionHistory(playerInput, playerRoom.playerSelector.GetRoomAt(playerRoom.playerSelector.GetNumberOfRoomsPlaced() - 1), mountedRoom);

            if (playerRoom.currentRoomState == WorldRoom.RoomState.MOUNTED)
            {

                Vector3 playerPos = spawnPoint.position;
                playerPos.x += Random.Range(-0.25f, 0.25f);
                playerRoom.playerSelector.GetCurrentPlayerData().SpawnPlayerInScene(playerPos);

                if (AllRoomsMounted())
                {
                    foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
                        player.SetPlayerState(PlayerData.PlayerState.ReadyForCombat);

                    readyUpManager.Init();
                }

                return true;
            }
            else
            {
                playerRoom.SetRoomObject(Instantiate(playerRoom.playerSelector.GetRoomToPlace(), roomParentTransform));
                MoveRoomInScene(playerRoom, Vector2.zero);
            }

            return false;
        }

        private void AddToPlayerActionHistory(PlayerInput playerInput, RoomInfo currentRoomInfo, Room mountedRoom)
        {
            if (tankBuildHistory.Count != 0)
                playerActionContainer.GetChild(playerActionContainer.childCount - 1).GetComponentInChildren<Image>().color = defaultPlayerActionColor;

            GameObject newAction = Instantiate(playerActionPrefab, playerActionContainer);
            newAction.GetComponentInChildren<TextMeshProUGUI>().text = playerInput.name + " Placed " + currentRoomInfo.name;
            newAction.GetComponentInChildren<Image>().color = mostRecentActionColor;
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentTransform);

            tankBuildHistory.Push(new PlayerAction(playerInput, mountedRoom));
        }

        public void UndoPlayerAction(PlayerInput playerInput)
        {
            if (tankBuildHistory.Count == 0)
            {
                Debug.Log("No actions to undo.");
                return;
            }

            PlayerAction currentAction = tankBuildHistory.Peek();
            PlayerData playerData = PlayerData.ToPlayerData(playerInput);

            //If the most current action was done by the player trying to undo, then undo
            if (currentAction.playerInput == playerInput)
            {
                //Dismount the room from the tank
                tankBuildHistory.Pop();
                currentAction.room.Dismount();
                Destroy(currentAction.room.gameObject);

                //Update the action container
                if (playerActionContainer.childCount - 2 >= 0)
                    playerActionContainer.GetChild(playerActionContainer.childCount - 2).GetComponentInChildren<Image>().color = mostRecentActionColor;

                if (playerActionContainer.childCount > 0)
                    Destroy(playerActionContainer.GetChild(playerActionContainer.childCount - 1).gameObject);

                //Revert to the previous room placed
                WorldRoom playerRoom = GetPlayerRoom(playerInput);
                playerRoom.playerSelector.UndoRoomPlaced();

                //Show the gamepad cursor and instantiate the room object
                GamepadCursor gamepadCursor = playerData.playerInput.GetComponent<GamepadCursor>();
                GameManager.Instance.SetPlayerCursorActive(gamepadCursor, true);
                gamepadCursor.SetCursorMove(false);
                gamepadCursor.transform.position = Vector2.zero;

                //Destroy the current room object selected if it exists and replace it with the previous one
                if (playerRoom.roomObject != null)
                    Destroy(playerRoom.roomObject.gameObject);
                playerRoom.SetRoomObject(Instantiate(playerRoom.playerSelector.GetRoomToPlace(), roomParentTransform));
                MoveRoomInScene(playerRoom, Vector2.zero);
                playerRoom.SetRoomState(WorldRoom.RoomState.FLOATING);

                //If the player is ready for combat, revert that status and ensure the ready up manager is hidden
                if(playerData.GetCurrentPlayerState() == PlayerData.PlayerState.ReadyForCombat)
                {
                    playerData.SetPlayerState(PlayerData.PlayerState.IsBuilding);
                    playerData.RemovePlayerFromScene();
                    readyUpManager.HideReadyUpManager();
                }
            }
        }

        private void FinishTank()
        {
            TankDesign currentTankDesign = defaultPlayerTank.GetCurrentDesign();
            GameManager.Instance.tankDesign = currentTankDesign;
            gamePhaseUI?.ShowPhase(GAMESTATE.COMBAT);
        }

        private WorldRoom GetPlayerRoom(PlayerInput playerInput)
        {
            for (int i = 0; i < worldRoomObjects.Count; i++)
            {
                if (worldRoomObjects[i].playerSelector.GetCurrentPlayerInput().playerIndex == playerInput.playerIndex)
                    return worldRoomObjects[i];
            }

            return null;
        }

        public bool AllRoomsMounted()
        {
            foreach (WorldRoom room in worldRoomObjects)
            {
                if (!(room.currentRoomState == WorldRoom.RoomState.MOUNTED))
                    return false;
            }

            return true;
        }

        public ReadyUpManager GetReadyUpManager() => readyUpManager;
        public void RefreshPlayerTankName() => defaultPlayerTank.SetTankName(CampaignManager.Instance.PlayerTankName);
    }
}
