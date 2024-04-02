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
        public PlayerInput playerInput { get; private set; }
        public RectTransform cursorTransform { get; private set; }
        public Room roomObject { get; private set; }
        public Transform roomTransform { get; private set; }
        public bool isMounted { get; private set; }

        public WorldRoom(PlayerInput playerInput, Room roomObject, Transform roomTransform)
        {
            this.playerInput = playerInput;
            this.roomObject = roomObject;
            cursorTransform = playerInput.GetComponent<GamepadCursor>().GetCursorTransform();
            this.roomTransform = roomTransform;
            isMounted = false;
        }

        public void Mount()
        {
            isMounted = roomObject.Mount();

            if(isMounted)
            {
                Debug.Log("Room Mounted!");
                GameManager.Instance.AudioManager.Play("ConnectRoom");
            }
        }
    }

    [SerializeField, Tooltip("Building canvas.")] private Canvas buildingCanvas;
    [SerializeField, Tooltip("The UI that shows the transition between game phases.")] private GamePhaseUI gamePhaseUI;
    [SerializeField, Tooltip("The transform for all of the room pieces.")] private Transform roomParentTransform;

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

    /// <summary>
    /// Spawns a room in the world that the players can move around.
    /// </summary>
    /// <param name="roomToSpawn">The room to spawn into the world.</param>
    /// <param name="playerInput">The player to follow.</param>
    public void SpawnRoom(int roomToSpawn, PlayerInput playerInput)
    {
        GameObject roomObject = Instantiate(GameManager.Instance?.roomList[roomToSpawn], roomParentTransform);
        worldRoomObjects.Add(new WorldRoom(playerInput, roomObject.GetComponent<Room>(), roomObject.transform));
    }

    private void Update()
    {
        foreach(WorldRoom room in worldRoomObjects)
        {
            if (!room.isMounted)
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

        if (!playerRoom.isMounted)
        {
            GameManager.Instance.AudioManager.Play("RotateRoom");
            playerRoom.roomObject.debugRotate = true;
        }
    }

    public bool MountRoom(PlayerInput playerInput)
    {
        WorldRoom playerRoom = GetPlayerRoom(playerInput);

        if (!playerRoom.isMounted)
        {
            playerRoom.Mount();

            //If the mounting for the room failed, return false
            if (!playerRoom.isMounted)
                return false;

            if (AllRoomsMounted())
            {
                FinishTank();
            }
        }

        return true;
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
            if (worldRoomObjects[i].playerInput == playerInput)
                return worldRoomObjects[i];
        }

        return null;
    }

    public bool AllRoomsMounted()
    {
        foreach(WorldRoom room in worldRoomObjects)
        {
            if (!room.isMounted)
                return false;
        }

        return true;
    }
}
