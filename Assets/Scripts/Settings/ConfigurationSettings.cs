using UnityEngine;

[System.Serializable]
public class ConfigurationSettings
{
    public static readonly int[,] possibleResolutions = new int[,] { { 2560, 1440 }, { 1920, 1080 }, { 1280, 720 } };

    //Volume
    public float masterVolume { get; private set; }
    public float bgmVolume { get; private set; }
    public float sfxVolume { get; private set; }

    //Resolution Settings
    public int resolution { get; private set; }

    //Fullscreen Settings
    public int isFullScreen { get; private set; }

    //Screenshake Settings
    public int screenshakeOn { get; private set; }

    /// <summary>
    /// The default settings for the configuration.
    /// </summary>
    public ConfigurationSettings()
    {
        masterVolume = 1f;
        bgmVolume = 0.5f;
        sfxVolume = 0.5f;
        isFullScreen = 1;
        screenshakeOn = 1;
        GetDefaultResolutionSettings();
    }

    /// <summary>
    /// Sets the default resolution settings to the user's native resolution.
    /// </summary>
    private void GetDefaultResolutionSettings()
    {
        resolution = 1;

        if (isFullScreen == 1)
        {
            for (int i = 0; i < possibleResolutions.GetLength(0); i++)
            {
                if (Screen.currentResolution.width == possibleResolutions[i, 0]
                    && Screen.currentResolution.height == possibleResolutions[i, 1])
                {
                    resolution = i;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < possibleResolutions.GetLength(0); i++)
            {
                if (Screen.width == possibleResolutions[i, 0]
                    && Screen.height == possibleResolutions[i, 1])
                {
                    resolution = i;
                    break;
                }
            }
        }
    }

    public void SetMasterVolume(float masterVolume) => this.masterVolume = masterVolume;
    public void SetBGMVolume(float bgmVolume) => this.bgmVolume = bgmVolume;
    public void SetSFXVolume(float sfxVolume) => this.sfxVolume = sfxVolume;

    public void SetResolution(int resolution) => this.resolution = resolution;

    public void SetFullscreen(int isFullScreen) => this.isFullScreen = isFullScreen;

    public void SetScreenshakeOn(int screenshakeOn) => this.screenshakeOn = screenshakeOn;

    /// <summary>
    /// Prints all of the user's configuration settings.
    /// </summary>
    /// <returns>Returns the configuration settings.</returns>
    public override string ToString()
    {
        string log = "===Configuration Settings===\n";

        log += "Master Volume: " + masterVolume + "\n";
        log += "BGM Volume: " + bgmVolume + "\n";
        log += "SFX Volume: " + sfxVolume + "\n";
        log += "Resolution: " + possibleResolutions[resolution, 0] + " x " + possibleResolutions[resolution, 1] + "\n";
        log += "Is Fullscreen: " + (isFullScreen == 1? "True" : "False") + "\n";
        log += "Screenshake On: " + (screenshakeOn == 1 ? "True" : "False") + "\n";

        return log;
    }
}
