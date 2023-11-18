using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MenuEvent : UnityEvent<int> { }



public class OptionsMenuController : MonoBehaviour
{
    [SerializeField] private MenuController bgmController;
    [SerializeField] private MenuController sfxController;
    [SerializeField] private MenuController resController;
    [SerializeField] private MenuController fullscreenController;
    [SerializeField] private MenuController shakeController;

    private void OnEnable()
    {
        SetupMenu();
    }

    private void SetupMenu()
    {
        float setVal;

        //BGM Settings
        setVal = PlayerPrefs.GetFloat("BGMVolume", GameSettings.defaultBGMVolume) * 10f;
        bgmController.SetIndex((int)setVal);

        //SFX Settings
        setVal = PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume) * 10f;
        sfxController.SetIndex((int)setVal);

        //Res Settings
        resController.SetIndex(PlayerPrefs.GetInt("CurrentRes", -1));

        //Fullscreen Settings
        fullscreenController.SetIndex(PlayerPrefs.GetInt("IsFullscreen", 1));

        //Screenshake Settings
        shakeController.SetIndex(PlayerPrefs.GetInt("Screenshake", 1));
    }

    public void ChangeBGM(int val)
    {
        PlayerPrefs.SetFloat("BGMVolume", val * 0.1f);
        GameSettings.CheckBGM();
        GameManager.Instance.AudioManager.UpdateMusicVolume();
    }

    public void ChangeSFX(int val)
    {
        PlayerPrefs.SetFloat("SFXVolume", val * 0.1f);
        GameSettings.CheckSFX();
        GameManager.Instance.AudioManager.UpdateSFXVolume();
    }

    public void ChangeRes(int val)
    {
        PlayerPrefs.SetInt("CurrentRes", val);
        GameSettings.CheckResolution();
    }

    public void ChangeFullscreen(int val)
    {
        PlayerPrefs.SetInt("IsFullscreen", val);
        GameSettings.CheckFullscreen();
    }

    public void ChangeScreenshake(int val)
    {
        PlayerPrefs.SetInt("Screenshake", val);
        GameSettings.CheckScreenshake();
    }
}
