using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TowerTanks.Scripts
{
    public class LetterButton : GamepadSelectable
    {
        public enum LetterButtonType { LETTER, SPACE, BACKSPACE, SHIFT, CONFIRM }

        [SerializeField, Tooltip("The type of button.")] private LetterButtonType buttonType;
        [SerializeField, Tooltip("The CanvasGroup component for the button hover.")] private CanvasGroup buttonHoverCanvasGroup;
        [SerializeField, Tooltip("The letters to cycle through when pressing a letter button.")] private string[] letters;

        private NamepadController namepadParent;
        private TextMeshProUGUI buttonText;
        private int currentLetterIndex = 0;
        private bool isActive;
        private bool isTimerActive;
        private float currentAddDelayTimer;

        private void Awake()
        {
            namepadParent = transform.parent.parent.parent.GetComponent<NamepadController>();
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        private void Init()
        {
            if (buttonType == LetterButtonType.LETTER)
                UpdateLetterDisplay();
            else
            {
                switch (buttonType)
                {
                    case LetterButtonType.SPACE:
                        buttonText.text = "Space";
                        break;
                    case LetterButtonType.BACKSPACE:
                        buttonText.text = "Back";
                        break;
                    case LetterButtonType.SHIFT:
                        buttonText.text = "Shift";
                        break;
                    case LetterButtonType.CONFIRM:
                        buttonText.text = "OK";
                        break;
                }
            }
        }

        public override void OnCursorEnter(PlayerInput playerInput)
        {
            if (IsValidPlayer(playerInput.playerIndex))
                OnSelect();
        }

        public override void OnCursorExit(PlayerInput playerInput)
        {
            if (IsValidPlayer(playerInput.playerIndex))
                OnDeselect();
        }

        public override void OnSelectObject(PlayerInput playerInput)
        {
            if (IsValidPlayer(playerInput.playerIndex))
            {
                if (isSelected)
                    OnClick(playerInput);

                else
                    OnDeselect();
            }
        }

        public void OnSelect()
        {
            buttonHoverCanvasGroup.alpha = 1f;
            isSelected = true;
            //Debug.Log(gameObject.name + " Selected!");
        }

        public void OnDeselect()
        {
            //Debug.Log(gameObject.name + " Not Selected!");
            buttonHoverCanvasGroup.alpha = 0f;
            isSelected = false;
            isTimerActive = false;
            currentLetterIndex = 0;
            currentAddDelayTimer = 0f;

            if (isActive)
                DeactivateButton();
        }

        public void OnClick(PlayerInput playerInput)
        {
            if (isSelected)
            {
                switch (buttonType)
                {
                    case LetterButtonType.LETTER:
                        currentAddDelayTimer = 0f;

                        if (!isTimerActive)
                        {
                            if (!namepadParent.IsMaximumLengthReached())
                            {
                                currentLetterIndex = 0;
                                namepadParent?.AddCharacter(letters[currentLetterIndex]);
                                isTimerActive = true;
                                isActive = true;
                            }
                        }
                        else
                        {
                            IncrementLetterIndex();
                            namepadParent?.ReplaceCharacter(letters[currentLetterIndex]);
                        }
                        break;
                    case LetterButtonType.SPACE:
                        namepadParent?.AddCharacter(" ");
                        break;
                    case LetterButtonType.BACKSPACE:
                        namepadParent?.Backspace();
                        break;
                    case LetterButtonType.SHIFT:
                        namepadParent?.Shift();
                        break;
                    case LetterButtonType.CONFIRM:
                        namepadParent?.FinalizeName(PlayerData.ToPlayerData(playerInput), true);
                        break;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (isTimerActive)
            {
                currentAddDelayTimer += Time.deltaTime;
                if (currentAddDelayTimer >= namepadParent.GetAddCharacterDelay())
                    DeactivateButton();
            }
        }

        private void DeactivateButton()
        {
            isTimerActive = false;
            isActive = false;
            namepadParent?.CheckForUnshift();
        }

        private void IncrementLetterIndex()
        {
            currentLetterIndex++;

            if (currentLetterIndex >= letters.Length)
                currentLetterIndex = 0;
        }

        public void UpdateLetterDisplay()
        {
            string buttonLetterDisplay = "";

            foreach (string letter in letters)
                buttonLetterDisplay += namepadParent.IsShiftActive() ? letter.ToUpper() : letter.ToLower();

            buttonText.text = buttonLetterDisplay;
        }

        public LetterButtonType GetButtonType() => buttonType;
    }
}
