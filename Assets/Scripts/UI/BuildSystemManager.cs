using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class BuildSystemManager : SerializedMonoBehaviour
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
                bool mounted = roomObject.Mount(true);

                if (!mounted)
                    return null;

                Debug.Log("Room Mounted!");
                //GameManager.Instance.AudioManager.Play("ConnectRoom");

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

        public static BuildSystemManager Instance;

        [SerializeField, Tooltip("Building canvas.")] private Canvas buildingCanvas;
        [SerializeField, Tooltip("The UI that shows the transition between game phases.")] private GamePhaseUI gamePhaseUI;
        [SerializeField, Tooltip("The transform for all of the room pieces.")] private Transform roomParentTransform;
        [SerializeField, Tooltip("The Ready Up Manager that lets players display that they are ready.")] private ReadyUpManager readyUpManager;
        [SerializeField, Tooltip("The delay between the first input made for room movement and repeated tick movement.")] private float roomMoveDelay = 0.5f;
        [SerializeField, Tooltip("The tick rate for moving a room.")] private float roomMoveTickRate = 0.35f;

        private TankController defaultPlayerTank;
        private List<WorldRoom> worldRoomObjects;
        private Stack<PlayerAction> tankBuildHistory;

        public static System.Action<string, string> OnPlayerAction;
        public static System.Action OnPlayerUndo;

        public enum BuildingSubphase { Naming, PickRooms, BuildTank, ReadyUp }
        public BuildingSubphase CurrentSubPhase { get; private set; }

        [Button(ButtonSizes.Medium)]
        private void DebugUndo()
        {
            UndoPlayerAction(tankBuildHistory.Peek().playerInput);
        }

        [Button(ButtonSizes.Medium)]
        private void TestBuildTutorial()
        {
            GameManager.Instance.DisplayTutorial(1);
        }

        [Button(ButtonSizes.Medium)]
        private void TestStackTutorial()
        {
            GameManager.Instance.DisplayTutorial(2);
        }

        [Button(ButtonSizes.Medium)]
        private void TestInteractableTutorial()
        {
            GameManager.Instance.DisplayTutorial(3);
        }

        [Button(ButtonSizes.Medium)]
        private void TestCargoTutorial()
        {
            GameManager.Instance.DisplayTutorial(4);
        }

        [Button(ButtonSizes.Medium)]
        private void TestDemoTutorial()
        {
            GameManager.Instance.DisplayTutorial(5);
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
        }

        // Start is called before the first frame update
        void Start()
        {
            GameManager.Instance?.SetGamepadCursorsActive(true);
            gamePhaseUI?.ShowPhase(GAMESTATE.BUILDING);

            if (GameManager.Instance.tankDesign != null)
                defaultPlayerTank.Build(GameManager.Instance.tankDesign);

            GameManager.Instance.AudioManager.StartBuildMusic();
        }

        private void OnEnable()
        {
            ReadyUpManager.OnAllReady += FinishTank;
            GameManager.Instance.MultiplayerManager.OnPlayerConnected += AddPlayerToBuildSystem;
        }

        private void OnDisable()
        {
            ReadyUpManager.OnAllReady -= FinishTank;
            GameManager.Instance.MultiplayerManager.OnPlayerConnected -= AddPlayerToBuildSystem;
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
            roomObject.heldDuringPlacement = true;
            worldRoomObjects.Add(room);
            MoveRoomInScene(room, Vector2.zero);

            GameManager.Instance.DisplayTutorial(1, false, 3);
        }

        private void Update()
        {
            //If actively in a game menu, return
            if (GameManager.Instance.InGameMenu)
                return;

            foreach (WorldRoom room in worldRoomObjects)
            {
                if (!(room.currentRoomState == WorldRoom.RoomState.MOUNTED))
                    MoveRoom(room);
            }
        }

        /// <summary>
        /// Moves the player's room.
        /// </summary>
        /// <param name="room">The data for the room to move.</param>
        private void MoveRoom(WorldRoom room)
        {
            //Get the movement data from the player
            PlayerData player = PlayerData.ToPlayerData(room.playerSelector.GetCurrentPlayerInput());
            Vector2 playerMovement = player.playerMovementData;

            //If the movement vector is not zero and the room isn't moving, start moving
            if (playerMovement != Vector2.zero && room.currentRoomState == WorldRoom.RoomState.FLOATING)
                room.SetRoomState(WorldRoom.RoomState.MOVING);

            //If the movement vector is zero and the room is moving, stop moving
            else if (playerMovement == Vector2.zero && room.currentRoomState != WorldRoom.RoomState.FLOATING)
            {
                room.SetRoomState(WorldRoom.RoomState.FLOATING);
                room.SetIsMovementRepeating(false);
            }

            switch (room.currentRoomState)
            {
                //If the room is moving, move it and add a cooldown
                case WorldRoom.RoomState.MOVING:
                    MoveRoomInScene(room, playerMovement * 0.25f);
                    room.ResetCooldown(room.isMovementRepeating ? roomMoveTickRate : roomMoveDelay);
                    break;
                //If the room movement is on cooldown, tick the cooldown
                case WorldRoom.RoomState.ONCOOLDOWN:
                    room.UpdateTick();
                    break;
            }
        }

        /// <summary>
        /// Takes the room object in the scene and moves it.
        /// </summary>
        /// <param name="room">The data for the room to move.</param>
        /// <param name="distance">The distance to move the room.</param>
        private void MoveRoomInScene(WorldRoom room, Vector2 distance)
        {
            room.playerSelector.GetCurrentPlayerData().GetGamepadCursor().AdjustCursorPosition(Camera.main.WorldToScreenPoint(room.roomObject.SnapMove((Vector2)Camera.main.ScreenToWorldPoint(room.cursorTransform.position) + distance)), Vector2.zero);
        }

        /// <summary>
        /// Rotates the room.
        /// </summary>
        /// <param name="playerInput">The player rotating the room.</param>
        public void RotateRoom(PlayerInput playerInput)
        {
            //If actively in a game menu, return
            if (GameManager.Instance.InGameMenu)
                return;

            WorldRoom playerRoom = GetPlayerRoom(playerInput);

            //If the room is not mounted, rotate the room
            if (!(playerRoom.currentRoomState == WorldRoom.RoomState.MOUNTED))
            {
                GameManager.Instance.AudioManager.Play("RotateRoom");
                playerRoom.roomObject.Rotate();
            }
        }

        /// <summary>
        /// Mounts the room to the tank.
        /// </summary>
        /// <param name="playerInput">The player mounting their room.</param>
        /// <returns>Returns true if the mount was successful and false if it was not.</returns>
        public bool MountRoom(PlayerInput playerInput)
        {
            //If actively in a game menu, return
            if (GameManager.Instance.InGameMenu)
                return false;

            //If the current building subphase is not in a phase that allows for building, return false
            if (CurrentSubPhase != BuildingSubphase.BuildTank || PlayerData.ToPlayerData(playerInput).GetCurrentPlayerState() != PlayerData.PlayerState.IsBuilding)
                return false;

            //Get the room from the player and mount it
            WorldRoom playerRoom = GetPlayerRoom(playerInput);
            Room mountedRoom = playerRoom.Mount();

            //If there is no room, return false
            if (mountedRoom == null)
                return false;

            mountedRoom.heldDuringPlacement = false;

            //Add the room to the stats
            defaultPlayerTank.AddRoomToStats(mountedRoom);

            //Add the room mounting to the player action history
            AddToPlayerActionHistory(playerInput, playerRoom.playerSelector.GetRoomAt(playerRoom.playerSelector.GetNumberOfRoomsPlaced() - 1), mountedRoom);

            //Update all other rooms so that overlapping rooms cannot be placed:
            foreach(WorldRoom room in worldRoomObjects) //Iterate through each held room
            {
                if (!room.roomObject.mounted) room.roomObject.SnapMove(); //Snap room to closest gridpoint, updating ghost couplers and placeability
            }

            //If all of the rooms are mounted from the player
            if (playerRoom.currentRoomState == WorldRoom.RoomState.MOUNTED)
            {
                //Spawn the player in the tank
                TankController defaultTank = FindObjectOfType<TankController>();
                Vector3 playerPos = defaultTank.GetPlayerSpawnPointPosition();
                playerPos.x += Random.Range(-0.25f, 0.25f);

                PlayerMovement playerObject = playerRoom.playerSelector.GetCurrentPlayerData().SpawnPlayerInScene(playerPos);
                playerObject.SetAssignedTank(defaultTank);
                PlayerData.ToPlayerData(playerInput).SetPlayerState(PlayerData.PlayerState.InTank);

                //If all rooms from all players are mounted, note that all of them are ready and start the ready up manager
                if (AllRoomsMounted())
                {
                    UpdateBuildPhase(BuildingSubphase.ReadyUp);
                    foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
                    {
                        player.SetPlayerState(PlayerData.PlayerState.ReadyForCombat);
                    }
                    readyUpManager.Init();
                    GameManager.Instance.DisplayTutorial(2, false, 3);
                }

                return true;
            }
            //If the player has not mounted all of their rooms, give them their next room
            else
            {
                playerRoom.SetRoomObject(Instantiate(playerRoom.playerSelector.GetRoomToPlace(), roomParentTransform));
                MoveRoomInScene(playerRoom, Vector2.zero);
            }

            return false;
        }

        /// <summary>
        /// Adds a player action to the action history.
        /// </summary>
        /// <param name="playerInput">The player performing the action.</param>
        /// <param name="currentRoomInfo">The room information.</param>
        /// <param name="mountedRoom">The room object mounted.</param>
        private void AddToPlayerActionHistory(PlayerInput playerInput, RoomInfo currentRoomInfo, Room mountedRoom)
        {
            tankBuildHistory.Push(new PlayerAction(playerInput, mountedRoom));
            OnPlayerAction?.Invoke(playerInput.name, currentRoomInfo.name);
        }

        /// <summary>
        /// Undoes the player's most recent action.
        /// </summary>
        /// <param name="playerInput">The player undoing.</param>
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

                //Remove the room from the stats
                defaultPlayerTank.RemoveRoomFromStats(currentAction.room);

                //Update the UI
                OnPlayerUndo?.Invoke();

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
                playerRoom.roomObject.heldDuringPlacement = true;
                MoveRoomInScene(playerRoom, Vector2.zero);
                playerRoom.SetRoomState(WorldRoom.RoomState.FLOATING);

                //If the player is ready for combat, revert that status and ensure the ready up manager is hidden
                if(playerData.GetCurrentPlayerState() == PlayerData.PlayerState.ReadyForCombat)
                {
                    playerData.SetPlayerState(PlayerData.PlayerState.IsBuilding);
                    playerData.RemovePlayerFromScene();
                    readyUpManager.HideReadyUpManager();
                    UpdateBuildPhase(BuildingSubphase.BuildTank);
                }
            }
        }

        /// <summary>
        /// Finalizes the tank design by saving the design to the GameManager.
        /// </summary>
        private void FinishTank()
        {
            TankDesign currentTankDesign = defaultPlayerTank.GetCurrentDesign();
            GameManager.Instance.tankDesign = currentTankDesign;
            gamePhaseUI?.ShowPhase(GAMESTATE.COMBAT);
        }

        /// <summary>
        /// Get the player's room from the list of room objects in the world.
        /// </summary>
        /// <param name="playerInput">The player to get the room of.</param>
        /// <returns>Returns the world room data, if found. Returns null otherwise.</returns>
        private WorldRoom GetPlayerRoom(PlayerInput playerInput)
        {
            for (int i = 0; i < worldRoomObjects.Count; i++)
            {
                if (worldRoomObjects[i].playerSelector.GetCurrentPlayerInput().playerIndex == playerInput.playerIndex)
                    return worldRoomObjects[i];
            }

            return null;
        }

        /// <summary>
        /// Checks to see if all rooms have been mounted.
        /// </summary>
        /// <returns>Returns true if all rooms are mounted. Returns false if otherwise.</returns>
        public bool AllRoomsMounted()
        {
            foreach (WorldRoom room in worldRoomObjects)
            {
                if (!(room.currentRoomState == WorldRoom.RoomState.MOUNTED))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a player to the build scene after connecting.
        /// </summary>
        /// <param name="playerInput">The player to add to the build scene.</param>
        public void AddPlayerToBuildSystem(PlayerInput playerInput)
        {
            switch (CurrentSubPhase)
            {
                //If the rooms have all been picked, immediately spawn them in the tank
                case BuildingSubphase.BuildTank:
                case BuildingSubphase.ReadyUp:
                    TankController defaultTank = FindObjectOfType<TankController>();
                    Vector3 playerPos = defaultTank.GetPlayerSpawnPointPosition();
                    playerPos.x += Random.Range(-0.25f, 0.25f);
                    PlayerData playerData = PlayerData.ToPlayerData(playerInput);
                    PlayerMovement playerObject = playerData.SpawnPlayerInScene(playerPos);
                    playerObject.SetAssignedTank(defaultTank);
                    playerData.SetPlayerState(PlayerData.PlayerState.ReadyForCombat);
                    if (CurrentSubPhase == BuildingSubphase.ReadyUp)
                        readyUpManager.Init();
                    break;
            }
        }

        public void UpdateBuildPhase(BuildingSubphase newPhase) => CurrentSubPhase = newPhase;
        public ReadyUpManager GetReadyUpManager() => readyUpManager;
        public void RefreshPlayerTankName() => defaultPlayerTank.SetTankName(CampaignManager.Instance.PlayerTankName);
    }
}
