using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadyComponent : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI readyComponentText;
    [SerializeField] private Image readyImage;

    [SerializeField] private Color notReadyColor;
    [SerializeField] private Color readyColor;
    private bool isReady;

    public void UpdatePlayerNumber(int playerIndex)
    {
        readyComponentText.text = "Player " + (playerIndex + 1).ToString() + " Status:";
    }

    public void UpdateReadyStatus(bool isPlayerReady)
    {
        isReady = isPlayerReady;
        readyImage.color = isReady ? readyColor : notReadyColor;
    }

    public bool IsReady() => isReady;
}
