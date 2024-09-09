using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance;

    [SerializeField, Tooltip("The level event data that dictates how the level must be run.")] private LevelEvents currentLevelEvent;

    internal string PlayerTankName { get; private set; }
    internal int CurrentRound { get; private set; }

    private bool hasCampaignStarted = false;

    public static Action OnCampaignStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!hasCampaignStarted && SceneManager.GetActiveScene().name == "BuildTankScene")
            SetupCampaign();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        //If the game has reached the title screen, destroy self since we don't need this anymore
        if ((GAMESCENE)scene.buildIndex == GAMESCENE.TITLE)
        { 
            Destroy(gameObject);
        }
        else
        {
            CurrentRound++;
            StackManager.main?.GenerateExistingStack();
        }
    }

    /// <summary>
    /// Sets up the campaign manager to have the information for the current campaign being run.
    /// </summary>
    public void SetupCampaign()
    {
        Debug.Log("Setting Up Campaign...");
        CurrentRound = 1;

        StackManager.ClearStack();
        foreach (INTERACTABLE interactable in currentLevelEvent.startingInteractables)
            StackManager.AddToStack(interactable);

        OnCampaignStarted?.Invoke();
    }

    public void SetLevelEvent(LevelEvents levelEvent)
    {
        currentLevelEvent = levelEvent;
    }

    public void SetPlayerTankName(string playerTankName) => PlayerTankName = playerTankName;
    public LevelEvents GetCurrentLevelEvent() => currentLevelEvent;
}
