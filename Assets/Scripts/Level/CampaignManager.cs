using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance;

    [SerializeField, Tooltip("The level event data that dictates how the level must be run.")] private LevelEvents currentLevelEvent;

    private int currentRound;
    private bool hasCampaignStarted = false;

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
        if (!hasCampaignStarted)
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
        currentRound++;
        StackManager.main?.GenerateExistingStack();
    }

    /// <summary>
    /// Sets up the campaign manager to have the information for the current campaign being run.
    /// </summary>
    public void SetupCampaign()
    {
        currentRound = 1;

        foreach (TankInteractable interactable in currentLevelEvent.startingInteractables)
        {
            StackManager.AddToStack(interactable);
        }

        hasCampaignStarted = true;
    }

    public void SetLevelEvent(LevelEvents levelEvent)
    {
        currentLevelEvent = levelEvent;
    }

    public LevelEvents GetCurrentLevelEvent() => currentLevelEvent;
    public int GetCurrentRound() => currentRound;
}
