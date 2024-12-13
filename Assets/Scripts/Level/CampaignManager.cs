using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerTanks.Scripts
{
    public class CampaignManager : MonoBehaviour
    {
        public static CampaignManager Instance;

        [SerializeField, Tooltip("The level event data that dictates how the level must be run.")] private LevelEvents currentLevelEvent;

        internal string PlayerTankName { get; private set; }
        internal int CurrentRound { get; private set; }
        internal bool HasCampaignStarted { get; private set; }

        public static Action OnCampaignStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            HasCampaignStarted = false;
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

            switch ((GAMESCENE)scene.buildIndex)
            {
                case GAMESCENE.BUILDING:
                    if (HasCampaignStarted)
                    {
                        CurrentRound++;
                    }
                    break;
            }

            if((GAMESCENE)scene.buildIndex != GAMESCENE.TITLE)
                StackManager.main?.GenerateExistingStack();
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
            HasCampaignStarted = true;
        }

        public void SetLevelEvent(LevelEvents levelEvent)
        {
            currentLevelEvent = levelEvent;
        }

        public void EndCampaign()
        {
            Debug.Log("Ending Campaign...");

            GameManager.Instance.tankDesign = null;
            StackManager.ClearStack();
            FindObjectOfType<AnalyticsSender>()?.SubmitAnalytics();

            HasCampaignStarted = false;
        }

        public void SetPlayerTankName(string playerTankName) => PlayerTankName = playerTankName;
        public LevelEvents GetCurrentLevelEvent() => currentLevelEvent;
    }
}
