using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSettings
{
    public static int[,] possibleResolutions = new int[,] { { 2560, 1440 }, { 1920, 1080 }, { 1280, 720 } };

    public static bool mainMenuEntered = false;
    public static bool skipTutorial = false;

    //0.5 = Easy, 1 = Normal, 1.5 = Hard
    public static float difficulty = 1;

    public static void CheckBGM()
    {
        Debug.Log("BGM Volume: " + PlayerPrefs.GetFloat("BGMVolume", 0.5f));
    }

    public static void CheckSFX()
    {
        Debug.Log("SFX Volume: " + PlayerPrefs.GetFloat("SFXVolume", 0.5f));
    }

    public static void CheckResolution()
    {
        Screen.SetResolution(
            possibleResolutions[PlayerPrefs.GetInt("CurrentRes", -1), 0],
            possibleResolutions[PlayerPrefs.GetInt("CurrentRes", -1), 1],
            Screen.fullScreenMode);

        Debug.Log("Current Resolution: " + possibleResolutions[PlayerPrefs.GetInt("CurrentRes", -1), 0] + " x " + possibleResolutions[PlayerPrefs.GetInt("CurrentRes", -1), 1]);
    }

    public static void CheckFullscreen()
    {
        switch(PlayerPrefs.GetInt("IsFullscreen", 1)){
            case 0:
                Debug.Log("Game Is Windowed");
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 1:
                Debug.Log("Game Is Fullscreen");
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
        }
    }

    public static void CheckScreenshake()
    {
        switch (PlayerPrefs.GetInt("Screenshake", 1))
        {
            case 0:
                Debug.Log("Screen Shake: Off");
                break;
            case 1:
                Debug.Log("Screen Shake: On");
                break;
        }
    }
}
