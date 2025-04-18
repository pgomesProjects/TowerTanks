using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class TitlescreenController : MonoBehaviour
    {
        private GameObject currentMenuState;

        public enum MenuState { START, MAIN, OPTIONS, CREDITS, DIFFICULTYSETTINGS }

        [SerializeField] private GameObject startMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject optionsMenu;
        [SerializeField] private GameObject creditsMenu;
        [SerializeField] private GameObject difficultySettingsMenu;

        [Header("Menu Animators")]
        [SerializeField] private MoveAnimation startScreenAnimator;
        [SerializeField] private MoveAnimation mainMenuAnimator;

        [Header("Menu First Selected Items")]
        [SerializeField] private Selectable[] mainMenuButtons;
        [SerializeField] private Selectable optionsMenuSelected;
        [SerializeField] private Selectable difficultySettingsMenuSelected;

        private bool inMenu = false;
        [SerializeField] private string sceneToLoad;

        private PlayerControlSystem playerControlSystem;

        private void Awake()
        {
            playerControlSystem = new PlayerControlSystem();
            playerControlSystem.UI.Start.performed += _ => StartScreenToMain();
            playerControlSystem.UI.Cancel.performed += _ => CancelAction();
            playerControlSystem.UI.DebugMode.performed += _ => DebugMode();
        }

        // Start is called before the first frame update
        void Start()
        {
            GameManager.Instance.AudioManager.Play("MainMenuAmbience");
            GameManager.Instance.AudioManager.Play("MainMenuWindAmbience");

            currentMenuState = startMenu;

            if (GameSettings.mainMenuEntered)
            {
                inMenu = true;
                GoToMain();
            }
            else
            {
                LevelTransition.Instance?.EndTransition(1f, LevelTransition.LevelTransitionType.FADE);
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

        public void StartGame()
        {
            GameManager.Instance.AudioManager.Stop("MainMenuAmbience");
            GameManager.Instance.AudioManager.Stop("MainMenuWindAmbience");
            GameManager.Instance.LoadScene(sceneToLoad, LevelTransition.LevelTransitionType.FADE);
        }

        public void ShowDifficulty()
        {
            SwitchMenu(MenuState.DIFFICULTYSETTINGS);
            difficultySettingsMenuSelected.Select();
        }

        public void ShowDifficultyText(GameObject difficultyText)
        {
            difficultyText.SetActive(true);
            PlayButtonSFX("Hover");
        }

        public void HideDifficultyText(GameObject difficultyText)
        {
            difficultyText.SetActive(false);
        }

        public void SetDifficulty(float difficulty)
        {
            GameSettings.difficulty = difficulty;
            StartGame();
        }

        private void StartScreenToMain()
        {
            if (!inMenu)
            {
                inMenu = true;
                GameSettings.mainMenuEntered = true;
                PlayButtonSFX("Click");
                GameManager.Instance.MultiplayerManager.SetUIControlScheme();
                StartCoroutine(WaitStartScreenToMain());
            }
        }

        private IEnumerator WaitStartScreenToMain()
        {
            startScreenAnimator.Play();
            yield return new WaitForSeconds(startScreenAnimator.duration);
            GoToMain();
            mainMenuAnimator.Play();
        }

        private void CancelAction()
        {
            //If the player is not in the main menu, go back to the main menu
            if (currentMenuState != mainMenu && currentMenuState != startMenu)
            {
                Back();
            }
            //If they are in the main menu, go back to start
            else
            {
                SwitchMenu(MenuState.START);
                inMenu = false;
            }

            PlayButtonSFX("Cancel");
        }

        private void DebugMode()
        {
            if (!inMenu && !GameSettings.debugMode)
            {
                GameSettings.debugMode = true;
                DontDestroyOnLoad(Instantiate(Resources.Load("DebugCanvas")));
                GameManager.Instance.AudioManager.Play("DebugBeep");
            }
        }

        public void GoToMain()
        {
            GameManager.Instance.MultiplayerManager.SwitchAllPlayerActionMaps("UI");
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
                case MenuState.START:
                    newMenu = startMenu;
                    break;
                case MenuState.MAIN:
                    newMenu = mainMenu;
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
                default:
                    newMenu = mainMenu;
                    break;
            }

            GameObject prevMenu = currentMenuState;

            DeselectButton();

            currentMenuState.SetActive(false);
            currentMenuState = newMenu;
            currentMenuState.SetActive(true);

            if (menu == MenuState.MAIN)
                SelectButtonOnMain(prevMenu);
        }

        private void DeselectButton()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void SelectButtonOnMain(GameObject menu)
        {
            foreach (var button in mainMenuButtons)
            {
                button.GetComponent<Animator>().Play("ButtonDeselectAni");
                button.GetComponent<Animator>().CrossFade("ButtonDeselectAni", 0f, 0, 1f);
            }

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

                yield return null;
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

                yield return null;
            }

            rectTransform.localPosition = startPos;
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

        public void PlayButtonSFX(string name)
        {
            GameManager.Instance.AudioManager.Play("Button" + name);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
