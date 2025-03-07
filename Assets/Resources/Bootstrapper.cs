using UnityEngine;
using UnityEngine.Rendering;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Execute()
    {
        //Before the scene loads, spawn an Init prefab and make sure it never gets destroyed, even between scenes
        Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("Init")));
        Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("CampaignManager")));
        Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("DebugCanvas")));
        Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("AnalyticsManager")));

        Cursor.lockState = CursorLockMode.Confined;
        GetCurrentSettings();
        GameSettings.gamePlatform = GameSettings.GetRunningPlatform();
        Debug.Log("Current Platform: " + GameSettings.gamePlatform);
        GameSettings.RefreshSettings();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAfterSceneLoad()
    {
        DebugManager.instance.enableRuntimeUI = false;
    }

    private static void GetCurrentSettings()
    {
        ConfigurationSettings currentSettings = new ConfigurationSettings();
        currentSettings.SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", GameSettings.defaultSettings.masterVolume));
        currentSettings.SetBGMVolume(PlayerPrefs.GetFloat("BGMVolume", GameSettings.defaultSettings.bgmVolume));
        currentSettings.SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSettings.sfxVolume));
        currentSettings.SetResolution(PlayerPrefs.GetInt("CurrentRes", GameSettings.defaultSettings.resolution));
        currentSettings.SetFullscreen(PlayerPrefs.GetInt("IsFullscreen", GameSettings.defaultSettings.isFullScreen));
        currentSettings.SetScreenshakeOn(PlayerPrefs.GetInt("Screenshake", GameSettings.defaultSettings.screenshakeOn));
        GameSettings.currentSettings = currentSettings;
    }
}