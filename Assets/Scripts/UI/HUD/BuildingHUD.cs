using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace TowerTanks.Scripts
{
    public class BuildingHUD : GameHUD
    {
        [SerializeField, Tooltip("The name UI system.")] private GameObject nameUI;
        [SerializeField, Tooltip("The text to show when all players are connected and ready to continue.")] private GameObject playersConnectedAndReady;
        [SerializeField, Tooltip("The tank name controller.")] private TankNameController tankNameController;
        [SerializeField, Tooltip("The room building menu.")] private RoomBuildingMenu roomBuildingMenu;
        [SerializeField, Tooltip("The container for the player namepads.")] private Transform playerNamepadContainer;

        [SerializeField, Tooltip("The player action container.")] private RectTransform playerActionContainer;
        [SerializeField, Tooltip("The player action prefab.")] private GameObject playerActionPrefab;
        [SerializeField, Tooltip("The color for the most recent action.")] private Color mostRecentActionColor;

        private bool allPlayersConnectedAndReady = false;
        private PlayerControlSystem playerControls;

        private RectTransform historyParentTransform;
        private Color defaultPlayerActionColor;

        protected override void Awake()
        {
            base.Awake();
            playerControls = new PlayerControlSystem();
            playerControls.UI.Confirm.performed += _ => ConfirmNames();
            defaultPlayerActionColor = playerActionPrefab.GetComponentInChildren<Image>().color;
            historyParentTransform = playerActionContainer.parent.GetComponent<RectTransform>();
        }

        protected override void Start()
        {
            base.Start();
            InitializePlayerNamepads();
            CheckForAllPlayersConnectedAndReady();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            GameManager.Instance.MultiplayerManager.OnPlayerConnected += AddPlayer;
            PlayerData.OnPlayerStateChanged += CheckForAllPlayersConnectedAndReady;
            GamePhaseUI.OnCombatPhase += GoToCombatScene;
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildSystemManager.OnPlayerAction += AddToPlayerHistoryUI;
            BuildSystemManager.OnPlayerUndo += RemoveMostRecentPlayerAction;

            playerControls?.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            GameManager.Instance.MultiplayerManager.OnPlayerConnected -= AddPlayer;
            PlayerData.OnPlayerStateChanged -= CheckForAllPlayersConnectedAndReady;
            GamePhaseUI.OnCombatPhase -= GoToCombatScene;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BuildSystemManager.OnPlayerAction -= AddToPlayerHistoryUI;
            BuildSystemManager.OnPlayerUndo -= RemoveMostRecentPlayerAction;

            playerControls?.Disable();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            //Start the campaign if it has not started already
            if (!CampaignManager.Instance.HasCampaignStarted)
            {
                CampaignManager.Instance.SetupCampaign();
                BuildSystemManager.Instance.UpdateBuildPhase(BuildSystemManager.BuildingSubphase.Naming);
                RefreshBuildPhaseUI();
            }
            else
            {
                BuildSystemManager.Instance.UpdateBuildPhase(BuildSystemManager.BuildingSubphase.PickRooms);
                RefreshBuildPhaseUI();
            }
        }

        private void AddToPlayerHistoryUI(string playerName, string roomName)
        {
            if (playerActionContainer.childCount != 0)
                playerActionContainer.GetChild(playerActionContainer.childCount - 1).GetComponentInChildren<Image>().color = defaultPlayerActionColor;

            GameObject newAction = Instantiate(playerActionPrefab, playerActionContainer);
            newAction.GetComponentInChildren<TextMeshProUGUI>().text = playerName + " Placed " + roomName;
            newAction.GetComponentInChildren<Image>().color = mostRecentActionColor;
            LayoutRebuilder.ForceRebuildLayoutImmediate(historyParentTransform);
        }

        private void RemoveMostRecentPlayerAction()
        {
            if (playerActionContainer.childCount - 2 >= 0)
                playerActionContainer.GetChild(playerActionContainer.childCount - 2).GetComponentInChildren<Image>().color = mostRecentActionColor;

            if (playerActionContainer.childCount > 0)
                Destroy(playerActionContainer.GetChild(playerActionContainer.childCount - 1).gameObject);
        }

        public void RefreshBuildPhaseUI()
        {
            switch (BuildSystemManager.Instance.CurrentSubPhase)
            {
                case BuildSystemManager.BuildingSubphase.Naming:
                    foreach (Transform namepad in playerNamepadContainer)
                        namepad.gameObject.SetActive(false);
                    ShowNameUI();
                    CheckForAllPlayersConnectedAndReady();
                    break;

                case BuildSystemManager.BuildingSubphase.PickRooms:
                    HideNameUI();
                    roomBuildingMenu.OpenMenu();
                    break;
            }
        }

        private void ShowNameUI()
        {
            nameUI?.SetActive(true);
        }

        private void HideNameUI()
        {
            nameUI?.SetActive(false);
        }

        private void ConfirmNames()
        {
            if (GameManager.Instance.InGameMenu)
                return;

            if (BuildSystemManager.Instance.CurrentSubPhase == BuildSystemManager.BuildingSubphase.Naming && allPlayersConnectedAndReady)
            {
                CampaignManager.Instance.SetPlayerTankName(tankNameController.GetCurrentName());
                BuildSystemManager.Instance.RefreshPlayerTankName();
                BuildSystemManager.Instance.UpdateBuildPhase(BuildSystemManager.BuildingSubphase.PickRooms);
                RefreshBuildPhaseUI();
            }
        }

        private void CheckForAllPlayersConnectedAndReady()
        {
            if (BuildSystemManager.Instance.CurrentSubPhase == BuildSystemManager.BuildingSubphase.Naming)
            {
                allPlayersConnectedAndReady = AreAllPlayersConnectedAndReady();
                playersConnectedAndReady.SetActive(allPlayersConnectedAndReady);
            }
        }

        private bool AreAllPlayersConnectedAndReady()
        {
            PlayerData[] allPlayers = GameManager.Instance.MultiplayerManager.GetAllPlayers();

            if (allPlayers.Length > 0)
            {
                foreach (PlayerData player in allPlayers)
                {
                    if (player.GetCurrentPlayerState() == PlayerData.PlayerState.SettingUp)
                        return false;
                }

                return true;
            }

            return false;
        }

        private void InitializePlayerNamepads()
        {
            foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
                AddPlayer(player.playerInput);
        }

        public void AddPlayer(PlayerInput playerInput)
        {
            if (GameSettings.customPlayerNames)
            {
                //Give the player a namepad to name themselves
                NamepadController namepad = playerNamepadContainer.GetChild(playerInput.playerIndex).GetComponent<NamepadController>();
                namepad.gameObject.SetActive(true);
                namepad.AssignPlayerToGamepad(playerInput);
            }

            CheckForAllPlayersConnectedAndReady();
        }

        public void GoToCombatScene() => GameManager.Instance.LoadScene("HotteScene", LevelTransition.LevelTransitionType.GATE, true, true, false);

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
