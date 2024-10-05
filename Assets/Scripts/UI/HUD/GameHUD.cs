using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace TowerTanks.Scripts
{
    public class GameHUD : MonoBehaviour
    {
        [Tooltip("Singleton instance of this script in game.")] public static GameHUD main;

        [SerializeField, Tooltip("The controller for the pause menu.")] protected PauseController pauseMenu;
        [SerializeField, Tooltip("UI displaying what is in player's stack.")] protected GameObject stackUI;
        [Space]
        [SerializeField, Tooltip("The transform that indicates the amount of scrap in the game.")] protected RectTransform resourcesDisplay;
        [SerializeField, Tooltip("The minimum duration for the resources change.")] protected float minResourcesAnimationDuration = 0.5f;
        [SerializeField, Tooltip("The maximum duration for the resources change.")] protected float maxResourcesAnimationDuration = 2f;
        [SerializeField, Tooltip("The resources animation range (the larger the number, the bigger the amount has to be in order to reach the max resources duration).")] protected float resourcesAnimationDurationRange = 100f;
        [SerializeField, Tooltip("The time (in seconds) of the resources animation.")] protected float resourcesAnimationDuration;
        [SerializeField, Tooltip("The time (in seconds) between the resources showing up and the resources leaving.")] protected float resourcesIdleTime;
        [SerializeField, Tooltip("The ease type for the resources animation.")] protected LeanTweenType resourcesEaseType;

        private float startResourcesPosX = -330f;
        private float endResourcesPosX = 15f;

        private TextMeshProUGUI resourcesDisplayNumber;
        private float currentResourcesValue, displayedResourcesValue;
        private float transitionStartTime;
        private bool resourcesUpdated;

        protected virtual void Awake()
        {
            //Initialize:
            main = this; //Set main game hud to this script
            StackManager.activeStackUI = stackUI; //Send stackUI object reference to stack manager
        }


        protected virtual void Start()
        {
            resourcesDisplay.anchoredPosition = new Vector2(startResourcesPosX, resourcesDisplay.anchoredPosition.y);
            resourcesDisplayNumber = resourcesDisplay.GetComponentInChildren<TextMeshProUGUI>();
            resourcesUpdated = true;

            if (GameSettings.debugMode)
                resourcesDisplayNumber.text = "Inf.";
        }

        protected virtual void OnEnable()
        {
            LevelManager.OnGamePaused += ShowPauseMenu;
            LevelManager.OnGameResumed += HidePauseMenu;
            LevelManager.OnResourcesUpdated += UpdateResources;
        }

        protected virtual void OnDisable()
        {
            LevelManager.OnGamePaused -= ShowPauseMenu;
            LevelManager.OnGameResumed -= HidePauseMenu;
            LevelManager.OnResourcesUpdated -= UpdateResources;
        }

        protected void ShowPauseMenu(int playerPaused)
        {
            pauseMenu.gameObject.SetActive(true);
            pauseMenu.UpdatePausedPlayer(playerPaused);
        }

        protected void HidePauseMenu()
        {
            pauseMenu.ReactivateAllPlayerInput();
            pauseMenu.gameObject.SetActive(false);
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
            {
                transitionStartTime = Time.time;
                StartResourcesAnimation();
            }
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
                    resourcesUpdated = false;
                    float transitionDuration = CalculateTransitionDuration();
                    float progress = Mathf.Clamp01((Time.time - transitionStartTime) / transitionDuration);

                    //Lerp between the displayed resources and the current resources
                    displayedResourcesValue = Mathf.Round(Mathf.Lerp(displayedResourcesValue, currentResourcesValue, progress));

                    //Make sure the display resources ends up as the current resources
                    if (progress >= 1.0f)
                    {
                        displayedResourcesValue = currentResourcesValue;
                    }
                }
                else if (!resourcesUpdated)
                {
                    resourcesUpdated = true;
                    EndResourcesAnimation();
                }

                UpdateResourcesDisplay();
            }
        }

        private void StartResourcesAnimation()
        {
            LeanTween.moveX(resourcesDisplay, endResourcesPosX, resourcesAnimationDuration).setEase(resourcesEaseType);
        }

        private void EndResourcesAnimation()
        {
            Debug.Log("Ending Animation...");
            LeanTween.delayedCall(resourcesIdleTime, () => LeanTween.moveX(resourcesDisplay, startResourcesPosX, resourcesAnimationDuration).setEase(resourcesEaseType));
        }

        /// <summary>
        /// Calculates the amount of time it should take for the resources to animate to the current resources number.
        /// </summary>
        /// <returns>The time it should take to reach the current resources value (in seconds). The larger the difference between the current and displayed value, the smaller duration value is returned.</returns>
        private float CalculateTransitionDuration() => Mathf.Lerp(minResourcesAnimationDuration, maxResourcesAnimationDuration, Mathf.Abs(currentResourcesValue - displayedResourcesValue) / resourcesAnimationDurationRange);
        private void UpdateResourcesDisplay() => resourcesDisplayNumber.text = displayedResourcesValue.ToString("n0");

        protected virtual void OnDestroy()
        {
            if (StackManager.activeStackUI == stackUI) StackManager.activeStackUI = null; //Clear stackManager reference upon destruction
        }
    }
}
