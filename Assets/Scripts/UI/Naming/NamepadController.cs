using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class NamepadController : MonoBehaviour
{
    [SerializeField, Tooltip("The input text for the name.")] private TextMeshProUGUI namepadText;
    [SerializeField, Tooltip("The text for the current character count.")] private TextMeshProUGUI characterCountText;
    [SerializeField, Tooltip("The maximum amount of characters in the name.")] private int maximumCharacters = 10;
    [SerializeField, Tooltip("The delay for pressing a letter button and letting the button replace the current character instead of adding a new one.")] private float addCharacterDelay = 0.75f;

    private int currentRow = 0;
    private int currentColumn = 0;
    private int gridRows = 5;
    private int gridColumns = 3;
    private float navigateCooldown = 0.05f;
    private float currentNavigateCooldown;
    private bool isOnNavigateCooldown;

    private string playerName;
    private bool isShiftActive;
    private bool isCapsLockOn;
    private LetterButton[] letterButtons;
    private LetterButton currentHighlightedButton;

    private void Awake()
    {
        isShiftActive = false;
        isCapsLockOn = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        letterButtons = GetComponentsInChildren<LetterButton>();
        playerName = string.Empty;
        UpdateName();
        HighlightButton(0, 0);
    }

    public void OnNavigate(Vector2 navigateInput)
    {
        if (!isOnNavigateCooldown)
        {
            int navigateRow = Mathf.RoundToInt(-navigateInput.y);
            int navigateCol = Mathf.RoundToInt(navigateInput.x);

            currentRow = Mathf.Clamp(currentRow + navigateRow, 0, gridRows - 1);
            currentColumn = Mathf.Clamp(currentColumn + navigateCol, 0, gridColumns - 1);
            HighlightButton(currentRow, currentColumn);

            isOnNavigateCooldown = true;
            currentNavigateCooldown = 0f;
        }
    }

    private void HighlightButton(int row, int column)
    {
        int buttonIndex = row * gridColumns + column;
        if (buttonIndex >= 0 && buttonIndex < letterButtons.Length)
        {
            //Deselect the previous button, if any
            currentHighlightedButton?.OnDeselect();

            // Highlight the new button
            currentHighlightedButton = letterButtons[buttonIndex];
            currentHighlightedButton.OnSelect();
        }
    }

    public void SelectCurrentButton(PlayerInput playerInput)
    {
        currentHighlightedButton?.OnClick(playerInput);
    }

    private void Update()
    {
        if (isOnNavigateCooldown)
        {
            currentNavigateCooldown += Time.deltaTime;
            if(currentNavigateCooldown >= navigateCooldown)
                isOnNavigateCooldown = false;
        }
    }

    public void AddCharacter(string newCharacter)
    {
        playerName += GetCharacterCase(newCharacter);
        UpdateName();
    }

    public void ReplaceCharacter(string newCharacter)
    {
        playerName = playerName.Remove(playerName.Length - 1, 1) + GetCharacterCase(newCharacter);
        UpdateName();
    }

    public void Backspace()
    {
        if(playerName.Length > 0)
        {
            playerName = playerName.Remove(playerName.Length - 1, 1);
            UpdateName();
        }
    }

    public void Shift()
    {
        if (!isShiftActive)
        {
            isShiftActive = true;
            foreach(LetterButton button in letterButtons)
            {
                if(button.GetButtonType() == LetterButton.LetterButtonType.LETTER)
                    button.UpdateLetterDisplay();
            }
        }
        else if (!isCapsLockOn)
        {
            isCapsLockOn = true;
        }
        else
        {
            isShiftActive = false;
            isCapsLockOn = false;
            foreach (LetterButton button in letterButtons)
            {
                if (button.GetButtonType() == LetterButton.LetterButtonType.LETTER)
                    button.UpdateLetterDisplay();
            }
        }
    }

    public void CheckForUnshift()
    {
        if(isShiftActive && !isCapsLockOn)
        {
            isShiftActive = false;
            foreach (LetterButton button in letterButtons)
            {
                if (button.GetButtonType() == LetterButton.LetterButtonType.LETTER)
                    button.UpdateLetterDisplay();
            }
        }
    }

    public void FinalizeName(PlayerData playerData)
    {
        if (playerName.Length == 0)
            playerData.SetDefaultPlayerName();
        else
            playerData.SetPlayerName(playerName);

        Debug.Log("Player Name Registered: " + playerName);

        gameObject.SetActive(false);
    }

    private string GetCharacterCase(string newCharacter) => isShiftActive ? newCharacter.ToUpper() : newCharacter.ToLower();

    private void UpdateName()
    {
        namepadText.text = playerName.ToString();
        characterCountText.text = playerName.Length.ToString() + " / " + maximumCharacters.ToString();
    }

    public float GetAddCharacterDelay() => addCharacterDelay;
    public bool IsMaximumLengthReached() => playerName.Length >= maximumCharacters;
    public bool IsShiftActive() => isShiftActive;
    public bool IsCapsLockOn() => isCapsLockOn;
}
