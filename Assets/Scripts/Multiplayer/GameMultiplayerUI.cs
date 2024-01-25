using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameMultiplayerUI : MultiplayerUI
{
    [SerializeField] private bool showJoinPrompt;
    [SerializeField] private GameObject onJoinObject;

    public override void OnPlayerJoined(PlayerInput playerInput)
    {
        if (onJoinObject.activeInHierarchy)
            onJoinObject.SetActive(false);

        onJoinObject.SetActive(true);
        onJoinObject.GetComponentInChildren<TextMeshProUGUI>().text = "Player " + (playerInput.playerIndex + 1) + " Joined";

        if (PlayerInput.all.Count <= ConnectionController.NumberOfActivePlayers())
        {
            joinPrompt.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (LevelManager.Instance != null)
        {
            if (LevelManager.Instance.levelPhase != GAMESTATE.GAMEOVER)
            {
                if (showJoinPrompt)
                {
                    int playerCount = Gamepad.all.Count + 1;

                    //If the number of gamepads is less than the number of active controllers
                    if (ConnectionController.NumberOfActivePlayers() == 0 || playerCount > ConnectionController.NumberOfActivePlayers())
                    {
                        //If the join prompt is not already active, make it active
                        if (!joinPrompt.gameObject.activeInHierarchy)
                            joinPrompt.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (joinPrompt.gameObject.activeInHierarchy)
                        joinPrompt.gameObject.SetActive(false);
                }
            }
            //If the game is over, disable joining
            else
            {
                GameManager.Instance.MultiplayerManager.playerInputManager.DisableJoining();
            }
        }
    }

    public override void OnPlayerLost(int playerIndex)
    {
        //throw new System.NotImplementedException();
    }

    public override void OnPlayerRejoined(int playerIndex)
    {
        //throw new System.NotImplementedException();
    }
}
