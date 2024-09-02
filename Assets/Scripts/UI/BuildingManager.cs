using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevel;
using UnityEngine.UIElements;

public class BuildingManager : MonoBehaviour
{
    private class WorldRoom
    {
        public PlayerRoomSelection playerSelector { get; private set; }
        public RectTransform cursorTransform { get; private set; }
        public Room roomObject { get; private set; }
        public Transform roomTransform { get; private set; }
        public bool mountingComplete { get; private set; }

        public WorldRoom(PlayerRoomSelection playerSelector, Room roomObject, Transform roomTransform)
        {
            this.playerSelector = playerSelector;
            this.roomObject = roomObject;
            cursorTransform = playerSelector.GetCurrentPlayerInput().GetComponent<GamepadCursor>().GetCursorTransform();
            this.roomTransform = roomTransform;
        }

        public void Mount()
        {
            roomObject.Mount();
            Debug.Log("Room Mounted!");
            GameManager.Instance.AudioManager.Play("ConnectRoom");

            playerSelector.MountRoom();
            mountingComplete = playerSelector.AllRoomsMounted();
        }

        public void SetRoomObject(Room roomObject)
        {
            this.roomObject = roomObject;
        }
    }

    [SerializeField, Tooltip("Building canvas.")] private Canvas buildingCanvas;
    [SerializeField, Tooltip("The UI that shows the transition between game phases.")] private GamePhaseUI gamePhaseUI;
    [SerializeField, Tooltip("The transform for all of the room pieces.")] private Transform roomParentTransform;
    [SerializeField, Tooltip("The Spawn Point for all players in the build scene.")] private Transform spawnPoint;
    [SerializeField, Tooltip("The Ready Up Manager that lets players display that they are ready.")] private ReadyUpManager readyUpManager;

    public static BuildingManager Instance;

    private TankController defaultPlayerTank;
    private List<WorldRoom> worldRoomObjects;

    private void Awake()
    {
        Instance = this;
        worldRoomObjects = new List<WorldRoom>();
        defaultPlayerTank = FindObjectOfType<TankController>();
        defaultPlayerTank.TankName = "The Supreme Potato But In-Game";
    }

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance?.SetGamepadCursorsActive(true);
        gamePhaseUI?.ShowPhase(GAMESTATE.BUILDING);
        
        if(GameManager.Instance.tankDesign != null)
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
    public void SpawnRoom(int roomToSpawn, PlayerRoomSelection playerSelector)
    {
        GameObject roomObject = Instantiate(GameManager.Instance?.roomList[roomToSpawn], roomParentTransform);
        worldRoomObjects.Add(new WorldRoom(playerSelector, roomObject.GetComponent<Room>(), roomObject.transform));
    }

    private void Update()
    {
        foreach(WorldRoom room in worldRoomObjects)
        {
            if (!room.mountingComplete)
            {
                // Get the position of the target RectTransform in screen space
                Vector3 targetPosition = room.cursorTransform.position;

                // Convert the screen space position to world space
                targetPosition = Camera.main.ScreenToWorldPoint(targetPosition);
                targetPosition.z = 0f;

                room.roomObject.SnapMove(targetPosition);

                // Set the position of the game object to follow the RectTransform
                //room.roomTransform.position = targetPosition;
            }
        }
    }

    public void RotateRoom(PlayerInput playerInput)
    {
        WorldRoom playerRoom = GetPlayerRoom(playerInput);

        if (!playerRoom.mountingComplete)
        {
            GameManager.Instance.AudioManager.Play("RotateRoom");
            playerRoom.roomObject.debugRotate = true;
        }
    }

    public bool MountRoom(PlayerInput playerInput)
    {
        WorldRoom playerRoom = GetPlayerRoom(playerInput);

        playerRoom.Mount();

        if (playerRoom.mountingComplete)
        {
            SpawnPlayerInScene(playerRoom.playerSelector.GetCurrentPlayerInput());

            if (AllRoomsMounted())
            {
                foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
                    player.isReadyingUp = true;

                readyUpManager.Init();
            }

            return true;
        }
        else
        {
            playerRoom.SetRoomObject(Instantiate(GameManager.Instance?.roomList[playerRoom.playerSelector.GetRoomToPlace()], roomParentTransform).GetComponent<Room>());
        }

        return false;
    }

    private void SpawnPlayerInScene(PlayerInput playerInput)
    {
        PlayerMovement character = Instantiate(GameManager.Instance.MultiplayerManager.GetPlayerPrefab());
        character.LinkPlayerInput(playerInput);
        character.GetComponent<Rigidbody2D>().isKinematic = false;
        Vector3 playerPos = spawnPoint.position;
        playerPos.x += Random.Range(-0.25f, 0.25f);
        character.transform.position = playerPos;
        character.transform.GetComponentInChildren<Renderer>().material.SetColor("_Color", GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerInput.playerIndex]);
        GameManager.Instance.SetPlayerCursorActive(playerInput.GetComponent<GamepadCursor>(), false);
    }

    private void FinishTank()
    {
        foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
            player.isReadyingUp = false;

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
        foreach(WorldRoom room in worldRoomObjects)
        {
            if (!room.mountingComplete)
                return false;
        }

        return true;
    }

    public ReadyUpManager GetReadyUpManager() => readyUpManager;
}
