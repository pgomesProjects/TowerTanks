using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerTanks.Scripts
{
    public class TutorialPopupController : MonoBehaviour
    {
        [SerializeField, Tooltip("The tutorial image component.")] private Image tutorialImage;
        [SerializeField, Tooltip("The tutorial text component.")] private TextMeshProUGUI tutorialText;
        [SerializeField, Tooltip("The tutorial text component.")] private TextMeshProUGUI advanceTutorialText;
        [SerializeField, Tooltip("The current tutorial settings to display.")] private TutorialPopupSettings currentTutorial;

        private bool isTutorialActive;
        private bool canEndTutorial;
        private int currentPageNumber;

        private PlayerControlSystem playerControls;

        private void Awake()
        {
            playerControls = new PlayerControlSystem();
            playerControls.Player.AdvanceTutorialText.performed += _ => AdvanceTutorial();

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

        public void StartTutorial(TutorialPopupSettings newTutorial)
        {
            currentTutorial = newTutorial;
            ActivateTutorial();
        }

        private void ActivateTutorial()
        {
            if (currentTutorial == null)
                return;

            isTutorialActive = currentTutorial.tutorialPages.Length > 0;
            GameManager.Instance.tutorialWindowActive = isTutorialActive;
            if (!isTutorialActive)
            {
                gameObject.SetActive(false);
                return;
            }

            if(!GameManager.Instance.InGameMenu)
                Time.timeScale = 0f;
            gameObject.SetActive(true);
            ShowTutorialPage(0);
        }

        private void DeactivateTutorial()
        {
            if (!GameManager.Instance.InGameMenu)
                Time.timeScale = 1f;
            GameManager.Instance.tutorialWindowActive = false;
            Destroy(gameObject);
        }

        private void ShowTutorialPage(int pageNumber)
        {
            currentPageNumber = Mathf.Clamp(pageNumber, 0, currentTutorial.tutorialPages.Length - 1);

            Debug.Log("Showing Page " + currentPageNumber);

            bool isLastPage = currentPageNumber >= currentTutorial.tutorialPages.Length - 1;
            bool isFirstPage = currentPageNumber <= 0 && !isLastPage;

            Sprite tutorialSprite = currentTutorial.tutorialPages[currentPageNumber].tutorialImage;

            if(tutorialSprite == null)
                tutorialImage.color = new Color(1, 1, 1, 0);
            else
            {
                tutorialImage.color = new Color(1, 1, 1, 1);
                tutorialImage.sprite = tutorialSprite;
            }

            tutorialText.text = currentTutorial.tutorialPages[currentPageNumber].tutorialText;

            advanceTutorialText.text = isLastPage ? "Close" : "Continue";
            canEndTutorial = isLastPage;
        }

        private void AdvanceTutorial()
        {
            if (canEndTutorial)
                DeactivateTutorial();
            else
                ShowTutorialPage(currentPageNumber + 1);
        }
    }
}
