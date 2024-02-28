using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GamePhaseUI : MonoBehaviour
{
    [SerializeField, Tooltip("The text that shows the new phase active.")] private TextMeshProUGUI levelPhaseText;
    [SerializeField, Tooltip("The building phase UI.")] private RoomBuildingMenu roomBuildingMenu;

    private CanvasGroup phaseUICanvasGroup;

    public static Action OnBuildingPhase;
    public static Action OnCombatPhase;

    private void Awake()
    {
        phaseUICanvasGroup = GetComponent<CanvasGroup>();
        phaseUICanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Updates the UI when a new phase is active.
    /// </summary>
    /// <param name="newPhase">The new game phase.</param>
    public void ShowPhase(GAMESTATE newPhase)
    {
        phaseUICanvasGroup.alpha = 1f;
        string phaseText = "";
        switch (newPhase)
        {
            case GAMESTATE.BUILDING:
                phaseText = "Building Phase";
                OnBuildingPhase?.Invoke();
                break;
            case GAMESTATE.COMBAT:
                phaseText = "Combat Phase";
                OnCombatPhase?.Invoke();
                break;
        }

        levelPhaseText.text = phaseText;
        Invoke("EndingAnimation", 0.5f);
    }

    private void EndingAnimation()
    {
        phaseUICanvasGroup.alpha = 0f;
    }
}
