using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SessionStatsController : MonoBehaviour
{
    private SessionStats sessionStats;
    [SerializeField] private TextMeshProUGUI sessionText;

    private PlayerControlSystem playerControlSystem;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
        playerControlSystem.UI.Submit.performed += _ => GoToMain();
    }

    private void OnEnable()
    {
        playerControlSystem.Enable();
        sessionStats = LevelManager.instance.currentSessionStats;
        CreateSessionsData();
    }

    private void OnDisable()
    {
        playerControlSystem.Disable();
    }

    private void CreateSessionsData()
    {
        string sessionsData = "" +
            "Waves Cleared: " + sessionStats.wavesCleared + "\n" +
            "Maximum Height: " + sessionStats.maxHeight + " Layers \n" +
            "Normal Tanks Defeated: " + sessionStats.normalTanksDefeated + "\n"+
            "Drill Tanks Defeated: " + sessionStats.drillTanksDefeated + "\n" +
            "Number Of Cannons Built: " + sessionStats.numberOfCannons + "\n" +
            "Number Of Ammo Crates Built: " + sessionStats.numberOfAmmoCrates + "\n" +
            "Number Of Engines Built: " + sessionStats.numberOfEngines + "\n" +
            "Number Of Throttles Built: " + sessionStats.numberOfThrottles + "";

        sessionText.text = sessionsData;
    }

    private void GoToMain()
    {
        LevelFader.instance.FadeToLevel("Title");
        Time.timeScale = 1.0f;
    }
}
