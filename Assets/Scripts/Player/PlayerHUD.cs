using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField, Tooltip("The border that shows the player's color.")] private Image playerBorder;

    [SerializeField, Tooltip("The fill of the health bar.")] private Image healthBar;
    [SerializeField, Tooltip("The fill of the health bar.")] private Image fuelBar;
    [SerializeField, Tooltip("The fill of the health bar.")] private Image progressBar;

    [SerializeField, Tooltip("The Image for the player button prompt.")] private Image buttonPrompt;

    /// <summary>
    /// Initializes the player HUD to its default stats.
    /// </summary>
    /// <param name="playerColor">The color that indicates the player.</param>
    public void InitializeHUD(Color playerColor)
    {
        playerBorder.color = playerColor;
        healthBar.fillAmount = 1f;
        fuelBar.fillAmount = 1f;
        progressBar.fillAmount = 0f;
        buttonPrompt.sprite = null;
        buttonPrompt.color = new Color(0, 0, 0, 0);
    }


    /// <summary>
    /// Updates the value of a player stat bar.
    /// </summary>
    /// <param name="statBar">The stat to update.</param>
    /// <param name="fillAmount">The amount to fill the bar (0 = empty, 1 = full).</param>
    public void UpdateStatBar(PlayerStat statBar, float fillAmount)
    {
        switch(statBar){
            case PlayerStat.HEALTH:
                healthBar.fillAmount = fillAmount;
                break;
            case PlayerStat.FUEL:
                fuelBar.fillAmount = fillAmount;
                break;
            case PlayerStat.PROGRESS:
                progressBar.fillAmount = fillAmount;
                break;
        }
    }

    public void ShowButtonPrompt(Sprite buttonPromptSprite)
    {
        buttonPrompt.sprite = buttonPromptSprite;
        buttonPrompt.color = buttonPromptSprite == null ? new Color(0, 0, 0, 0) : new Color(1, 1, 1, 1);
    }
}
