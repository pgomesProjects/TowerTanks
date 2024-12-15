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
        private GameObject currentMenuState;
        public enum MenuState { PAUSE, TUTORIALS, OPTIONS, REFRESH }

        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject tutorialsMenu;
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
            currentMenuState = pauseMenu;
        }

        private void OnEnable()
        {
            playerControlSystem.Enable();
            currentMenuState = pauseMenu;
            SwitchMenu(MenuState.REFRESH);
            GameManager.Instance.isPaused = true;
        }

        private void OnDisable()
        {
            playerControlSystem.Disable();
            GameManager.Instance.isPaused = false;
        }

        public void UpdatePausedPlayer(int playerIndex)
        {
            pauseText.text = "Player " + (playerIndex + 1) + " Paused";
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
            if (currentMenuState != pauseMenu)
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

        private void SwitchMenu(MenuState menu)
        {
            GameObject newMenu;

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
            if (menu == tutorialsMenu)
            {
                pauseMenuButtons[1].Select();
            }
            else if (menu == optionsMenu)
            {
                pauseMenuButtons[2].Select();
            }
            else
            {
                pauseMenuButtons[0].Select();
            }
        }

        public void Resume()
        {
            EventSystem.current.SetSelectedGameObject(null);
            LevelManager.Instance?.PauseToggle(currentPlayerPaused);
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

        public void ReturnToMain()
        {
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
