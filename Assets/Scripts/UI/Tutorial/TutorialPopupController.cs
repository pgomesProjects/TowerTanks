using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

namespace TowerTanks.Scripts
{
    public class TutorialPopupController : MonoBehaviour
    {
        [Header("Tutorial Popup Settings")]
        [SerializeField, Tooltip("The tutorial header component.")] private TextMeshProUGUI tutorialHeader;
        [SerializeField, Tooltip("The tutorial image component.")] private Image tutorialImage;
        [SerializeField, Tooltip("The tutorial text component.")] private TextMeshProUGUI tutorialText;
        [SerializeField, Tooltip("The advance tutorial component.")] private TextMeshProUGUI advanceTutorialText;
        [SerializeField, Tooltip("The advance tutorial task bar.")] private TaskProgressBar advanceTaskBar;
        [SerializeField, Tooltip("The duration to hold the advance button for.")] private float advanceTutorialDuration;
        [SerializeField, Tooltip("The current tutorial settings to display.")] private TutorialPopupSettings currentTutorial;
        [SerializeField, Tooltip("The container for the advance prompt.")] private RectTransform promptContainer;
        [Space()]
        [Header("Tutorial Animation Settings")]
        [SerializeField, Tooltip("The canvas group for the tutorial window.")] private CanvasGroup tutorialWindowCanvasGroup;
        [Space()]
        [Header("Open Animation Settings")]
        [SerializeField, Tooltip("The distance for the window to move into view.")] private float windowXDistanceIn;
        [SerializeField, Tooltip("The duration of the tutorial open animation.")] private float tutorialWindowInDuration;
        [SerializeField, Tooltip("The LeanTween ease type of the tutorial window open animation.")] private LeanTweenType tutorialWindowInEaseType;
        [Space()]
        [Header("Close Animation Settings")]
        [SerializeField, Tooltip("The distance for the window to move out of view.")] private float windowXDistanceOut;
        [SerializeField, Tooltip("The duration of the tutorial close animation.")] private float tutorialWindowOutDuration;
        [SerializeField, Tooltip("The LeanTween ease type of the tutorial window close animation.")] private LeanTweenType tutorialWindowOutEaseType;

        private bool isTutorialActive;
        private bool canEndTutorial;
        private int currentPageNumber;

        private bool isTutorialAdvanceStarted;
        private float currentAdvanceTimer;

        private PlayerControlSystem playerControls;

        public static System.Action OnTutorialStarted;
        public static System.Action OnTutorialEnded;

        private void Awake()
        {
            playerControls = new PlayerControlSystem();
            playerControls.Player.AdvanceTutorialText.started += _ => StartAdvance();
            playerControls.Player.AdvanceTutorialText.canceled += _ => CancelAdvance();

            GameManager.Instance.UIManager.AddButtonPrompt(promptContainer.gameObject, Vector2.zero, 50f, GameAction.AdvanceTutorial, PlatformType.Gamepad, GameUIManager.PromptDisplayType.Button, false);

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
        /// <param name="newTutorial">A reference to the tutorial information.</param>
        /// <param name="overrideViewedInGame">If true, the tutorial can be viewed even if it has already been viewed.</param>
        public void StartTutorial(ref TutorialItem newTutorial, bool overrideViewedInGame)
        {
            //If the tutorial has been viewed and not overridden, destroy and return
            if (GameManager.Instance.tutorialWindowActive || (newTutorial.hasBeenViewedInGame && !overrideViewedInGame))
            {
                Destroy(gameObject);
                return;
            }

            newTutorial.hasBeenViewedInGame = true;
            currentTutorial = newTutorial.tutorialPopup;
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
            PlayTutorialOpenAnimation();
            OnTutorialStarted?.Invoke();
        }

        private void PlayTutorialOpenAnimation()
        {
            tutorialWindowCanvasGroup.alpha = 0f;
            RectTransform tutorialRectTransform = tutorialWindowCanvasGroup.GetComponent<RectTransform>();
            Vector2 currentTutorialPos = tutorialRectTransform.anchoredPosition;
            currentTutorialPos.x = windowXDistanceIn;
            tutorialRectTransform.anchoredPosition = currentTutorialPos;

            LeanTween.alphaCanvas(tutorialWindowCanvasGroup, 1f, tutorialWindowInDuration).setEase(tutorialWindowInEaseType).setIgnoreTimeScale(true);
            LeanTween.moveX(tutorialRectTransform, 0f, tutorialWindowInDuration).setEase(tutorialWindowInEaseType).setIgnoreTimeScale(true);
        }

        private void DeactivateTutorial()
        {
            if (!GameManager.Instance.isPaused)
                GameManager.Instance.UnpauseFrames(4);

            GameManager.Instance.tutorialWindowActive = false;
            PlayTutorialCloseAnimation();
            OnTutorialEnded?.Invoke();
        }

        private void PlayTutorialCloseAnimation()
        {
            tutorialWindowCanvasGroup.alpha = 1f;
            RectTransform tutorialRectTransform = tutorialWindowCanvasGroup.GetComponent<RectTransform>();

            LeanTween.alphaCanvas(tutorialWindowCanvasGroup, 0f, tutorialWindowOutDuration).setEase(tutorialWindowOutEaseType).setIgnoreTimeScale(true);
            LeanTween.moveX(tutorialRectTransform, windowXDistanceOut, tutorialWindowOutDuration).setEase(tutorialWindowOutEaseType).setIgnoreTimeScale(true).setOnComplete(() => Destroy(gameObject));
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

            //Display the stored tutorial text with any actions replaced with sprites
            string tutText = ExtractActions(currentTutorial.tutorialPages[currentPageNumber].tutorialText, bracketFunction => 
            {
                return DisplayAction(bracketFunction);
            });
            tutorialText.text = tutText;

            //Show "Close" or "Continue" based on whether the current page is the last one or not
            advanceTutorialText.text = isLastPage ? "Close" : "Continue";
            canEndTutorial = isLastPage;
        }

        /// <summary>
        /// Extracts all of the actions from the string and displays their sprites in the text.
        /// </summary>
        /// <param name="input">The tutorial text.</param>
        /// <param name="bracketFunction">The function which takes the action and converts it into a sprite.</param>
        /// <returns>Returns the string with the bracketed actions converted into appropriate sprites.</returns>
        private string ExtractActions(string input, System.Func<string, string> bracketFunction)
        {
            // Define the regex pattern to match content inside square brackets
            string pattern = @"\[(.*?)\]";

            // Replace matches using a callback function
            string result = Regex.Replace(input, pattern, match =>
            {
                //Get the action from the brackets
                string phrase = match.Groups[1].Value;
                //Take the func parameter and use it to alter the result 
                return bracketFunction(phrase);
            });


            //Return the altered string
            return result;
        }

        /// <summary>
        /// Takes an action name and returns the sprite equivalent of it.
        /// </summary>
        /// <param name="action">The action to display.</param>
        /// <returns>Returns the appropriate sprite text if found. Returns an empty string if not found.</returns>
        private string DisplayAction(string action)
        {
            string actionSprite = string.Empty;

            //If the current action exists as a game action, get the action
            if (System.Enum.TryParse(action, true, out GameAction result))
            {
                PlatformPrompt promptInfo = GameManager.Instance.buttonPromptSettings.GetPlatformPrompt(result, GameSettings.gamePlatform);

                //If the prompt exists in the system, get the sprite id from the prompt and show it in the text
                if (promptInfo != null)
                    actionSprite = "<sprite index=" + promptInfo.SpriteID + ">";
            }

            return actionSprite;
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
