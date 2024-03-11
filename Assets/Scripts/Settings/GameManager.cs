using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : SerializedMonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public AudioManager AudioManager { get; private set; }
    public MultiplayerManager MultiplayerManager { get; private set; }

    public ParticleSpawner ParticleSpawner { get; private set; }

    [SerializeField, Tooltip("The list of possible rooms for the players to pick.")] public GameObject[] roomList { get; private set; }

    [SerializeField, Tooltip("The time for levels to fade in.")] private float fadeInTime = 1f;
    [SerializeField, Tooltip("The time for levels to fade out.")] private float fadeOutTime = 0.5f;
    [SerializeField, Tooltip("The canvas for the loading screen.")] private GameObject loaderCanvas;
    [SerializeField, Tooltip("The loading progress bar.")] private Image progressBar;
    private float target;
    private float loadMaxDelta = 3f;
    private bool loadingScene = false;

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
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        SetGamepadCursorsActive(false);
    }

    public async void LoadScene(string sceneName, bool fadeOutScene = true, bool fadeInScene = true)
    {
        if (fadeOutScene)
        {
            LevelFader.Instance?.FadeOut(fadeOutTime);
            await Task.Delay(Mathf.CeilToInt(fadeOutTime * 1000));
        }

        target = 0f;
        progressBar.fillAmount = 0f;
        loadingScene = true;

        AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        loaderCanvas?.SetActive(true);

        do
        {
            await Task.Delay(100);
            target = scene.progress;
        } while (scene.progress < 0.9f);

        await Task.Delay(500);

        scene.allowSceneActivation = true;
        loaderCanvas?.SetActive(false);
        loadingScene = false;

        Instance?.AudioManager.StopAllSounds();

        if(fadeInScene)
            LevelFader.Instance?.FadeIn(fadeInTime);
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
            cursor.RefreshCursor();
    }
}
