using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitlescreenController : MonoBehaviour
{
    private GameObject currentMenuState;

    public enum MenuState {START, MAIN, OPTIONS, CREDITS, DIFFICULTYSETTINGS, SKIPTUTORIAL}

    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject creditsMenu;
    [SerializeField] private GameObject difficultySettingsMenu;
    [SerializeField] private GameObject skipTutorialMenu;

    [Header("Menu First Selected Items")]
    [SerializeField] private Selectable[] mainMenuButtons;
    [SerializeField] private Selectable optionsMenuSelected;
    [SerializeField] private Selectable creditsMenuSelected;
    [SerializeField] private Selectable difficultySettingsMenuSelected;
    [SerializeField] private Selectable skipTutorialMenuSelected;

    private bool inMenu = false;
    [SerializeField] private string sceneToLoad;

    private PlayerControlSystem playerControlSystem;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
        playerControlSystem.UI.Submit.performed += _ => StartScreenToMain();
        playerControlSystem.UI.Cancel.performed += _ => CancelAction();
    }

    // Start is called before the first frame update
    void Start()
    {
        currentMenuState = startMenu;

        if (GameSettings.mainMenuEntered)
        {
            inMenu = true;
            GoToMain();
        }
    }

    private void OnEnable()
    {
        playerControlSystem.Enable();
    }

    private void OnDisable()
    {
        playerControlSystem.Disable();
    }

    private void StartGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void ShowDifficulty()
    {
        SwitchMenu(MenuState.DIFFICULTYSETTINGS);
        difficultySettingsMenuSelected.Select();
    }

    public void ShowDifficultyText(GameObject difficultyText)
    {
        difficultyText.SetActive(true);
    }

    public void HideDifficultyText(GameObject difficultyText)
    {
        difficultyText.SetActive(false);
    }

    public void SetDifficulty(float difficulty)
    {
        GameSettings.difficulty = difficulty;
        SwitchMenu(MenuState.SKIPTUTORIAL);
        skipTutorialMenuSelected.Select();
    }

    public void SetSkipTutorial(bool tutorial)
    {
        GameSettings.skipTutorial = tutorial;
        StartGame();
    }

    private void StartScreenToMain()
    {
        if(!inMenu)
        {
            inMenu = true;
            GameSettings.mainMenuEntered = true;
            StartCoroutine(WaitStartScreenToMain());
        }
    }

    private IEnumerator WaitStartScreenToMain()
    {
        yield return new WaitForSeconds(0.5f);
        GoToMain();
    }

    private void CancelAction()
    {
        //If the player is not in the main menu, go back to the main menu
        if (currentMenuState != mainMenu)
        {
            //If the current menu is the skip tutorial box, go back to the difficulty menu
            if(currentMenuState == skipTutorialMenu)
            {
                ShowDifficulty();
            }
            else
            {
                Back();
            }
        }
        //If they are in the main menu, quit the game
        else
            QuitGame();
    }

    public void GoToMain()
    {
        SwitchMenu(MenuState.MAIN);
    }

    public void Options()
    {
        SwitchMenu(MenuState.OPTIONS);
        optionsMenuSelected.Select();
    }

    public void Credits()
    {
        SwitchMenu(MenuState.CREDITS);
        creditsMenuSelected.Select();
    }

    public void Back()
    {
        SwitchMenu(MenuState.MAIN);
    }

    private void SwitchMenu(MenuState menu)
    {
        GameObject newMenu;

        switch (menu)
        {
            case MenuState.MAIN:
                newMenu = mainMenu;
                SelectButtonOnMain(currentMenuState);
                break;
            case MenuState.OPTIONS:
                newMenu = optionsMenu;
                break;
            case MenuState.CREDITS:
                newMenu = creditsMenu;
                break;
            case MenuState.DIFFICULTYSETTINGS:
                newMenu = difficultySettingsMenu;
                break;
            case MenuState.SKIPTUTORIAL:
                newMenu = skipTutorialMenu;
                break;
            default:
                newMenu = mainMenu;
                SelectButtonOnMain(currentMenuState);
                break;
        }

        if(menu != MenuState.SKIPTUTORIAL)
            currentMenuState.SetActive(false);

        currentMenuState = newMenu;
        currentMenuState.SetActive(true);
    }

    private void SelectButtonOnMain(GameObject menu)
    {
        if (menu == optionsMenu)
        {
            mainMenuButtons[1].Select();
        }
        else if (menu == creditsMenu)
        {
            mainMenuButtons[2].Select();
        }
        else
        {
            mainMenuButtons[0].Select();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
