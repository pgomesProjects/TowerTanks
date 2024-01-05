using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Execute()
    {
        //Before the scene loads, spawn an Init prefab and make sure it never gets destroyed, even between scenes
        Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("Init")));
        if (GameSettings.debugMode)
        {
            Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("DebugCanvas")));
        }

        Cursor.lockState = CursorLockMode.Confined;
        SettingsOnStart();
    }

    private static void SettingsOnStart()
    {
        GameSettings.CheckBGM();
        GameSettings.CheckSFX();
        GameSettings.CheckFullscreen();
        ResolutionOnStart();
        GameSettings.CheckScreenshake();
    }

    private static void ResolutionOnStart()
    {
        int currentResolutionIndex = -1;


        //If the game is currently fullscreen, check the size of the whole screen
        if (PlayerPrefs.GetInt("IsFullscreen", 1) == 1)
        {
            for (int i = 0; i < GameSettings.possibleResolutions.GetLength(0); i++)
            {
                if (Screen.currentResolution.width == GameSettings.possibleResolutions[i, 0]
                    && Screen.currentResolution.height == GameSettings.possibleResolutions[i, 1])
                    currentResolutionIndex = i;
            }
        }
        else
        {
            for (int i = 0; i < GameSettings.possibleResolutions.GetLength(0); i++)
            {
                if (Screen.width == GameSettings.possibleResolutions[i, 0]
                    && Screen.height == GameSettings.possibleResolutions[i, 1])
                    currentResolutionIndex = i;
            }
        }

        //Set to 1080p if none apply
        if (currentResolutionIndex == -1)
            currentResolutionIndex = 1;

        PlayerPrefs.SetInt("CurrentRes", currentResolutionIndex);

        GameSettings.CheckResolution();
    }
}