using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingHUD : GameHUD
{
    [SerializeField, Tooltip("The name UI system.")] private GameObject nameUI;
    [SerializeField, Tooltip("The text to show when all players are connected and ready to continue.")] private GameObject playersConnectedAndReady;
    [SerializeField, Tooltip("The tank name controller.")] private TankNameController tankNameController;
    [SerializeField, Tooltip("The room building menu.")] private RoomBuildingMenu roomBuildingMenu;
    [SerializeField, Tooltip("The container for the player namepads.")] private Transform playerNamepadContainer;

    public enum BuildingSubphase { Naming, PickRooms, BuildTank, ReadyUp }
    private BuildingSubphase currentSubphase;

    private bool allPlayersConnectedAndReady = false;
    private PlayerControlSystem playerControls;

    protected override void Awake()
    {
        base.Awake();
        playerControls = new PlayerControlSystem();
        playerControls.UI.Confirm.performed += _ => ConfirmNames();
    }

    protected override void Start()
    {
        base.Start();
        if(CampaignManager.Instance.HasCampaignStarted)
            UpdateBuildPhase(BuildingSubphase.PickRooms);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        GameManager.Instance.MultiplayerManager.OnPlayerConnected += AddPlayer;
        PlayerData.OnPlayerStateChanged += CheckForAllPlayersConnectedAndReady;
        CampaignManager.OnCampaignStarted += SwitchToNamingPhase;
        GamePhaseUI.OnCombatPhase += GoToCombatScene;

        playerControls?.Enable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        GameManager.Instance.MultiplayerManager.OnPlayerConnected -= AddPlayer;
        PlayerData.OnPlayerStateChanged -= CheckForAllPlayersConnectedAndReady;
        CampaignManager.OnCampaignStarted -= SwitchToNamingPhase;
        GamePhaseUI.OnCombatPhase -= GoToCombatScene;

        playerControls?.Disable();
    }

    private void SwitchToNamingPhase() => UpdateBuildPhase(BuildingSubphase.Naming);

    public void UpdateBuildPhase(BuildingSubphase newPhase)
    {
        currentSubphase = newPhase;
        switch (currentSubphase)
        {
            case BuildingSubphase.Naming:
                foreach (Transform namepad in playerNamepadContainer)
                    namepad.gameObject.SetActive(false);
                ShowNameUI();
                CheckForAllPlayersConnectedAndReady();
                break;

            case BuildingSubphase.PickRooms:
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
        if(currentSubphase == BuildingSubphase.Naming && allPlayersConnectedAndReady)
        {
            CampaignManager.Instance.SetPlayerTankName(tankNameController.GetCurrentName());
            BuildingManager.Instance.RefreshPlayerTankName();
            UpdateBuildPhase(BuildingSubphase.PickRooms);
        }
    }

    private void CheckForAllPlayersConnectedAndReady()
    {
        if(currentSubphase == BuildingSubphase.Naming)
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
            foreach(PlayerData player in allPlayers)
            {
                if (player.GetCurrentPlayerState() == PlayerData.PlayerState.SettingUp)
                    return false;
            }

            return true;
        }

        return false;
    }

    public void AddPlayer(PlayerInput playerInput)
    {
        NamepadController namepad = playerNamepadContainer.GetChild(playerInput.playerIndex).GetComponent<NamepadController>();
        namepad.gameObject.SetActive(true);
        namepad.AssignPlayerToGamepad(playerInput);

        CheckForAllPlayersConnectedAndReady();
    }

    public void GoToCombatScene() => GameManager.Instance.LoadScene("HotteScene", LevelTransition.LevelTransitionType.GATE, true, true, false);

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
