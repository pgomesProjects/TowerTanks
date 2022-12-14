using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Cinemachine;
using TMPro;
public class MultiplayerManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerParent;
    [SerializeField] private GameObject playerJoinObject;
    [SerializeField] private GameObject joinPrompt;

    private PlayerInputManager playerInputManager;

    private Color[] playerColors = { Color.red, Color.blue, Color.yellow, Color.green };
    private bool onStartJoin;

    private void Awake()
    {
        playerInputManager = FindObjectOfType<PlayerInputManager>();
        playerInputManager.EnableJoining();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Delegate function to when the scene is unloaded
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        //Disconnect lost controllers

        //Add players on start
        AddPlayersOnStart();

        //Delegate join function
        playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    private void Update()
    {
        if(LevelManager.instance.levelPhase != GAMESTATE.GAMEOVER)
        {
            int gamepadCount = Gamepad.all.Count;

            //If the number of gamepads is less than the number of active controllers
            if (gamepadCount > ConnectionController.NumberOfActivePlayers())
            {
                //If the join prompt is not already active, make it active
                if (!joinPrompt.activeInHierarchy)
                    joinPrompt.SetActive(true);
            }
        }
        //If the game is over, disable joining
        else
        {
            playerInputManager.DisableJoining();
        }
    }

    private void AddPlayersOnStart()
    {
        onStartJoin = true;
        
        foreach(var i in Gamepad.all)
        {
            if (i.enabled)
            {
                PlayerInput currentPlayer = PlayerInput.Instantiate(playerInputManager.playerPrefab, controlScheme: "Gamepad", pairWithDevice: i);
                OnPlayerJoined(currentPlayer);
            }
        }

        onStartJoin = false;
    }

    /// <summary>
    /// Behavior for when a player joins the game
    /// </summary>
    /// <param name="playerInput"></param>
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        //Disable the current join animation if active
        if (playerJoinObject.activeInHierarchy)
            playerJoinObject.SetActive(false);

        //Generate the new player's index
        int playerIndex = ConnectionController.CheckForIndex();
        ConnectionController.connectedControllers[playerIndex] = true;

        //Move the player to the spawn point
        playerInput.transform.position = spawnPoints[playerIndex].position;

        playerInput.GetComponent<PlayerController>().SetPlayerIndex(playerIndex);
        SetColorOfPlayer(playerInput.transform, playerIndex);
        playerInput.transform.parent = playerParent;
        playerInput.onDeviceLost += OnDeviceLost;
        playerInput.onDeviceRegained += OnDeviceRegained;

        if(LevelManager.instance.levelPhase == GAMESTATE.GAMEACTIVE || GameSettings.skipTutorial)
        playerInput.GetComponent<PlayerController>().SetPlayerMove(true);

        //Create join animation
        if (!onStartJoin)
        {
            playerJoinObject.SetActive(true);
            playerJoinObject.GetComponentInChildren<TextMeshProUGUI>().text = "Player " + (playerIndex + 1) + " Joined";
        }

        //Check to see if the join prompt needs to be disabled
        if(Gamepad.all.Count <= ConnectionController.NumberOfActivePlayers())
        {
            joinPrompt.SetActive(false);
        }
    }

    private void SetColorOfPlayer(Transform player, int playerIndex)
    {
        //Change color of player depending on which player number they are
        player.transform.GetComponent<Renderer>().material.SetColor("_Color", playerColors[playerIndex]);
    }

    private void OnSceneUnloaded(Scene current)
    {
        //Get rid of the controllers when the game is unloaded
        for(int i = 0; i < ConnectionController.connectedControllers.Length; i++)
        {
            ConnectionController.connectedControllers[i] = false;
        }
    }

    private void OnDeviceLost(PlayerInput playerInput)
    {
        playerInput.gameObject.SetActive(false);
    }

    private void OnDeviceRegained(PlayerInput playerInput)
    {
        playerInput.gameObject.SetActive(true);
    }
}
