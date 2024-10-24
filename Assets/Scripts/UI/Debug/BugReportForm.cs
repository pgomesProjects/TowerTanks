using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace TowerTanks.Scripts
{
    [RequireComponent(typeof(BugReportSubmissionManager))]
    public class BugReportForm : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField, Tooltip("The main bug report screen.")] private RectTransform mainScreen;
        [SerializeField, Tooltip("The RectTransform that holds the bug report form.")] private RectTransform bugReportRectTransform;
        [SerializeField, Tooltip("The RectTransform that holds the bug report submitting screen.")] private RectTransform bugSubmittingRectTransform;
        [SerializeField, Tooltip("The RectTransform that holds the bug report success screen.")] private RectTransform reportSuccessRectTransform;
        [SerializeField, Tooltip("The RectTransform that holds the bug report error screen.")] private RectTransform reportErrorRectTransform;
        [Space]
        [Header("Form Details")]
        [SerializeField, Tooltip("The input field for the bug report title.")] private TMP_InputField titleField;
        [SerializeField, Tooltip("The dropdown for the bug report severity.")] private TMP_Dropdown severityField;
        [SerializeField, Tooltip("The input field for the bug report description.")] private TMP_InputField descriptionField;
        [SerializeField, Tooltip("The toggle for attaching a screenshot.")] private Toggle screenshotToggle;
        [Space]
        [SerializeField, Tooltip("The text field that shows the bug report ID.")] private TextMeshProUGUI bugReportIDText;
        [SerializeField, Tooltip("The text that shows that the report ID has been copied to the clipboard.")] private TextMeshProUGUI reportIDCopiedText;
        [Space]
        [SerializeField, Tooltip("The button to copy the report ID to the clipboard.")] private Button copyToClipboardButton;
        [SerializeField, Tooltip("The button to try the report again.")] private Button tryAgainButton;

        private enum BugReportState { Form, Submitting, Success, Fail };

        private Texture2D currentScreenshot;
        BugReportSubmissionManager submissionManager;

        private RectTransform currentActiveTransform;
        private Selectable currentSelectable;

        private PlayerControlSystem playerControls;

        private void Awake()
        {
            submissionManager = GetComponent<BugReportSubmissionManager>();
            mainScreen.gameObject.SetActive(false);
            playerControls = new PlayerControlSystem();
            playerControls.Debug.Tab.performed += _ => NavigateForm();

        }

        private void OnEnable()
        {
            BugReportSubmissionManager.OnBugReportSubmitted += BugReportSubmitted;
            BugReportSubmissionManager.OnBugReportFailed += BugReportFailed;
            StartCoroutine(OpenForm());
            playerControls?.Enable();
        }

        private void OnDisable()
        {
            BugReportSubmissionManager.OnBugReportSubmitted -= BugReportSubmitted;
            BugReportSubmissionManager.OnBugReportFailed -= BugReportFailed;
            Time.timeScale = 1f;
            currentActiveTransform.gameObject.SetActive(false);
            mainScreen.gameObject.SetActive(false);
            playerControls?.Disable();
        }

        /// <summary>
        /// Gets a screenshot of the game, and then opens the form.
        /// </summary>
        /// <returns></returns>
        private IEnumerator OpenForm()
        {
            //Attempt to get a screenshot of the game
            yield return new WaitForEndOfFrame();
            currentScreenshot = ScreenCapture.CaptureScreenshotAsTexture();

            mainScreen.gameObject.SetActive(true);
            currentActiveTransform = null;
            SwitchFormScreen(BugReportState.Form);
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Switches to a specific form screen.
        /// </summary>
        /// <param name="bugReportState">The state the form is currently at.</param>
        private void SwitchFormScreen(BugReportState bugReportState)
        {
            if (currentActiveTransform != null)
                currentActiveTransform.gameObject.SetActive(false);

            switch (bugReportState)
            {
                case BugReportState.Form:
                    bugReportRectTransform.gameObject.SetActive(true);
                    currentActiveTransform = bugReportRectTransform;
                    titleField.Select();
                    titleField.ActivateInputField();
                    currentSelectable = titleField;
                    break;
                case BugReportState.Submitting:
                    bugSubmittingRectTransform.gameObject.SetActive(true);
                    currentActiveTransform = bugSubmittingRectTransform;
                    break;
                case BugReportState.Success:
                    reportSuccessRectTransform.gameObject.SetActive(true);
                    currentActiveTransform = reportSuccessRectTransform;
                    copyToClipboardButton.Select();
                    break;
                case BugReportState.Fail:
                    reportErrorRectTransform.gameObject.SetActive(true);
                    currentActiveTransform = reportErrorRectTransform;
                    tryAgainButton.Select();
                    break;
            }
        }

        /// <summary>
        /// Navigates the form by moving down whenever the tab input is performed.
        /// </summary>
        private void NavigateForm()
        {
            if (currentSelectable != null)
            {
                // Find the next selectable in the down direction
                Selectable nextSelectable = currentSelectable.FindSelectableOnDown();

                if (nextSelectable != null)
                {
                    EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
                    currentSelectable = nextSelectable;
                }
            }
        }

        /// <summary>
        /// Submits a bug report using the information in the form.
        /// </summary>
        public void SubmitBugReport()
        {
            string title = titleField.text;
            string desc = descriptionField.text;
            int severity = severityField.value;

            bool attachScreenshot = screenshotToggle.isOn;

            submissionManager.SubmitBugReport(new BugReportInfo(title, desc, 3 - severity), attachScreenshot ? currentScreenshot : null);
            SwitchFormScreen(BugReportState.Submitting);
        }

        /// <summary>
        /// Copies the report ID to the user's clipboard.
        /// </summary>
        public void CopyReportID()
        {
            GameSettings.CopyToClipboard(bugReportIDText.text);
            reportIDCopiedText.gameObject.SetActive(true);
        }

        /// <summary>
        /// Gets the report ID from the bug report submission system and switches to the success screen.
        /// </summary>
        /// <param name="reportID">The report ID created.</param>
        private void BugReportSubmitted(string reportID)
        {
            bugReportIDText.text = reportID;
            SwitchFormScreen(BugReportState.Success);
            reportIDCopiedText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Switches to the bug report failed screen.
        /// </summary>
        private void BugReportFailed()
        {
            SwitchFormScreen(BugReportState.Fail);
        }

        /// <summary>
        /// Switches back to the form for the user to try again.
        /// </summary>
        public void TryAgain()
        {
            SwitchFormScreen(BugReportState.Form);
        }
    }
}
