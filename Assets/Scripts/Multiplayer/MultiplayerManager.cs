using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MultiplayerManager : MonoBehaviour
{
    internal static bool[] connectedControllers;

    [SerializeField] private Color[] playerColors = { Color.red, Color.blue, Color.yellow, Color.green };

    private Transform[] spawnPoints;
    private Transform playerParent;

    private PlayerInputManager playerInputManager;
    private MultiplayerUI currentMultiplayerUI;

    private bool onStartJoin;

    private void Awake()
    {
        playerInputManager = FindObjectOfType<PlayerInputManager>();
        connectedControllers = new bool[playerInputManager.maxPlayerCount];
        for (int i = 0; i < connectedControllers.Length; i++)
            connectedControllers[i] = false;
        playerInputManager.EnableJoining();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Find any multiplayer UI available
        currentMultiplayerUI = FindObjectOfType<MultiplayerUI>();

        //Delegate functions to when the scene is loaded
        SceneManager.sceneLoaded += OnSceneLoaded;

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        //Delegate join function
        playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    private void AddPlayersOnStart()
    {
        onStartJoin = true;

        int playerIndex = 0;
        foreach(var player in transform.GetComponentsInChildren<PlayerController>())
        {
            SetupPlayer(player.gameObject, playerIndex);
            playerIndex++;
        }

        onStartJoin = false;
    }

    /// <summary>
    /// Behavior for when a player joins the game
    /// </summary>
    /// <param name="playerInput"></param>
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.transform.SetParent(transform);
        playerInput.defaultControlScheme = playerInput.currentControlScheme;
        playerInput.GetComponent<Rigidbody2D>().isKinematic = true;

        //Generate the new player's index
        int playerIndex = ConnectionController.CheckForIndex();
        connectedControllers[playerIndex] = true;

        playerInput.GetComponent<PlayerController>().SetPlayerIndex(playerIndex);

        playerInput.name = "Player " + (playerIndex + 1).ToString();

        playerInput.onDeviceLost += OnDeviceLost;
        playerInput.onDeviceRegained += OnDeviceRegained;

        //If the player is in a level
        if (LevelManager.instance != null)
        {
            SetupPlayer(playerInput.gameObject, playerIndex);
        }

        if (currentMultiplayerUI != null && !onStartJoin)
            currentMultiplayerUI.OnPlayerJoined(playerIndex, playerColors[playerIndex], playerInput.currentControlScheme);
    }

    private void SetupPlayer(GameObject currentPlayer, int playerIndex)
    {
        //Move the player to the spawn point
        currentPlayer.GetComponent<Rigidbody2D>().isKinematic = false;
        currentPlayer.transform.position = spawnPoints[playerIndex].position;
        currentPlayer.transform.SetParent(playerParent);

        SetColorOfPlayer(currentPlayer.transform, playerIndex);

        if (LevelManager.instance.levelPhase == GAMESTATE.GAMEACTIVE || GameSettings.skipTutorial)
            currentPlayer.GetComponent<PlayerController>().SetPlayerMove(true);
    }

    private void SetColorOfPlayer(Transform player, int playerIndex)
    {
        //Change color of player depending on which player number they are
        player.transform.GetComponent<Renderer>().material.SetColor("_Color", playerColors[playerIndex]);
    }

    private void OnSceneLoaded(Scene current, LoadSceneMode loadSceneMode)
    {
        currentMultiplayerUI = FindObjectOfType<MultiplayerUI>();

        if (current.name == "GameScene")
        {
            playerParent = GameObject.FindGameObjectWithTag("PlayerContainer").transform;
            spawnPoints = FindObjectOfType<SpawnPoints>().spawnPoints;

            //Add players on start
            AddPlayersOnStart();
        }
        else
        {
            playerParent = transform;
            spawnPoints = null;

            foreach (var player in playerParent.GetComponentsInChildren<PlayerController>())
            {
                player.GetComponent<Rigidbody2D>().isKinematic = true;
                player.transform.position = Vector3.zero;
            }
        }
    }

    public void ChildPlayerInput()
    {
        foreach (var player in playerParent.GetComponentsInChildren<PlayerController>())
        {
            player.transform.SetParent(transform);
        }
    }

    public void SetUIControlScheme()
    {
        GameSettings.controlSchemeUI = playerParent.GetComponentInChildren<PlayerInput>().currentControlScheme;
    }

    public void ClearAllPlayers()
    {
        //Destroy all players
        foreach (var player in playerParent.GetComponentsInChildren<PlayerController>())
            Destroy(player.gameObject);

        //Set all connected controllers to false
        for (int i = 0; i < connectedControllers.Length; i++)
            connectedControllers[i] = false;
    }

    private void OnDeviceLost(PlayerInput playerInput)
    {
        playerInput.gameObject.SetActive(false);
        if (currentMultiplayerUI != null)
            currentMultiplayerUI.OnPlayerLost(playerInput.playerIndex);
    }

    private void OnDeviceRegained(PlayerInput playerInput)
    {
        playerInput.gameObject.SetActive(true);
        if (currentMultiplayerUI != null)
            currentMultiplayerUI.OnPlayerRejoined(playerInput.playerIndex);
    }

    public PlayerInput[] GetPlayerInputs() => playerParent.GetComponentsInChildren<PlayerInput>();
}
