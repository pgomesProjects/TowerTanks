using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;
using TMPro;
using TowerTanks.Scripts.OdinTools;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
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

        public PlayerData GetCurrentPlayerData() => currentPlayer;
        public PlayerInput GetCurrentPlayerInput() => currentPlayer.playerInput;
        public int GetNumberOfRoomsGiven() => currentRooms.Count;
        public RoomInfo GetRoomAt(int index) => currentRooms[index];
        public int GetMaxRoomsToPlace() => maxRoomsToPlace;
        public int GetNumberOfRoomsPlaced() => roomsPlaced;
        public Room GetRoomToPlace() => currentRooms[roomsPlaced].roomObject;
        public bool AllRoomsSelected() => currentRooms.Count >= maxRoomsToPlace;
        public bool AllRoomsMounted() => roomsPlaced >= maxRoomsToPlace || currentRooms.Count == 0;

        public void SetMaxRoomsToPlace(int maxRoomsToPlace)
        {
            this.maxRoomsToPlace = maxRoomsToPlace;
            ResetSelection();
        }

        public void UndoRoomPlaced() => roomsPlaced -= 1;
        public void IncrementMaxRoomsToPlace() => this.maxRoomsToPlace += 1;

        public void ResetSelection()
        {
            this.roomsPlaced = 0;
            currentRooms.Clear();
        }

        public override string ToString()
        {
            return "Player " + (this.currentPlayer.playerInput.playerIndex + 1).ToString() + " | " + this.roomsPlaced + " Rooms Placed Out Of " + this.maxRoomsToPlace;
        }

        public void DebugPrintRooms()
        {
            string message = "Rooms: ";

            for(int i = 0; i < currentRooms.Count; i++)
            {
                message += currentRooms[i].roomObject.name;

                if (i < currentRooms.Count - 1)
                    message += ", ";
            }

            Debug.Log(message);
        }
    }

    public class RoomBuildingMenu : SerializedMonoBehaviour
    {
        public static int MAX_ROOMS_TO_PLACE;

        [SerializeField, Tooltip("The amount of rooms for the players to pick from.")] private int numberOfRoomsToGenerate;
        [SerializeField, Tooltip("The max amount of rooms that all players can pick in a round.")] private int maxRoomsPerRound;
        [SerializeField, Tooltip("The prefab that generates data for a room that the players can pick.")] private SelectableRoomObject roomIconPrefab;
        [SerializeField, Tooltip("The transform to store the selectable rooms.")] private Transform roomListTransform;
        [SerializeField, Tooltip("The RectTransform for the building menu.")] private RectTransform buildingMenuRectTransform;
        [SerializeField, Tooltip("The RectTransform for the building background.")] private RectTransform buildingBackgroundRectTransform;
        [SerializeField, Tooltip("The text showing the rooms remaining.")] private TextMeshProUGUI roomsRemainingText;
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
        Dictionary<int, List<RoomInfo>> hatchRoomIDToInfo = new Dictionary<int, List<RoomInfo>>();

        private void Awake()
        {
            MAX_ROOMS_TO_PLACE = maxRoomsPerRound;
            startingBackgroundPos = buildingBackgroundRectTransform.anchoredPosition;
            startingMenuPos = buildingMenuRectTransform.anchoredPosition;
            roomButtons = new List<SelectableRoomObject>();
            roomSelections = new List<PlayerRoomSelection>();
        }

        private void OnEnable()
        {
            GameManager.Instance.MultiplayerManager.OnPlayerConnected += AddPlayerToSelection;
        }

        private void OnDisable()
        {
            GameManager.Instance.MultiplayerManager.OnPlayerConnected -= AddPlayerToSelection;
        }

        public void AddPlayerToSelection(PlayerInput playerInput)
        {
            if(BuildSystemManager.Instance.CurrentSubPhase == BuildSystemManager.BuildingSubphase.PickRooms)
                StartCoroutine(WaitForAddPlayerToSelection(playerInput));
        }

        private IEnumerator WaitForAddPlayerToSelection(PlayerInput playerInput)
        {
            yield return null;

            roomSelections.Add(new PlayerRoomSelection(PlayerData.ToPlayerData(playerInput)));
            SetRoomSelections();
        }

        /// <summary>
        /// Opens the room selection menu.
        /// </summary>
        public void OpenMenu()
        {
            //Generate the random rooms before the menu displays
            GenerateRooms();

            buildingMenuRectTransform.anchoredPosition = startingMenuPos;
            buildingBackgroundRectTransform.anchoredPosition = startingBackgroundPos;
            LeanTween.move(buildingMenuRectTransform, menuEndingPos, menuAniDuration).setEase(openMenuEaseType);
            LeanTween.move(buildingBackgroundRectTransform, backgroundEndingPos, backgroundAniDuration).setEase(showBackgroundEaseType);
        }

        /// <summary>
        /// Closes the room selection menu.
        /// </summary>
        public void CloseMenu()
        {
            buildingMenuRectTransform.anchoredPosition = menuEndingPos;
            buildingBackgroundRectTransform.anchoredPosition = backgroundEndingPos;
            LeanTween.move(buildingMenuRectTransform, startingMenuPos, menuAniDuration).setEase(closeMenuEaseType);
            LeanTween.move(buildingBackgroundRectTransform, startingBackgroundPos, backgroundAniDuration).setEase(hideBackgroundEaseType).setOnComplete(() => StartBuilding());
        }

        /// <summary>
        /// Generates a collection of rooms for the players to pick.
        /// </summary>
        public void GenerateRooms()
        {
            //Clear any existing rooms
            foreach (Transform rooms in roomListTransform)
                Destroy(rooms.gameObject);
            
            //choose random rooms
            int[] randomRooms = new int[numberOfRoomsToGenerate];
            System.Random random = new System.Random();
            for (int i = 0; i < numberOfRoomsToGenerate; i++)
            {
                var randIndex = Random.Range(0, GameManager.Instance.roomList.Length);
                randomRooms[i] = randIndex;
            }
            List<int> hatchRooms = randomRooms.OrderBy(x => random.Next()).Take(2).ToList(); // chooses 2 random rooms to have hatches
            //Spawn in random rooms and display their names
            var offset = 0;
            for (int i = 0; i < numberOfRoomsToGenerate; i++)
            {
                int roomIndex = randomRooms[i];
                SelectableRoomObject newRoom = Instantiate(roomIconPrefab, roomListTransform); 
                if (hatchRooms.Contains(roomIndex))
                {
                    Debug.Log("Hatch Room!");
                    //spawn room at 0,0
                    newRoom.roomHasHatch = true;
                    var littleguy = Instantiate(GameManager.Instance.roomList[roomIndex].roomObject, new Vector3(0 + offset, -25, 0), Quaternion.identity, GameObject.Find("Rooms").transform);
                    littleguy.isAHatchRoom = true;
                    offset += 10;
                    littleguy.GenerateRandomHatch();
                    
                    RoomInfo newlyHatchedRoom = Instantiate(GameManager.Instance.roomList[roomIndex]);
                    newlyHatchedRoom.roomObject = littleguy;
                    StartCoroutine(WaitForRoomInit());
                    IEnumerator WaitForRoomInit()
                    {
                        yield return new WaitForSeconds(.05f);
                        newlyHatchedRoom.sprite = RuntimePolaroid.CaptureSpriteFromObject(littleguy.gameObject);
                        newRoom.DisplayRoomInfo(newlyHatchedRoom);
                    }

                    if (!hatchRoomIDToInfo.Keys.Contains(roomIndex))
                    {
                        List<RoomInfo> n = new List<RoomInfo>() { newlyHatchedRoom };
                        hatchRoomIDToInfo.Add(roomIndex, n);
                    }
                    else
                    {
                        hatchRoomIDToInfo[roomIndex].Add(newlyHatchedRoom);
                    }
                    
                    hatchRooms.Remove(roomIndex);
                }
                else
                {
                    newRoom.DisplayRoomInfo(GameManager.Instance.roomList[roomIndex]);
                }
                newRoom.SetRoomID(roomIndex);
                newRoom.OnSelected.AddListener(OnRoomSelected);
                newRoom.gameObject.name = GameManager.Instance.roomList[roomIndex].name;
                roomButtons.Add(newRoom);
            }

            //Give each player an empty room selection object to use to pick rooms
            roomSelections.Clear();
            foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
            {
                roomSelections.Add(new PlayerRoomSelection(player));
                
                player.SetPlayerState(PlayerData.PlayerState.PickingRooms);
            }

            SetRoomSelections();
            UpdateRoomsRemainingDisplay();
        }

        /// <summary>
        /// Updates the display for the amount of rooms the players can pick.
        /// </summary>
        private void UpdateRoomsRemainingDisplay()
        {
            roomsRemainingText.text = "Rooms Remaining: " + (MAX_ROOMS_TO_PLACE - roomsSelected).ToString();
        }

        /// <summary>
        /// Sets the amount of rooms that each player can pick.
        /// </summary>
        private void SetRoomSelections()
        {
            foreach (SelectableRoomObject roomButton in roomButtons)
                roomButton.DeselectRoom();

            roomsSelected = 0;

            int playerCount = roomSelections.Count;

            //Reset all room maxes
            foreach (PlayerRoomSelection player in roomSelections)
                player.SetMaxRoomsToPlace(0);

            for (int i = 0; i < MAX_ROOMS_TO_PLACE; i++)
                roomSelections[i % playerCount].IncrementMaxRoomsToPlace();
        }

        /// <summary>
        /// Lets a player select a room to add to their list of rooms to build.
        /// </summary>
        /// <param name="playerSelected">The player selecting the room.</param>
        /// <param name="currentRoomID">The ID of the room selected.</param>
        private List<RoomInfo> hatchRoomsSelected;
        private void OnRoomSelected(PlayerInput playerSelected, int currentRoomID, bool hasHatch)
        {
            roomsSelected++;
            UpdateRoomsRemainingDisplay();
            PlayerRoomSelection currentSelector = GetPlayerSelectionData(playerSelected);
            if (hatchRoomIDToInfo.Keys.Contains(currentRoomID) && hatchRoomIDToInfo[currentRoomID].Count > 0 && hasHatch)
            {
                currentSelector.AddRoomInfo(hatchRoomIDToInfo[currentRoomID][0]);
                hatchRoomIDToInfo[currentRoomID].RemoveAt(0);
            }
            else
            {
                currentSelector.AddRoomInfo(GameManager.Instance.roomList[currentRoomID]);
            }
            

            //If the player has selected the maximum amount of rooms they can, don't let them pick any more
            if (currentSelector.AllRoomsSelected())
            {
                PlayerData currentPlayerData = PlayerData.ToPlayerData(playerSelected);
                currentPlayerData.SetPlayerState(PlayerData.PlayerState.PickedRooms);

                Debug.Log("Player " + (currentPlayerData.playerInput.playerIndex + 1).ToString() + " Has Stopped Selecting.");
                playerSelected.GetComponent<GamepadCursor>().SetCursorMove(false);
            }

            //If everyone has selected a room, move onto the next step
            if (roomsSelected >= MAX_ROOMS_TO_PLACE || roomsSelected >= numberOfRoomsToGenerate)
            {
                CloseMenu();
                GivePlayersRooms();
                BuildSystemManager.Instance.UpdateBuildPhase(BuildSystemManager.BuildingSubphase.BuildTank);
            }
        }

        /// <summary>
        /// Let all players begin the process of building the tank.
        /// </summary>
        private void StartBuilding()
        {
            //Let the players build
            foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
                player.SetPlayerState(PlayerData.PlayerState.IsBuilding);

            //Check to see if any players need to be moved to the tank
            BuildSystemManager.Instance.ValidatePlayers();
        }

        /// <summary>
        /// Gives all of the players rooms that they can move around.
        /// </summary>
        private void GivePlayersRooms()
        {
            foreach (var room in roomSelections)
            {
                //If the player has a room, spawn it for them
                if (room.GetNumberOfRoomsGiven() > 0)
                    BuildSystemManager.Instance.SpawnRoom(room.GetRoomAt(0), room);
                //If not, add them to the list without any rooms
                else
                {
                    BuildSystemManager.Instance.AddEmptyPlayerRoom(room);
                }
            }
        }

        /// <summary>
        /// Gets the player selection data based on the player input component given.
        /// </summary>
        /// <param name="currentPlayer">The current player input component.</param>
        /// <returns>Returns a PlayerRoomSelection object if found in the list. Returns null if nothing is found.</returns>
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
}
