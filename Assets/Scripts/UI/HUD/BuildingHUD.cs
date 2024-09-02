using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingHUD : GameHUD
{
    [SerializeField, Tooltip("The container for the player namepads.")] private Transform playerNamepadContainer;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        foreach (Transform namepad in playerNamepadContainer)
            namepad.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.Instance.MultiplayerManager.OnPlayerConnected += AddNamepad;
    }

    private void OnDisable()
    {
        GameManager.Instance.MultiplayerManager.OnPlayerConnected -= AddNamepad;
    }

    public void AddNamepad(PlayerInput playerInput)
    {
        NamepadController namepad = playerNamepadContainer.GetChild(playerInput.playerIndex).GetComponent<NamepadController>();
        namepad.gameObject.SetActive(true);
        namepad.AssignPlayerToGamepad(playerInput);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
