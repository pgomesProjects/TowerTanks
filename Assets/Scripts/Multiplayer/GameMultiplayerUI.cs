using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameMultiplayerUI : MultiplayerUI
{
    [SerializeField] private GameObject onJoinObject;

    public override void OnPlayerJoined(int playerIndex, Color playerColor, string controlScheme)
    {
        if (onJoinObject.activeInHierarchy)
            onJoinObject.SetActive(false);

        onJoinObject.SetActive(true);
        onJoinObject.GetComponentInChildren<TextMeshProUGUI>().text = "Player " + (playerIndex + 1) + " Joined";

        if (PlayerInput.all.Count <= ConnectionController.NumberOfActivePlayers())
        {
            joinPrompt.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (LevelManager.instance != null)
        {
            if (LevelManager.instance.levelPhase != GAMESTATE.GAMEOVER)
            {
                int playerCount = PlayerInput.all.Count;

                //If the number of gamepads is less than the number of active controllers
                if (playerCount > ConnectionController.NumberOfActivePlayers())
                {
                    //If the join prompt is not already active, make it active
                    if (!onJoinObject.activeInHierarchy)
                        onJoinObject.SetActive(true);
                }
            }
            //If the game is over, disable joining
            else
            {
                FindObjectOfType<PlayerInputManager>().DisableJoining();
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
