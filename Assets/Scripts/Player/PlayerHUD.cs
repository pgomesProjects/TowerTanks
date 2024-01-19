using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField, Tooltip("The border that shows the player's color.")] private Image playerBorder;
    [SerializeField, Tooltip("The border that shows the player's color.")] private TextMeshProUGUI playerScrapCount;

    public void InitializeHUD(Color playerColor)
    {
        playerBorder.color = playerColor;
        playerScrapCount.text = (0).ToString();
    }

    public void UpdateScrapCount(int newScrapCount)
    {
        playerScrapCount.text = newScrapCount.ToString();
    }
}
