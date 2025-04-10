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
        private GameObject currentMenuStateObject;
        private MenuState currentMenuState;

        public enum MenuState { START, MAIN, OPTIONS, CREDITS, DIFFICULTYSETTINGS }

        [SerializeField] private GameObject startMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject optionsMenu;
        [SerializeField] private GameObject creditsMenu;
        [SerializeField] private GameObject difficultySettingsMenu;
        [Space()]
        [Header("Idle Screen Settings")]
        [SerializeField, Tooltip("The main menu canvas group.")] private CanvasGroup mainMenuCanvasGroup;
        [SerializeField, Tooltip("The idle canvas group.")] private CanvasGroup idleCanvasCroup;
        [SerializeField, Tooltip("The duration of the fade into the idle menu.")] private float idleFadeInDuration;
        [SerializeField, Tooltip("The duration of the fade out of the idle menu.")] private float idleFadeOutDuration;
        [Space()]
        [Header("Menu Animators")]
        [SerializeField] private MoveAnimation startScreenAnimator;
        [SerializeField] private MoveAnimation mainMenuAnimator;
        [Space()]
        [SerializeField, Tooltip("The animator for the title screen transition.")] private Animator titleScreenTransitionAnimator;
        [SerializeField, Tooltip("The duration of the fade out of the main menu when transitioning out.")] private float mainMenuExitDuration;
        [Space()]
        [Header("Menu First Selected Items")]
        [SerializeField] private Selectable[] mainMenuButtons;
        [SerializeField] private Selectable optionsMenuSelected;
        [SerializeField] private Selectable difficultySettingsMenuSelected;

        private bool inMenu = false;
        [SerializeField] private string sceneToLoad;

        private bool idleMenuAnimationActive;
        private float startingMenuAlpha, endingMenuAlpha;
        private float startingIdleAlpha, endingIdleAlpha;
        private float idleAnimationElapsed;
        private float idleAnimationDuration;

        private bool mainMenuTransitionActive;
        private float mainMenuExitElapsed;

        private PlayerControlSystem playerControlSystem;

        private void Awake()
        {
            playerControlSystem = new PlayerControlSystem();
            playerControlSystem.UI.Cancel.performed += _ => CancelAction();
            playerControlSystem.UI.DebugMode.performed += _ => DebugMode();
        }

        // Start is called before the first frame update
        void Start()
        {
            GameManager.Instance.AudioManager.Play("MainMenuAmbience");
            GameManager.Instance.AudioManager.Play("MainMenuWindAmbience");

            currentMenuState = MenuState.START;
            currentMenuStateObject = startMenu;

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
            IdleUITracker.OnIdleScreenActivated += () => ShowIdleScreen(true);
            IdleUITracker.OnIdleEnd += () => ShowIdleScreen(false);
            AnyButtonPressDetection.OnAnyButtonPressed += OnMenuAction;
        }

        private void OnDisable()
        {
            playerControlSystem.Disable();
            IdleUITracker.OnIdleScreenActivated -= () => ShowIdleScreen(true);
            IdleUITracker.OnIdleEnd -= () => ShowIdleScreen(false);
            AnyButtonPressDetection.OnAnyButtonPressed -= OnMenuAction;
        }

        public void StartGame()
        {
            mainMenuTransitionActive = true;
            GameManager.Instance.AudioManager.Stop("MainMenuAmbience");
            GameManager.Instance.AudioManager.Stop("MainMenuWindAmbience");
            mainMenuCanvasGroup.interactable = false;
            mainMenuExitElapsed = 0f;
            titleScreenTransitionAnimator.Play("ZoomIn&Pan");
        }

        private void MainMenuFadeAnimation()
        {
            if (mainMenuTransitionActive)
            {
                if (mainMenuExitElapsed < mainMenuExitDuration)
                {
                    mainMenuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, mainMenuExitElapsed / mainMenuExitDuration);
                    mainMenuExitElapsed += Time.deltaTime;
                }
                else
                {
                    mainMenuCanvasGroup.alpha = 0f;
                    mainMenuTransitionActive = false;
                }
            }
        }

        public void LoadNextScene()
        {
            GameManager.Instance.LoadScene(sceneToLoad, LevelTransition.LevelTransitionType.FADE, true, true, false);
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

        public void OnMenuAction(InputControl control)
        {
            switch (currentMenuState)
            {
                case MenuState.START:
                    StartScreenToMain();
                    break;
                case MenuState.MAIN:
                    IdleUITracker.InputRecorded = true;
                    break;
            }
        }

        private void ShowIdleScreen(bool showScreen)
        {
            //Set the animation variables
            startingMenuAlpha = showScreen ? 1f : 0f;
            endingMenuAlpha = showScreen ? 0f : 1f;
            startingIdleAlpha = showScreen ? 0f : 1f;
            endingIdleAlpha = showScreen ? 1f : 0f;
            idleAnimationDuration = showScreen ? idleFadeInDuration : idleFadeOutDuration;
            idleAnimationElapsed = 0f;

            idleMenuAnimationActive = true;
        }

        private void IdleScreenAnimation()
        {
            if (idleMenuAnimationActive)
            {
                //Lerp the alpha of the canvas groups
                if(idleAnimationElapsed < idleAnimationDuration)
                {
                    mainMenuCanvasGroup.alpha = Mathf.Lerp(startingMenuAlpha, endingMenuAlpha, idleAnimationElapsed / idleAnimationDuration);
                    idleCanvasCroup.alpha = Mathf.Lerp(startingIdleAlpha, endingIdleAlpha, idleAnimationElapsed / idleAnimationDuration);
                    idleAnimationElapsed += Time.deltaTime;
                }
                else
                {
                    mainMenuCanvasGroup.alpha = endingMenuAlpha;
                    idleCanvasCroup.alpha = endingIdleAlpha;
                    idleMenuAnimationActive = false;
                }
            }
        }

        private void CancelAction()
        {
            //If the player is not in the main menu, go back to the main menu
            if (currentMenuStateObject != mainMenu && currentMenuStateObject != startMenu)
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

            GameObject prevMenu = currentMenuStateObject;

            DeselectButton();

            currentMenuStateObject.SetActive(false);
            currentMenuStateObject = newMenu;
            currentMenuStateObject.SetActive(true);

            if (menu == MenuState.MAIN)
                SelectButtonOnMain(prevMenu);

            currentMenuState = menu;
        }

        private void Update()
        {
            IdleScreenAnimation();
            MainMenuFadeAnimation();
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
