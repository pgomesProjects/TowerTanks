using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace TowerTanks.Scripts
{
    public class PauseController : MonoBehaviour
    {
        public static PauseController Instance;

        public enum MenuState { PAUSE, TUTORIALS, OPTIONS, RETURN }
        private MenuState currentMenuState;
        private GameObject currentMenuGameObject;

        [SerializeField, Tooltip("The master pause menu container.")] private GameObject masterPauseMenuContainer;
        [Space()]
        [SerializeField] private TextMeshProUGUI pauseText;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject tutorialsMenu;
        [SerializeField] private GameObject optionsMenu;
        [SerializeField] private ConfirmationWindow returnToMainConfirmationWindow;

        [Header("Menu First Selected Items")]
        [SerializeField] private Selectable[] pauseMenuButtons;
        [SerializeField] private Selectable optionsMenuSelected;

        private int currentPlayerPaused;
        private PlayerControlSystem playerControlSystem;

        public static System.Action<int> OnGamePaused;
        public static System.Action OnGameResumed;

        private void Awake()
        {
            Instance = this;
            playerControlSystem = new PlayerControlSystem();
            playerControlSystem.UI.Cancel.performed += _ => CancelAction();
            currentMenuState = MenuState.PAUSE;
            currentPlayerPaused = -1;
        }

        public void PauseToggle(int playerIndex)
        {
            Debug.Log("Womp");

            //If the game is not paused, pause the game
            if (!GameManager.Instance.isPaused)
            {
                Time.timeScale = 0;
                GameManager.Instance.AudioManager.PauseAllSounds();
                currentPlayerPaused = playerIndex;
                OnEnablePauseMenu();
                OnGamePaused?.Invoke(playerIndex);
            }
            //If the game is paused, resume the game if the person that paused the game unpauses
            else if (GameManager.Instance.isPaused && playerIndex == currentPlayerPaused)
            {
                GameManager.Instance.UnpauseFrames(4);
                GameManager.Instance.AudioManager.ResumeAllSounds();
                currentPlayerPaused = -1;
                OnDisablePauseMenu();
                OnGameResumed?.Invoke();
            }
        }

        private void OnEnablePauseMenu()
        {
            playerControlSystem.Enable();
            masterPauseMenuContainer.SetActive(true);
            UpdatePausedPlayer(currentPlayerPaused);
            SwitchMenu(MenuState.PAUSE);
            currentMenuGameObject = pauseMenu;
            GameManager.Instance.isPaused = true;
        }

        private void OnDisablePauseMenu()
        {
            playerControlSystem.Disable();
            ReactivateAllPlayerInput();
            masterPauseMenuContainer.SetActive(false);
            GameManager.Instance.isPaused = false;
        }

        public void UpdatePausedPlayer(int playerIndex)
        {
            pauseText.text = (playerIndex >= 0 ? "Player " + (playerIndex + 1) + " " : "") + "Paused";
            currentPlayerPaused = playerIndex;

            foreach (var player in FindObjectsOfType<PlayerMovement>())
            {
                PlayerInput playerInput = player.GetPlayerData().playerInput;
                if (playerInput.playerIndex != currentPlayerPaused)
                {
                    //Disable other player input
                    playerInput.actions.Disable();
                }
                else
                {
                    //Make sure the current player's action asset is tied to the EventSystem so they can use the menus
                    EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset = playerInput.actions;
                }
            }
        }

        public void ReactivateAllPlayerInput()
        {
            foreach (var player in FindObjectsOfType<PlayerMovement>())
            {
                PlayerInput playerInput = player.GetPlayerData().playerInput;
                //Activates all of the input for the players that aren't the one that's already enabled
                if (playerInput.playerIndex != currentPlayerPaused)
                    playerInput.actions.Enable();
            }
        }

        private void CancelAction()
        {
            if (currentMenuState != MenuState.PAUSE)
            {
                Debug.Log("Back...");
                Back();
            }
            else
            {
                Debug.Log("Resume...");
                Resume();
            }
            PlayButtonSFX("Cancel");
        }

        /// <summary>
        /// Switches the menu to the next menu state.
        /// </summary>
        /// <param name="menu">The pause menu state to switch to.</param>
        private void SwitchMenu(MenuState menu)
        {
            GameObject newMenu;

            //Set the new menu based on the menu state given
            switch (menu)
            {
                case MenuState.PAUSE:
                    newMenu = pauseMenu;
                    break;
                case MenuState.TUTORIALS:
                    newMenu = tutorialsMenu;
                    break;
                case MenuState.OPTIONS:
                    newMenu = optionsMenu;
                    break;
                case MenuState.RETURN:
                    newMenu = returnToMainConfirmationWindow.gameObject;
                    break;
                default:
                    newMenu = pauseMenu;
                    break;
            }

            //Disable the previous menu, if applicable
            EventSystem.current.SetSelectedGameObject(null);
            currentMenuGameObject?.SetActive(false);

            //Activate the new menu
            currentMenuGameObject = newMenu;
            currentMenuGameObject.SetActive(true);

            //Select the corresponding pause menu button in the menu based on the previous menu
            if (menu == MenuState.PAUSE)
                StartCoroutine(SelectPauseMenuButton((int)currentMenuState));

            currentMenuState = menu;
        }

        private IEnumerator SelectPauseMenuButton(int menuState)
        {
            yield return null;
            pauseMenuButtons[menuState].Select();
        }

        public void Resume()
        {
            EventSystem.current.SetSelectedGameObject(null);
            PauseToggle(currentPlayerPaused);
        }

        public void Tutorials()
        {
            SwitchMenu(MenuState.TUTORIALS);
        }

        public void Options()
        {
            SwitchMenu(MenuState.OPTIONS);
            optionsMenuSelected.Select();
        }

        public void ConfirmReturnToMain()
        {
            SwitchMenu(MenuState.RETURN);
            returnToMainConfirmationWindow.Init("Are you sure you want to exit to the main menu?<br><br>(All unsaved progress will be lost.)", ReturnToMain, Back);
        }

        public void ReturnToMain()
        {
            Debug.Log("Returning to Main Menu...");
            GameManager.Instance.LoadScene("Title", LevelTransition.LevelTransitionType.FADE, false, true, true);
            Time.timeScale = 1.0f;
            GameManager.Instance.AudioManager.StopAllSounds();
        }

        public void PlayButtonSFX(string name)
        {
            GameManager.Instance.AudioManager.Play("Button" + name);
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
}
