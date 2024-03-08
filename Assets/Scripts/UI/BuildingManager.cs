using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class BuildingManager : MonoBehaviour
{
    private class WorldRoom
    {
        public PlayerInput playerInput { get; private set; }
        public RectTransform cursorTransform { get; private set; }
        public Transform roomTransform { get; private set; }
        public bool isMounted { get; private set; }

        public WorldRoom(PlayerInput playerInput, Transform roomTransform)
        {
            this.playerInput = playerInput;
            this.roomTransform = roomTransform;
            cursorTransform = playerInput.GetComponent<GamepadCursor>().GetCursorTransform();
            isMounted = false;
        }

        public void Mount() => isMounted = true;
    }

    [SerializeField, Tooltip("Building canvas.")] private Canvas buildingCanvas;
    [SerializeField, Tooltip("The UI that shows the transition between game phases.")] private GamePhaseUI gamePhaseUI;
    [SerializeField, Tooltip("The transform for all of the room pieces.")] private Transform roomParentTransform;

    public static BuildingManager Instance;

    private List<WorldRoom> worldRoomObjects;

    private void Awake()
    {
        Instance = this;
        worldRoomObjects = new List<WorldRoom>();
    }

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance?.SetGamepadCursorsActive(true);
        gamePhaseUI?.ShowPhase(GAMESTATE.BUILDING);
    }

    /// <summary>
    /// Spawns a room in the world that the players can move around.
    /// </summary>
    /// <param name="roomToSpawn">The room to spawn into the world.</param>
    /// <param name="playerInput">The player to follow.</param>
    public void SpawnRoom(int roomToSpawn, PlayerInput playerInput)
    {
        Room currentRoom = GameManager.Instance?.roomList[roomToSpawn].GetComponent<Room>();
        GameObject roomObject = Instantiate(currentRoom.gameObject, roomParentTransform);
        worldRoomObjects.Add(new WorldRoom(playerInput, roomObject.transform));
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

                // Set the position of the game object to follow the RectTransform
                room.roomTransform.position = targetPosition;
            }
        }
    }

    public void MountRoom(PlayerInput playerInput)
    {
        for(int i = 0; i < worldRoomObjects.Count; i++)
        {
            if (!worldRoomObjects[i].isMounted)
            {
                if (worldRoomObjects[i].playerInput == playerInput)
                {
                    Debug.Log("Room Mounted!");
                    worldRoomObjects[i].Mount();

                    if (AllRoomsMounted())
                        gamePhaseUI?.ShowPhase(GAMESTATE.COMBAT);

                    break;
                }
            }
        }
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
