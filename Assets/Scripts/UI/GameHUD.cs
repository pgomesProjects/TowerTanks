using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField, Tooltip("The controller for the pause menu.")] private PauseController pauseMenu;
    [SerializeField, Tooltip("The GameObject for the game over menu.")] private GameObject gameOverMenu;
    [SerializeField, Tooltip("The delay (in seconds) for the game over screen to show.")] private float gameOverDelay;
    [SerializeField, Tooltip("The controller for the session stats menu.")] private SessionStatsController sessionStatsMenu;

    [SerializeField, Tooltip("The prompt that tells the player to advance forward.")] private GoArrowAnimation goPrompt;
    [SerializeField, Tooltip("The alarm animation for incoming enemies.")] private AlarmAnimation alarmAnimation;

    [SerializeField, Tooltip("The text that indicates the amount of scrap in the game.")] private TextMeshProUGUI resourcesDisplay;
    [SerializeField, Tooltip("The minimum duration for the resources change.")] private float minResourcesAnimationDuration = 0.5f;
    [SerializeField, Tooltip("The maximum duration for the resources change.")] private float maxResourcesAnimationDuration = 2f;
    [SerializeField, Tooltip("The resources animation range (the larger the number, the bigger the amount has to be in order to reach the max resources duration).")] private float resourcesAnimationDurationRange = 100f;

    private float currentResourcesValue, displayedResourcesValue;
    private float transitionStartTime;

    // Start is called before the first frame update
    void Start()
    {
        if (GameSettings.debugMode)
            resourcesDisplay.text = "Inf.";
    }

    private void OnEnable()
    {
        LevelManager.OnGamePaused += ShowPauseMenu;
        LevelManager.OnGameResumed += HidePauseMenu;
        LevelManager.OnGameOver += ShowGameOverScreen;
        EnemySpawnManager.OnEnemySpawned += HideGoPrompt;
        EnemySpawnManager.OnEnemyInRange += ShowEnemyAlarm;
        LevelManager.OnCombatEnded += EndCombat;
        LevelManager.OnResourcesUpdated += UpdateResources;
    }

    private void OnDisable()
    {
        LevelManager.OnGamePaused -= ShowPauseMenu;
        LevelManager.OnGameResumed -= HidePauseMenu;
        LevelManager.OnGameOver -= ShowGameOverScreen;
        EnemySpawnManager.OnEnemySpawned -= HideGoPrompt;
        EnemySpawnManager.OnEnemyInRange -= ShowEnemyAlarm;
        LevelManager.OnCombatEnded -= EndCombat;
        LevelManager.OnResourcesUpdated -= UpdateResources;
    }

    private void ShowPauseMenu(int playerPaused)
    {
        pauseMenu.gameObject.SetActive(true);
        pauseMenu.UpdatePausedPlayer(playerPaused);
    }

    private void HidePauseMenu()
    {
        pauseMenu.ReactivateAllPlayerInput();
        pauseMenu.gameObject.SetActive(false);
    }

    private async void ShowGameOverScreen()
    {
        gameOverMenu.SetActive(true);

        await Task.Delay(Mathf.CeilToInt(gameOverDelay * 1000));

        gameOverMenu.SetActive(false);
        sessionStatsMenu.gameObject.SetActive(true);
    }

    /// <summary>
    /// Updates the amount of resources.
    /// </summary>
    /// <param name="amount">The amount to change the resources by.</param>
    /// <param name="animate">The true, the current resources animates to the current resources.</param>
    private void UpdateResources(int amount, bool animate = true)
    {
        //Don't do anything if the game is in debug mode
        if (GameSettings.debugMode)
            return;
        
        currentResourcesValue += amount;

        //If specified not to animate the resources value, just update the displayed value immediately
        if (!animate)
            displayedResourcesValue = currentResourcesValue;
        else
            transitionStartTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        RefreshResourcesDisplay();
    }

    /// <summary>
    /// Keeps the displayed resources value up to date.
    /// </summary>
    private void RefreshResourcesDisplay()
    {
        if (!GameSettings.debugMode)
        {
            if (displayedResourcesValue != currentResourcesValue)
            {
                //Calculate the progress based on the time elapsed and the time the transition started
                float transitionDuration = CalculateTransitionDuration();
                float progress = Mathf.Clamp01((Time.time - transitionStartTime) / transitionDuration);

                //Lerp between the displayed resources and the current resources
                displayedResourcesValue = Mathf.Round(Mathf.Lerp(displayedResourcesValue, currentResourcesValue, progress));

                //Make sure the display resources ends up as the current resources
                if (progress >= 1.0f)
                    displayedResourcesValue = currentResourcesValue;
            }

            UpdateResourcesDisplay();
        }
    }

    /// <summary>
    /// The function called to show the enemy alarm.
    /// </summary>
    private void ShowEnemyAlarm()
    {
        alarmAnimation.gameObject.SetActive(true);
    }

    /// <summary>
    /// The function called when combat has ended.
    /// </summary>
    private void EndCombat()
    {
        ShowGoPrompt();
    }

    private void ShowGoPrompt() => goPrompt?.gameObject.SetActive(true);
    private void HideGoPrompt() => goPrompt?.EndAnimation();

    /// <summary>
    /// Calculates the amount of time it should take for the resources to animate to the current resources number.
    /// </summary>
    /// <returns>The time it should take to reach the current resources value (in seconds). The larger the difference between the current and displayed value, the smaller duration value is returned.</returns>
    private float CalculateTransitionDuration() => Mathf.Lerp(minResourcesAnimationDuration, maxResourcesAnimationDuration, Mathf.Abs(currentResourcesValue - displayedResourcesValue) / resourcesAnimationDurationRange);
    private void UpdateResourcesDisplay() => resourcesDisplay.text = displayedResourcesValue.ToString("n0");
}