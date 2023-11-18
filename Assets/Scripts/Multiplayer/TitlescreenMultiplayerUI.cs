using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TitlescreenMultiplayerUI : MultiplayerUI
{
    [SerializeField] private string joinText;
    [SerializeField] private Transform playerDisplayContainer;

    private void Start()
    {
        CheckForExistingPlayers();   
    }

    public void CheckForExistingPlayers()
    {
        int counter = 0;
        foreach(var player in GameManager.Instance?.MultiplayerManager.GetPlayerInputs())
        {
            OnPlayerJoined(player);
            counter++;
        }
    }

    public override void OnPlayerJoined(PlayerInput playerInput)
    {
        playerDisplayContainer.GetChild(playerInput.playerIndex).GetComponent<PlayerDisplay>().ShowPlayerInfo(GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerInput.playerIndex], playerInput.currentControlScheme);
        UpdateJoinText(playerInput.playerIndex);
    }

    public override void OnPlayerLost(int playerIndex)
    {
        playerDisplayContainer.GetChild(playerIndex).GetComponent<PlayerDisplay>().HidePlayerInfo();
    }

    public override void OnPlayerRejoined(int playerIndex)
    {
        playerDisplayContainer.GetChild(playerIndex).GetComponent<PlayerDisplay>().ShowPlayerInfo();
    }

    private void UpdateJoinText(int playerIndex)
    {
        joinPrompt.text = "Player " + (playerIndex + 2).ToString() + ": " + joinText;
    }
}
