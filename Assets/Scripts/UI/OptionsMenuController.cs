using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TowerTanks.Scripts
{
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
            setVal = GameSettings.currentSettings.bgmVolume * 10f;
            bgmController.SetIndex((int)setVal);

            //SFX Settings
            setVal = GameSettings.currentSettings.sfxVolume * 10f;
            sfxController.SetIndex((int)setVal);

            //Res Settings
            resController.SetIndex(GameSettings.currentSettings.resolution);

            //Fullscreen Settings
            fullscreenController.SetIndex(GameSettings.currentSettings.isFullScreen);

            //Screenshake Settings
            shakeController.SetIndex(GameSettings.currentSettings.screenshakeOn);
        }

        public void ChangeBGM(int val)
        {
            PlayerPrefs.SetFloat("BGMVolume", val * 0.1f);
            GameSettings.currentSettings.SetBGMVolume(val * 0.1f);
            GameSettings.CheckBGM();
            GameManager.Instance.AudioManager.UpdateMusicVolume();
        }

        public void ChangeSFX(int val)
        {
            PlayerPrefs.SetFloat("SFXVolume", val * 0.1f);
            GameSettings.currentSettings.SetSFXVolume(val * 0.1f);
            GameSettings.CheckSFX();
            GameManager.Instance.AudioManager.UpdateSFXVolume();
        }

        public void ChangeRes(int val)
        {
            PlayerPrefs.SetInt("CurrentRes", val);
            GameSettings.currentSettings.SetResolution(val);
            GameSettings.CheckResolution();
        }

        public void ChangeFullscreen(int val)
        {
            PlayerPrefs.SetInt("IsFullscreen", val);
            GameSettings.currentSettings.SetFullscreen(val);
            GameSettings.CheckFullscreen();
        }

        public void ChangeScreenshake(int val)
        {
            PlayerPrefs.SetInt("Screenshake", val);
            GameSettings.currentSettings.SetScreenshakeOn(val);
        }
    }
}
