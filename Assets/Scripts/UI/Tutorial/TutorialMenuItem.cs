using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TowerTanks.Scripts
{
    [RequireComponent(typeof(Selectable))]
    public class TutorialMenuItem : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField, Tooltip("The text of the button.")] private TextMeshProUGUI tutorialButtonText;

        private bool isSelected;
        private int tutorialIndex;
        private PlayerControlSystem playerControls;

        private void Awake()
        {
            playerControls = new PlayerControlSystem();
            playerControls.UI.Submit.performed += _ => ViewTutorial();
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
        /// Initializes the button with the information needed to display the tutorial.
        /// </summary>
        /// <param name="index">The index of the tutorial in the master tutorial list.</param>
        /// <param name="header">The header text.</param>
        public void InitializeButton(int index, string header)
        {
            tutorialIndex = index;
            tutorialButtonText.text = header;
        }

        /// <summary>
        /// Lets the player view the tutorial selected.
        /// </summary>
        public void ViewTutorial()
        {
            if (!isSelected || GameManager.Instance.tutorialWindowActive)
                return;

            GameManager.Instance.AudioManager.Play("ButtonClick");
            GameManager.Instance.DisplayTutorial(tutorialIndex, true);
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
        }
    }
}
