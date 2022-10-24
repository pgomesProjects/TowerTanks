using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Cinemachine;
public class MultiplayerManager : MonoBehaviour
{
    [SerializeField] private CinemachineTargetGroup targetGroup;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform playerParent;

    private Color[] playerColors = { Color.red, Color.blue, Color.yellow, Color.green };

    // Start is called before the first frame update
    void Start()
    {
        //Delegate function to when the scene is unloaded
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    /// <summary>
    /// Behavior for when a player joins the game
    /// </summary>
    /// <param name="playerInput"></param>
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.transform.position = spawnPoint.position;

        //Add the new player to the camera's target group
        targetGroup.AddMember(playerInput.transform, 1, 1);

        //Generate the new player's index
        int playerIndex = ConnectionController.CheckForIndex();
        ConnectionController.connectedControllers[playerIndex] = true;
        playerInput.GetComponent<PlayerController>().SetPlayerIndex(playerIndex);
        FindObjectOfType<CornerUIController>().OnPlayerJoined(playerIndex);
        SetColorOfPlayer(playerInput.transform, playerIndex);
        playerInput.transform.parent = playerParent;
    }

    private void SetColorOfPlayer(Transform player, int playerIndex)
    {
        //Change color of player depending on which player number they are
        player.GetComponent<Renderer>().material.color = playerColors[playerIndex];
    }

    private void OnSceneUnloaded(Scene current)
    {
        //Get rid of the controllers when the game is unloaded
        for(int i = 0; i < ConnectionController.connectedControllers.Length; i++)
        {
            ConnectionController.connectedControllers[i] = false;
        }
    }

}
