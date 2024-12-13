using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerTanks.Scripts
{
    public class TutorialPopupController : MonoBehaviour
    {
        [SerializeField, Tooltip("The tutorial header component.")] private TextMeshProUGUI tutorialHeader;
        [SerializeField, Tooltip("The tutorial image component.")] private Image tutorialImage;
        [SerializeField, Tooltip("The tutorial text component.")] private TextMeshProUGUI tutorialText;
        [SerializeField, Tooltip("The advance tutorial component.")] private TextMeshProUGUI advanceTutorialText;
        [SerializeField, Tooltip("The advance tutorial task bar.")] private TaskProgressBar advanceTaskBar;
        [SerializeField, Tooltip("The duration to hold the advance button for.")] private float advanceTutorialDuration;
        [SerializeField, Tooltip("The current tutorial settings to display.")] private TutorialPopupSettings currentTutorial;

        private bool isTutorialActive;
        private bool canEndTutorial;
        private int currentPageNumber;

        private bool isTutorialAdvanceStarted;
        private float currentAdvanceTimer;

        private PlayerControlSystem playerControls;

        private void Awake()
        {
            playerControls = new PlayerControlSystem();
            playerControls.Player.AdvanceTutorialText.started += _ => StartAdvance();
            playerControls.Player.AdvanceTutorialText.canceled += _ => CancelAdvance();

            ActivateTutorial();
        }

        private void OnEnable()
        {
            playerControls?.Enable();
        }

        private void OnDisable()
        {
            playerControls?.Disable();
        }

        /// <summary>
        /// Starts a tutorial using information provided.
        /// </summary>
        /// <param name="newTutorial">The tutorial information.</param>
        public void StartTutorial(TutorialPopupSettings newTutorial)
        {
            currentTutorial = newTutorial;
            ActivateTutorial();
        }

        private void ActivateTutorial()
        {
            //If there is no tutorial, return
            if (currentTutorial == null)
                return;

            //If there are no pages in the tutorial, also return
            isTutorialActive = currentTutorial.tutorialPages.Length > 0;

            if (!GameManager.Instance.InGameMenu && isTutorialActive)
                Time.timeScale = 0f;
            GameManager.Instance.tutorialWindowActive = isTutorialActive;

            if (!isTutorialActive)
            {
                gameObject.SetActive(false);
                return;
            }

            //Set active and show the first page
            gameObject.SetActive(true);
            advanceTaskBar.gameObject.SetActive(false);
            tutorialHeader.text = currentTutorial.header;
            ShowTutorialPage(0);
        }

        private void DeactivateTutorial()
        {
            Time.timeScale = 1f;
            GameManager.Instance.tutorialWindowActive = false;
            Destroy(gameObject);
        }

        /// <summary>
        /// Show one of the pages of the stored tutorial.
        /// </summary>
        /// <param name="pageNumber">The page number to show from the stored tutorial.</param>
        private void ShowTutorialPage(int pageNumber)
        {
            //Clamp the page number to ensure that it does not go out of bounds
            currentPageNumber = Mathf.Clamp(pageNumber, 0, currentTutorial.tutorialPages.Length - 1);

            bool isLastPage = currentPageNumber >= currentTutorial.tutorialPages.Length - 1;
            bool isFirstPage = currentPageNumber <= 0 && !isLastPage;

            Sprite tutorialSprite = currentTutorial.tutorialPages[currentPageNumber].tutorialImage;

            //If there is no tutorial sprite, hide the image
            if(tutorialSprite == null)
                tutorialImage.color = new Color(1, 1, 1, 0);
            else
            {
                tutorialImage.color = new Color(1, 1, 1, 1);
                tutorialImage.sprite = tutorialSprite;
            }

            //Display the stored tutorial text
            tutorialText.text = currentTutorial.tutorialPages[currentPageNumber].tutorialText;

            //Show "Close" or "Continue" based on whether the current page is the last one or not
            advanceTutorialText.text = isLastPage ? "Close" : "Continue";
            canEndTutorial = isLastPage;
        }

        private void StartAdvance()
        {
            if (isTutorialAdvanceStarted)
                return;

            isTutorialAdvanceStarted = true;
            currentAdvanceTimer = 0f;
            advanceTaskBar.gameObject.SetActive(true);
        }

        private void AdvanceTutorial()
        {
            //If the tutorial can be ended, end it
            if (canEndTutorial)
                DeactivateTutorial();

            //If not, go to the next page
            else
                ShowTutorialPage(currentPageNumber + 1);

            isTutorialAdvanceStarted = false;
            currentAdvanceTimer = 0f;
            advanceTaskBar.gameObject.SetActive(false);
        }

        private void CancelAdvance()
        {
            if (!isTutorialAdvanceStarted)
                return;

            isTutorialAdvanceStarted = false;
            currentAdvanceTimer = 0f;
            advanceTaskBar.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isTutorialAdvanceStarted)
            {
                currentAdvanceTimer += Time.unscaledDeltaTime;

                //If the timer has been hit, advance to the next tutorial
                if (currentAdvanceTimer >= advanceTutorialDuration)
                    AdvanceTutorial();

                //Otherwise, show the task bar updating
                else
                    advanceTaskBar.UpdateProgressValue(currentAdvanceTimer / advanceTutorialDuration);
            }
        }
    }
}
