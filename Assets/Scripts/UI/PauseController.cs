using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseController : MonoBehaviour
{
    private GameObject currentMenuState;
    public enum MenuState { PAUSE, OPTIONS, REFRESH }

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject optionsMenu;

    [Header("Menu First Selected Items")]
    [SerializeField] private Selectable[] pauseMenuButtons;
    [SerializeField] private Selectable optionsMenuSelected;

    [SerializeField] private TextMeshProUGUI pauseText;

    private int currentPlayerPaused;

    private PlayerControlSystem playerControlSystem;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
        playerControlSystem.UI.Cancel.performed += _ => CancelAction();
    }

    private void Start()
    {
        currentMenuState = pauseMenu;
    }

    private void OnEnable()
    {
        playerControlSystem.Enable();
        SwitchMenu(MenuState.REFRESH);
    }

    private void OnDisable()
    {
        playerControlSystem.Disable();
    }

    private void CancelAction()
    {
        if (currentMenuState == optionsMenu)
        {
            Back();
        }
        else
        {
            Resume();
        }
        PlayButtonSFX("Cancel");
    }

    private void SwitchMenu(MenuState menu)
    {
        GameObject newMenu;

        switch (menu)
        {
            case MenuState.PAUSE:
                newMenu = pauseMenu;
                SelectButtonOnPause(currentMenuState);
                break;
            case MenuState.OPTIONS:
                newMenu = optionsMenu;
                break;
            default:
                newMenu = pauseMenu;
                SelectButtonOnPause(pauseMenu);
                break;
        }

        if(currentMenuState != null)
        {
            currentMenuState.SetActive(false);
            currentMenuState = newMenu;
            currentMenuState.SetActive(true);
        }
    }

    private void SelectButtonOnPause(GameObject menu)
    {
        if (menu == optionsMenu)
        {
            pauseMenuButtons[1].Select();
        }
        else
        {
            pauseMenuButtons[0].Select();
        }
    }

    public void Resume()
    {
        LevelManager.instance.PauseToggle(currentPlayerPaused);
    }

    public void Options()
    {
        SwitchMenu(MenuState.OPTIONS);
        optionsMenuSelected.Select();
    }

    public void ReturnToMain()
    {
        SceneManager.LoadScene("Title");
        Time.timeScale = 1.0f;
        FindObjectOfType<AudioManager>().StopAllSounds();
    }
    
    public void UpdatePauseText(int playerIndex)
    {
        pauseText.text = "Player " + (playerIndex + 1) + " Paused";
        currentPlayerPaused = playerIndex;
    }

    public void PlayButtonSFX(string name)
    {
        FindObjectOfType<AudioManager>().PlayOneShot("Button" + name, PlayerPrefs.GetFloat("SFXVolume", 0.5f));
    }

    public void Back()
    {
        SwitchMenu(MenuState.PAUSE);
    }

}
