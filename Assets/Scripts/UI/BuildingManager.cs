using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingManager : MonoBehaviour
{
    private struct WorldRoom
    {
        public RectTransform cursorTransform;
        public Transform roomTransform;

        public WorldRoom(RectTransform cursorTransform, Transform roomTransform)
        {
            this.cursorTransform = cursorTransform;
            this.roomTransform = roomTransform;
        }
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
    /// <param name="playerCursorTransform">The player cursor to follow.</param>
    public void SpawnRoom(int roomToSpawn, RectTransform playerCursorTransform)
    {
        Room currentRoom = GameManager.Instance?.roomList[roomToSpawn].GetComponent<Room>();
        GameObject roomObject = Instantiate(currentRoom.gameObject, roomParentTransform);
        worldRoomObjects.Add(new WorldRoom(playerCursorTransform, roomObject.transform));
    }

    private void Update()
    {
        foreach(WorldRoom room in worldRoomObjects)
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
