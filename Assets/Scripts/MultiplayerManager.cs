using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
public class MultiplayerManager : MonoBehaviour
{
    [SerializeField] private CinemachineTargetGroup targetGroup;

    internal int numberOfPlayers = 0;
    private Color[] playerColors = { Color.red, Color.blue, Color.yellow, Color.green};

    // Start is called before the first frame update
    void Start()
    {

    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        //Add the new player to the camera's target group
        targetGroup.AddMember(playerInput.transform, 1, 1);
        numberOfPlayers++;

        SetColorOfPlayer(playerInput.transform);
    }

    private void SetColorOfPlayer(Transform player)
    {
        //Change color of player depending on which player number they are
        player.GetComponent<Renderer>().material.color = playerColors[numberOfPlayers - 1];
    }
}
