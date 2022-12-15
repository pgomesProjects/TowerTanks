using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    private void OnEnable()
    {
        playerControlSystem.Enable();
        currentMenuState = pauseMenu;
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
                break;
            case MenuState.OPTIONS:
                newMenu = optionsMenu;
                break;
            default:
                newMenu = pauseMenu;
                break;
        }

        if (currentMenuState != null)
        {
            currentMenuState.SetActive(false);

            GameObject prevMenu = currentMenuState;

            DeselectButton();

            currentMenuState = newMenu;
            currentMenuState.SetActive(true);

            if (menu == MenuState.PAUSE || menu == MenuState.REFRESH)
                SelectButtonOnPause(prevMenu);
        }
    }

    private void DeselectButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
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

    public void ButtonOnSelectColor(Animator anim)
    {
        anim.SetBool("IsSelected", true);
        PlayButtonSFX("Hover");
    }

    public void ButtonOnDeselectColor(Animator anim)
    {
        anim.SetBool("IsSelected", false);

    }

    public void ButtonBounce(RectTransform rectTransform)
    {
        StartCoroutine(ButtonBounceAnimation(rectTransform));
    }

    private IEnumerator ButtonBounceAnimation(RectTransform rectTransform)
    {
        Vector3 startPos = rectTransform.localPosition;
        Vector3 endPos = startPos;
        endPos.y += 10;

        float seconds = 0.067f;

        float timeElapsed = 0;
        while (timeElapsed < seconds)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            rectTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            timeElapsed += Time.deltaTime;

            yield return new WaitForSecondsRealtime(0);
        }

        rectTransform.localPosition = endPos;

        timeElapsed = 0;
        while (timeElapsed < seconds)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            rectTransform.localPosition = Vector3.Lerp(endPos, startPos, t);
            timeElapsed += Time.deltaTime;

            yield return new WaitForSecondsRealtime(0);
        }

        rectTransform.localPosition = startPos;
    }

    public void Back()
    {
        SwitchMenu(MenuState.PAUSE);
    }

}
