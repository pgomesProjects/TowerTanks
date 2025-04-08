using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TowerTanks.Scripts
{
    public class ConfirmationWindow : MonoBehaviour
    {
        [SerializeField, Tooltip("The confirmation text.")] private TextMeshProUGUI confirmationText;
        [SerializeField, Tooltip("The yes button.")] private Button yesButton;
        [SerializeField, Tooltip("The no button.")] private Button noButton;

        internal UnityEvent onYesSelected = new UnityEvent();
        internal UnityEvent onNoSelected = new UnityEvent();

        private void OnDisable()
        {
            EventSystem.current.SetSelectedGameObject(null);
            yesButton.onClick.RemoveAllListeners();
            onYesSelected.RemoveAllListeners();
            noButton.onClick.RemoveAllListeners();
            onNoSelected.RemoveAllListeners();
        }

        /// <summary>
        /// Initializes the confirmation window.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="yesAction">The action(s) for when the yes button is pressed.</param>
        /// <param name="noAction">The action(s) for when the no button is pressed.</param>
        public void Init(string message, UnityAction yesAction, UnityAction noAction)
        {
            SetConfirmationMessage(message);
            yesButton.onClick.AddListener(() => onYesSelected?.Invoke());
            onYesSelected.AddListener(yesAction);
            onYesSelected.AddListener(DisableButton);
            noButton.onClick.AddListener(() => onNoSelected?.Invoke());
            onNoSelected.AddListener(noAction);
            onNoSelected.AddListener(DisableButton);

            //Have the yes button selected on default
            EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
        }

        private void SetConfirmationMessage(string message) => confirmationText.text = message;
        private void DisableButton() => gameObject.SetActive(false);

        public Selectable GetYesButton() => yesButton;
        public Selectable GetNoButton() => noButton;
    }
}
