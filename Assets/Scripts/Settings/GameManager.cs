using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : SerializedMonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public MultiplayerManager MultiplayerManager { get; private set; }

    public ParticleSpawner ParticleSpawner { get; private set; }

    [SerializeField, Tooltip("The list of possible rooms for the players to pick.")] public GameObject[] roomList;
    //[SerializeField, Tooltip("The list of possible interactables for the players to pick.")] public GameObject[] interactableList { get; private set; }

    [SerializeField, Tooltip("The list of possible cargo types for tanks to carry.")] public GameObject[] cargoList;

    [SerializeField, Tooltip("The time for levels to fade in.")] private float fadeInTime = 1f;
    [SerializeField, Tooltip("The time for levels to fade out.")] private float fadeOutTime = 0.5f;
    [SerializeField, Tooltip("The time for the closing gate transition.")] private float closeGateTime = 1f;
    [SerializeField, Tooltip("The time for the opening gate transition.")] private float openGateTime = 0.5f;
    [SerializeField, Tooltip("The canvas for the loading screen.")] private GameObject loaderCanvas;
    [SerializeField, Tooltip("The loading progress bar.")] private Image progressBar;
    private float target;
    private float loadMaxDelta = 3f;
    private bool loadingScene = false;

    public TankDesign tankDesign;

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
            switch(levelTransitionType)
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
        if(loadingScreen)
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
}
