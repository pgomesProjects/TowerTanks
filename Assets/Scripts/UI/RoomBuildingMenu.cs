using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class PlayerRoomSelection
{
    private PlayerData currentPlayer;
    private List<RoomInfo> currentRooms;
    private int roomsPlaced;
    private int maxRoomsToPlace;

    public PlayerRoomSelection(PlayerData currentPlayer)
    {
        this.currentPlayer = currentPlayer;
        this.currentRooms = new List<RoomInfo>();
        this.roomsPlaced = 0;
        this.maxRoomsToPlace = RoomBuildingMenu.MAX_ROOMS_TO_PLACE;
    }

    public PlayerRoomSelection(PlayerData currentPlayer, int maxRoomsToPlace)
    {
        this.currentPlayer = currentPlayer;
        this.currentRooms = new List<RoomInfo>(RoomBuildingMenu.MAX_ROOMS_TO_PLACE);
        this.roomsPlaced = 0;
        this.maxRoomsToPlace = maxRoomsToPlace;
    }

    public void AddRoomInfo(RoomInfo currentRoomInfo)
    {
        currentRooms.Add(currentRoomInfo);
    }

    public void MountRoom() => roomsPlaced++;

    public PlayerInput GetCurrentPlayerInput() => currentPlayer.playerInput;
    public RoomInfo GetRoomAt(int index) => currentRooms[index];
    public int GetMaxRoomsToPlace() => maxRoomsToPlace;
    public int GetNumberOfRoomsPlaced() => roomsPlaced;
    public Room GetRoomToPlace() => currentRooms[roomsPlaced].roomObject;
    public bool AllRoomsSelected() => currentRooms.Count >= maxRoomsToPlace;
    public bool AllRoomsMounted() => roomsPlaced >= maxRoomsToPlace;

    public void SetMaxRoomsToPlace(int maxRoomsToPlace)
    {
        this.maxRoomsToPlace = maxRoomsToPlace;
        ResetSelection();
    }

    public void ResetSelection()
    {
        this.roomsPlaced = 0;
        currentRooms.Clear();
    }

    public override string ToString()
    {
        return "Player " + (this.currentPlayer.playerInput.playerIndex + 1).ToString() + " | " + this.roomsPlaced + " Rooms Placed Out Of " + this.maxRoomsToPlace;
    }
}

public class RoomBuildingMenu : SerializedMonoBehaviour
{

    public const int MAX_ROOMS_TO_PLACE = 4;

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

    private List<SelectableRoomObject> roomButtons;
    private List<PlayerRoomSelection> roomSelections;

    private void Awake()
    {
        startingBackgroundPos = buildingBackgroundRectTransform.anchoredPosition;
        startingMenuPos = buildingMenuRectTransform.anchoredPosition;
        roomButtons = new List<SelectableRoomObject>();
        roomSelections = new List<PlayerRoomSelection>();
    }

    private void OnEnable()
    {
        GamePhaseUI.OnCombatPhase += GoToCombatScene;
        GameManager.Instance.MultiplayerManager.OnPlayerConnected += AddPlayerToSelection;
    }

    private void OnDisable()
    {
        GamePhaseUI.OnCombatPhase -= GoToCombatScene;
        GameManager.Instance.MultiplayerManager.OnPlayerConnected -= AddPlayerToSelection;
    }

    public void AddPlayerToSelection(PlayerInput playerInput)
    {
        StartCoroutine(WaitForAddPlayerToSelection(playerInput));
    }

    private IEnumerator WaitForAddPlayerToSelection(PlayerInput playerInput)
    {
        yield return null;

        roomSelections.Add(new PlayerRoomSelection(PlayerData.ToPlayerData(playerInput)));
        SetRoomSelections();
    }

    public void OpenMenu()
    {
        GenerateRooms();

        buildingMenuRectTransform.anchoredPosition = startingMenuPos;
        buildingBackgroundRectTransform.anchoredPosition = startingBackgroundPos;
        LeanTween.move(buildingMenuRectTransform, menuEndingPos, menuAniDuration).setEase(openMenuEaseType);
        LeanTween.move(buildingBackgroundRectTransform, backgroundEndingPos, backgroundAniDuration).setEase(showBackgroundEaseType);
    }

    public void CloseMenu()
    {
        buildingMenuRectTransform.anchoredPosition = menuEndingPos;
        buildingBackgroundRectTransform.anchoredPosition = backgroundEndingPos;
        LeanTween.move(buildingMenuRectTransform, startingMenuPos, menuAniDuration).setEase(closeMenuEaseType);
        LeanTween.move(buildingBackgroundRectTransform, startingBackgroundPos, backgroundAniDuration).setEase(hideBackgroundEaseType).setOnComplete(() => StartBuilding());
    }

    public void GoToCombatScene()
    {
        GameManager.Instance.LoadScene("HotteScene", LevelTransition.LevelTransitionType.GATE, true, true, false);
    }

    /// <summary>
    /// Generates a collection of rooms for the players to pick.
    /// </summary>
    public void GenerateRooms()
    {
        //Clear any existing rooms
        foreach(Transform rooms in roomListTransform)
            Destroy(rooms.gameObject);

        //Spawn in four random rooms and display their names
        for (int i = 0; i < MAX_ROOMS_TO_PLACE; i++)
        {
            int roomIndexRange = Random.Range(0, GameManager.Instance.roomList.Length);
            SelectableRoomObject newRoom = Instantiate(roomIconPrefab, roomListTransform);
            newRoom.DisplayRoomInfo(GameManager.Instance.roomList[roomIndexRange]);
            newRoom.SetRoomID(roomIndexRange);
            newRoom.OnSelected.AddListener(OnRoomSelected);
            roomButtons.Add(newRoom);
        }

        roomSelections.Clear();

        foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
        {
            roomSelections.Add(new PlayerRoomSelection(player));
            player.SetPlayerState(PlayerData.PlayerState.PickingRooms);
        }

        SetRoomSelections();
    }

    private void SetRoomSelections()
    {
        foreach (SelectableRoomObject roomButton in roomButtons)
            roomButton.DeselectRoom();

        roomsSelected = 0;

        int playerCount = roomSelections.Count;

        //Debug.Log("Player Count: " + playerCount);

        switch (playerCount)
        {
            //One player: 4 rooms per player
            case 1:
                for (int i = 0; i < playerCount; i++)
                    roomSelections[i].SetMaxRoomsToPlace(4);
                break;
            //Two players: 2 rooms per player
            case 2:
                for (int i = 0; i < playerCount; i++)
                    roomSelections[i].SetMaxRoomsToPlace(2);
                break;
            //Three players: 2 rooms for player one, 1 room for the rest
            case 3:
                for (int i = 0; i < playerCount; i++)
                {
                    if(i == 0)
                    {
                        roomSelections[i].SetMaxRoomsToPlace(2);
                        continue;
                    }

                    roomSelections[i].SetMaxRoomsToPlace(1);
                }
                break;
            //Four players: 1 room per player
            case 4:
                for (int i = 0; i < playerCount; i++)
                    roomSelections[i].SetMaxRoomsToPlace(1);
                break;
        }
    }

    private void OnRoomSelected(PlayerInput playerSelected, int currentRoomID)
    {
        roomsSelected++;
        PlayerRoomSelection currentSelector = GetPlayerSelectionData(playerSelected);
        currentSelector.AddRoomInfo(GameManager.Instance.roomList[currentRoomID]);

        if (currentSelector.AllRoomsSelected())
        {
            PlayerData currentPlayerData = PlayerData.ToPlayerData(playerSelected);
            currentPlayerData.SetPlayerState(PlayerData.PlayerState.PickedRooms);

            Debug.Log("Player " + (currentPlayerData.playerInput.playerIndex + 1).ToString() + " Has Stopped Selecting.");
            playerSelected.GetComponent<GamepadCursor>().SetCursorMove(false);
        }

        //If everyone has selected a room, move onto the next step
        if (roomsSelected >= MAX_ROOMS_TO_PLACE)
        {
            CloseMenu();
            GivePlayersRooms();
        }
    }

    private void StartBuilding()
    {
        foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
            player.SetPlayerState(PlayerData.PlayerState.IsBuilding);
    }

    /// <summary>
    /// Gives all of the players rooms that they can move around.
    /// </summary>
    private void GivePlayersRooms()
    {
        foreach (var room in roomSelections)
            BuildingManager.Instance.SpawnRoom(room.GetRoomAt(0), room);

        //GameManager.Instance.SetGamepadCursorsActive(true);
    }

    private PlayerRoomSelection GetPlayerSelectionData(PlayerInput currentPlayer)
    {
        foreach (PlayerRoomSelection selection in roomSelections)
        {
            if (selection.GetCurrentPlayerInput() == currentPlayer)
                return selection;
        }

        return null;
    }
}
