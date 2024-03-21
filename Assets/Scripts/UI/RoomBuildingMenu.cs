using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class RoomBuildingMenu : SerializedMonoBehaviour
{
    private struct PlayerRoomSelection
    {
        public PlayerInput currentPlayer;
        public int currentRoomID;

        public PlayerRoomSelection(PlayerInput currentPlayer, int currentRoomID)
        {
            this.currentPlayer = currentPlayer;
            this.currentRoomID = currentRoomID;
        }
    }

    [SerializeField, Tooltip("The prefab that generates data for a room that the players can pick.")] private SelectableRoomObject roomIconPrefab;
    [SerializeField, Tooltip("The transform to store the selectable rooms.")] private Transform roomListTransform;
    [SerializeField, Tooltip("The RectTransform for the building menu.")] private RectTransform buildingMenuRectTransform;
    [SerializeField, Tooltip("The RectTransform for the building background.")] private RectTransform buildingBackgroundRectTransform;
    [SerializeField, Tooltip("The ending position for the background.")] private Vector3 backgroundEndingPos;
    [SerializeField, Tooltip("The ending position for the menu.")] private Vector3 menuEndingPos;
    [SerializeField, Tooltip("The duration for the background animation.")] private float backgroundAniDuration = 0.5f;
    [SerializeField, Tooltip("The duration for the menu animation.")] private float menuAniDuration = 0.5f;
    [SerializeField, Tooltip("The ease type for the show background animation.")] private LeanTweenType showBackgroundEaseType;
    [SerializeField, Tooltip("The ease type for the hide background animation.")] private LeanTweenType hideBackgroundEaseType;
    [SerializeField, Tooltip("The ease type for the open menu animation.")] private LeanTweenType openMenuEaseType;
    [SerializeField, Tooltip("The ease type for the close menu animation.")] private LeanTweenType closeMenuEaseType;

    private Vector3 startingBackgroundPos;
    private Vector3 startingMenuPos;
    private int roomsSelected;

    private List<PlayerRoomSelection> roomSelections;

    private void Awake()
    {
        startingBackgroundPos = buildingBackgroundRectTransform.anchoredPosition;
        startingMenuPos = buildingMenuRectTransform.anchoredPosition;
        roomSelections = new List<PlayerRoomSelection>();
    }

    private void OnEnable()
    {
        GamePhaseUI.OnBuildingPhase += OpenMenu;
        GamePhaseUI.OnCombatPhase += GoToCombatScene;
    }

    private void OnDisable()
    {
        GamePhaseUI.OnBuildingPhase -= OpenMenu;
        GamePhaseUI.OnCombatPhase -= GoToCombatScene;
    }

    public void OpenMenu()
    {
        GenerateRooms();
        LeanTween.move(buildingMenuRectTransform, menuEndingPos, menuAniDuration).setEase(openMenuEaseType);
        LeanTween.move(buildingBackgroundRectTransform, backgroundEndingPos, backgroundAniDuration).setEase(showBackgroundEaseType);
    }

    public void CloseMenu()
    {
        LeanTween.move(buildingMenuRectTransform, startingMenuPos, menuAniDuration).setEase(closeMenuEaseType);
        LeanTween.move(buildingBackgroundRectTransform, startingBackgroundPos, backgroundAniDuration).setEase(hideBackgroundEaseType).setOnComplete(() => StartBuilding());
    }

    public void GoToCombatScene()
    {
        GameManager.Instance.LoadScene("GameScene", LevelTransition.LevelTransitionType.GATE, true, true, false);
    }

    /// <summary>
    /// Generates a collection of rooms for the players to pick.
    /// </summary>
    public void GenerateRooms()
    {
        //Clear any existing rooms
        foreach(Transform rooms in roomListTransform)
            Destroy(rooms.gameObject);

        roomSelections.Clear();

        roomsSelected = 0;

        //Spawn in four random rooms and display their names
        for (int i = 0; i < 4; i++)
        {
            int roomIndexRange = Random.Range(0, GameManager.Instance.roomList.Length);
            SelectableRoomObject newRoom = Instantiate(roomIconPrefab, roomListTransform);
            newRoom.GetComponentInChildren<TextMeshProUGUI>().text = GameManager.Instance.roomList[roomIndexRange].name.ToString();
            newRoom.SetRoomID(roomIndexRange);
            newRoom.OnSelected.AddListener(OnRoomSelected);
        }
    }

    private void OnRoomSelected(PlayerInput playerSelected, int currentRoomID)
    {
        roomsSelected++;
        roomSelections.Add(new PlayerRoomSelection(playerSelected, currentRoomID));

        //If everyone has selected a room, move onto the next step
        if(roomsSelected >= GameManager.Instance.MultiplayerManager.playerInputManager.playerCount)
        {
            CloseMenu();
            GivePlayersRooms();
        }
    }

    private void StartBuilding()
    {
        foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
            player.isBuilding = true;
    }

    /// <summary>
    /// Gives all of the players rooms that they can move around.
    /// </summary>
    private void GivePlayersRooms()
    {
        foreach (var room in roomSelections)
        {
            BuildingManager.Instance.SpawnRoom(room.currentRoomID, room.currentPlayer);
        }
    }
}
