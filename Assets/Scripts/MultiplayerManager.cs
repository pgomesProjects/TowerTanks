using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Cinemachine;
public class MultiplayerManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform playerParent;

    private PlayerInputManager playerInputManager;

    private Color[] playerColors = { Color.red, Color.blue, Color.yellow, Color.green };

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

        //Delegate join function
        playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    /// <summary>
    /// Behavior for when a player joins the game
    /// </summary>
    /// <param name="playerInput"></param>
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.transform.position = spawnPoint.position;

        //Generate the new player's index
        int playerIndex = ConnectionController.CheckForIndex();
        ConnectionController.connectedControllers[playerIndex] = true;
        playerInput.GetComponent<PlayerController>().SetPlayerIndex(playerIndex);
        FindObjectOfType<CornerUIController>().OnPlayerJoined(playerIndex);
        SetColorOfPlayer(playerInput.transform, playerIndex);
        playerInput.transform.parent = playerParent;
        playerInput.onDeviceLost += OnDeviceLost;
        playerInput.onDeviceRegained += OnDeviceRegained;
    }

    private void SetColorOfPlayer(Transform player, int playerIndex)
    {
        //Change color of player depending on which player number they are
        player.transform.Find("Outline").GetComponent<Renderer>().material.color = playerColors[playerIndex];
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
