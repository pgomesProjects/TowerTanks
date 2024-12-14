using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public enum INTERACTABLE { Throttle, EnergyShield, Boiler, Refuel, Cannon, MachineGun, Mortar, Armor, ShopTerminal };

    public class GameManager : SerializedMonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public AudioManager AudioManager { get; private set; }
        public MultiplayerManager MultiplayerManager { get; private set; }
        public ParticleSpawner ParticleSpawner { get; private set; }
        public SystemEffects SystemEffects { get; private set; }
        public GameUIManager UIManager { get; private set; }
        public CargoManager CargoManager { get; private set; }

        [SerializeField, Tooltip("The prefab for the tutorial popup.")] private TutorialPopupController tutorialPopup;
        [SerializeField, Tooltip("The list of all possible tutorials.")] private TutorialPopupSettings[] tutorialsList;

        [SerializeField, Tooltip("The list of possible rooms for the players to pick.")] public RoomInfo[] roomList;
        [SerializeField, Tooltip("The list of possible special rooms for use in the game.")] public RoomInfo[] specialRoomList;
        [SerializeField, Tooltip("The list of possible interactables for the players to pick. NOTE: Remember to update the enum list when updating this list.")] public TankInteractable[] interactableList;

        internal SessionStats currentSessionStats;

        internal int TotalInteractables = Enum.GetNames(typeof(INTERACTABLE)).Length;

        [SerializeField, Tooltip("The time for levels to fade in.")] private float fadeInTime = 1f;
        [SerializeField, Tooltip("The time for levels to fade out.")] private float fadeOutTime = 0.5f;
        [SerializeField, Tooltip("The time for the closing gate transition.")] private float closeGateTime = 1f;
        [SerializeField, Tooltip("The time for the opening gate transition.")] private float openGateTime = 0.5f;
        [SerializeField, Tooltip("The canvas for the loading screen.")] private GameObject loaderCanvas;
        [SerializeField, Tooltip("The loading progress bar.")] private Image progressBar;
        [Tooltip("The settings for all of the button prompts.")] public ButtonPromptSettings buttonPromptSettings;

        public bool tutorialWindowActive;
        public bool isPaused;
        public bool inBugReportMenu;
        public bool inDebugMenu;
        public bool InGameMenu
        {
            get { return isPaused || inBugReportMenu || inDebugMenu || tutorialWindowActive; }
        }

        //Global Static Variables
        public static float gameTimeScale = 1.0f;
        private static float gameDeltaTime;
        private static float gameElapsedTime;
        private static float gameFixedDeltaTimeStep;

        public static float shopChance;
        public float _shopChance;

        public static float GameDeltaTime
        {
            get { return gameDeltaTime; }
        }

        public static float GameTime
        {
            get { return gameElapsedTime; }
        }

        private float target;
        private float loadMaxDelta = 3f;
        private bool loadingScene = false;

        public TankDesign tankDesign;
        public CargoManifest cargoManifest;

        public bool CheatsMenuActive { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            AudioManager = GetComponentInChildren<AudioManager>();
            MultiplayerManager = GetComponentInChildren<MultiplayerManager>();
            ParticleSpawner = GetComponentInChildren<ParticleSpawner>();
            SystemEffects = GetComponentInChildren<SystemEffects>();
            CargoManager = GetComponentInChildren<CargoManager>();
            UIManager = GetComponentInChildren<GameUIManager>();
            currentSessionStats = new SessionStats();
            gameFixedDeltaTimeStep = Time.fixedDeltaTime;

            //LoadBearingCheck();
        }

        private void LoadBearingCheck()
        {
            string loadBearingDataPath = Path.Combine(Application.dataPath, "Resources", "Data", "!ImportantData");
            string filePath = Path.Combine(loadBearingDataPath, "loadBearingChristian.png");

            if (!File.Exists(filePath))
            {
#if UNITY_EDITOR

                Utils.ForceCrash(ForcedCrashCategory.FatalError);
#endif
                throw new Exception("Critical Error: Load bearing file is missing!");
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            switch ((GAMESCENE)scene.buildIndex)
            {
                case GAMESCENE.TITLE:
                    GameSettings.showGamepadCursors = false;

                    //End the campaign if it has not ended already
                    if (CampaignManager.Instance.HasCampaignStarted)
                        CampaignManager.Instance.EndCampaign();
                    break;
                case GAMESCENE.BUILDING:
                    GameSettings.showGamepadCursors = true;
                    break;
                default:
                    GameSettings.showGamepadCursors = false;
                    break;
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            SetGamepadCursorsActive(false);
        }

        /// <summary>
        /// Loads the next scene asynchronously.
        /// </summary>
        /// <param name="sceneName">The name of the scene to load in.</param>
        /// <param name="levelTransitionType">The type of transition to display between loading scenes.</param>
        /// <param name="transitionOnStart">If true, the starting transition plays.</param>
        /// <param name="transitionOnEnd">If true, the ending transition plays.</param>
        /// <param name="loadingScreen">If true, a loading screen is showing in between transitions.</param>
        public async void LoadScene(string sceneName, LevelTransition.LevelTransitionType levelTransitionType, bool transitionOnStart = true, bool transitionOnEnd = true, bool loadingScreen = true)
        {
            if (transitionOnStart)
            {
                switch (levelTransitionType)
                {
                    case LevelTransition.LevelTransitionType.FADE:
                        LevelTransition.Instance?.StartTransition(fadeOutTime, levelTransitionType);
                        await Task.Delay(Mathf.CeilToInt(fadeOutTime * 1000));
                        break;
                    case LevelTransition.LevelTransitionType.GATE:
                        LevelTransition.Instance?.StartTransition(closeGateTime, levelTransitionType);
                        await Task.Delay(Mathf.CeilToInt(closeGateTime * 1000));
                        break;
                }
            }

            target = 0f;
            progressBar.fillAmount = 0f;
            loadingScene = true;

            AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
            scene.allowSceneActivation = false;

            if (loadingScreen)
                loaderCanvas?.SetActive(true);

            do
            {
                await Task.Delay(100);
                target = scene.progress;
            } while (scene.progress < 0.9f);

            await Task.Delay(500);

            scene.allowSceneActivation = true;
            if (loadingScreen)
                loaderCanvas?.SetActive(false);
            loadingScene = false;

            Instance?.AudioManager.StopAllSounds();

            if (transitionOnEnd)
            {
                switch (levelTransitionType)
                {
                    case LevelTransition.LevelTransitionType.FADE:
                        LevelTransition.Instance?.EndTransition(fadeInTime, levelTransitionType);
                        break;
                    case LevelTransition.LevelTransitionType.GATE:
                        LevelTransition.Instance?.EndTransition(openGateTime, levelTransitionType);
                        break;
                }
            }
        }

        private void Update()
        {
            if (loadingScene)
                progressBar.fillAmount = Mathf.MoveTowards(progressBar.fillAmount, target, loadMaxDelta * Time.deltaTime);

            GameTimeScale();
        }

        private void GameTimeScale()
        {
            gameDeltaTime = Time.unscaledDeltaTime * gameTimeScale;
            gameElapsedTime += gameDeltaTime;
            Time.fixedDeltaTime = gameTimeScale * gameFixedDeltaTimeStep;
        }

        /// <summary>
        /// Displays a tutorial on the screen.
        /// </summary>
        /// <param name="tutorialIndex">The index of the tutorial to show.</param>
        public void DisplayTutorial(int tutorialIndex)
        {
            TutorialPopupController currentTutorialPopup = Instantiate(tutorialPopup, GameObject.FindGameObjectWithTag("CursorCanvas").transform);
            currentTutorialPopup.StartTutorial(tutorialsList[tutorialIndex]);
        }

        /// <summary>
        /// Sets the gamepad cursors to be active or not active.
        /// </summary>
        /// <param name="setActive">If true, the gamepad cursors are active. If false, the gamepad cursors are not active.</param>
        public void SetGamepadCursorsActive(bool setActive)
        {
            GameSettings.showGamepadCursors = setActive;
            foreach (var cursor in FindObjectsOfType<GamepadCursor>())
                cursor.RefreshCursor(setActive);
        }

        public void SetPlayerCursorActive(GamepadCursor currentPlayer, bool setActive)
        {
            currentPlayer.RefreshCursor(setActive);
        }

        /// <summary>
        /// Takes a TankInteractable object and converts it into an enum.
        /// </summary>
        /// <param name="interactable">The interactable object to convert.</param>
        /// <returns>Returns an enum that corresponds with the interactable given.</returns>
        public INTERACTABLE TankInteractableToEnum(TankInteractable interactable)
        {
            for (int i = 0; i < interactableList.Length; i++)
            {
                //If the interactable provided has the same stack name as the current interactable in the list, it is the correct item
                if (interactableList[i].stackName == interactable.stackName)
                    return (INTERACTABLE)i;
            }

            //If no interactable is found, return the first enum in the list.
            return default;
        }

        /// <summary>
        /// Takes an Interactable Type & creates an array of all valid Interactables of that type.
        /// </summary>
        /// <param name="type">The interactable type you're looking for.</param>
        /// <returns>Returns an array of interactables of that type.</returns>
        public TankInteractable[] GetInteractablesOfType(TankInteractable.InteractableType type)
        {
            TankInteractable[] validInteractables = null;
            int count = 0;

            //Find Count of Valid Interactables
            foreach (TankInteractable interactable in interactableList)
            {
                if(interactable.interactableType == type)
                {
                    count++;
                }
            }

            //If Count > 0, create an array of the Valid Interactables
            if (count > 0)
            {
                validInteractables = new TankInteractable[count];

                int _count = 0;
                foreach(TankInteractable interactable in interactableList)
                {
                    if (interactable.interactableType == type)
                    {
                        validInteractables[_count] = interactable;
                        _count++;
                    }
                }
            }

            return validInteractables;
        }

        /// <summary>
        /// Takes an Interactable Type & creates an array of all valid Interactables that are specifically NOT that type.
        /// </summary>
        /// <param name="type">The interactable type you're NOT looking for.</param>
        /// <returns>Returns an array of all interactables that are NOT that type.</returns>
        public TankInteractable[] GetInteractablesNotOfType(TankInteractable.InteractableType type)
        {
            TankInteractable[] validInteractables = null;
            int count = 0;

            //Find Count of Valid Interactables
            foreach (TankInteractable interactable in interactableList)
            {
                if (interactable.interactableType != type)
                {
                    if (interactable.interactableType != TankInteractable.InteractableType.CONSUMABLE)
                    {
                        if (interactable.interactableType != TankInteractable.InteractableType.SHOP)
                        {
                            count++;
                        }
                    }
                }
            }

            //If Count > 0, create an array of the Valid Interactables
            if (count > 0)
            {
                validInteractables = new TankInteractable[count];

                int _count = 0;
                foreach (TankInteractable interactable in interactableList)
                {
                    if (interactable.interactableType != type)
                    {
                        if (interactable.interactableType != TankInteractable.InteractableType.CONSUMABLE)
                        {
                            if (interactable.interactableType != TankInteractable.InteractableType.SHOP)
                            {
                                validInteractables[_count] = interactable;
                                _count++;
                            }
                        }
                    }
                }
            }

            return validInteractables;
        }

        public TankInteractable[] GetInteractableList()
        {
            TankInteractable[] validInteractables = new TankInteractable[interactableList.Length - 1];

            for(int i = 0; i < validInteractables.Length; i++)
            {
                validInteractables[i] = interactableList[i];
            }

            return validInteractables;
        }

        public bool SetCheatsMenuActive(bool cheatsActive) => CheatsMenuActive = cheatsActive;
    }
}
