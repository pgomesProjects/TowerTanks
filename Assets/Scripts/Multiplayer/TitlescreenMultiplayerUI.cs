using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        foreach(var player in FindObjectOfType<MultiplayerManager>().GetPlayerInputs())
        {
            OnPlayerJoined(player.GetComponent<PlayerController>().GetPlayerIndex(), player.GetComponent<PlayerController>().GetPlayerColor(), player.currentControlScheme);
            counter++;
        }
    }

    public override void OnPlayerJoined(int playerIndex, Color playerColor, string controlScheme)
    {
        playerDisplayContainer.GetChild(playerIndex).GetComponent<PlayerDisplay>().ShowPlayerInfo(playerColor, controlScheme);
        UpdateJoinText(playerIndex);
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
