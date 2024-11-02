using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerTanks.Scripts;

public enum DIFFICULTYMODE { EASY, NORMAL, HARD }

public struct Difficulty
{
    public DIFFICULTYMODE mode;
    public float multiplier;
}

public enum GAMESCENE { TITLE = 0, BUILDING = 1, COMBAT = 3 };

public static class GameSettings
{
    public static bool debugMode = false;
    public static bool customPlayerNames = false;

    public static string controlSchemeUI = "Gamepad";

    public static ConfigurationSettings defaultSettings = new ConfigurationSettings();
    public static ConfigurationSettings currentSettings;

    public static bool mainMenuEntered = false;
    public static bool skipTutorial = true;
    public static bool showGamepadCursors = true;

    //0.5 = Easy, 1 = Normal, 1.5 = Hard
    public static float difficulty = 1f;

    public static PlayerSystemSpecs systemSpecs = new PlayerSystemSpecs();

    public static void CheckBGM()
    {
        AkSoundEngine.SetRTPCValue("MusicVolume", currentSettings.masterVolume * currentSettings.bgmVolume * 100f);
    }

    public static void CheckSFX()
    {
        AkSoundEngine.SetRTPCValue("SFXVolume", currentSettings.masterVolume * currentSettings.sfxVolume * 100f);
    }

    public static void CheckFullscreen()
    {
        Screen.fullScreenMode = currentSettings.isFullScreen == 1? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
    }

    public static void CheckResolution()
    {
        Screen.SetResolution(ConfigurationSettings.possibleResolutions[currentSettings.resolution, 0], ConfigurationSettings.possibleResolutions[currentSettings.resolution, 1], Screen.fullScreenMode);
    }

    public static void RefreshSettings()
    {
        CheckBGM();
        CheckSFX();
        CheckFullscreen();
        CheckResolution();
    }

    public static void ApplyDefaultSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", defaultSettings.masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", defaultSettings.bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", defaultSettings.sfxVolume);
        PlayerPrefs.SetInt("CurrentRes", defaultSettings.resolution);
        PlayerPrefs.SetInt("IsFullscreen", defaultSettings.isFullScreen);
        PlayerPrefs.SetInt("Screenshake", defaultSettings.screenshakeOn);
        currentSettings = defaultSettings;
    }

    public static void CopyToClipboard(string text)
    {
        TextEditor textEditor = new TextEditor();
        textEditor.text = text;
        textEditor.SelectAll();
        textEditor.Copy();
        Debug.Log("Copied " + text + " to clipboard.");
    }

    public static PlatformType GetRunningPlatform()
    {
        switch (Application.platform)
        {
            //Playstation
            case RuntimePlatform.PS4:
            case RuntimePlatform.PS5:
                return PlatformType.PlayStation;
            //Switch
            case RuntimePlatform.Switch:
                return PlatformType.Switch;
            //Xbox
            case RuntimePlatform.XboxOne:
            case RuntimePlatform.GameCoreXboxSeries:
                return PlatformType.Xbox;
            //PC
            default:
                return PlatformType.PC;
        }
    }
}
