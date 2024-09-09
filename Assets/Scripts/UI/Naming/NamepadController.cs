using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.IO;

[System.Serializable]
public class SavedPlayerNameData
{
    public List<string> playerNames = new List<string>();
}

public class NamepadController : MonoBehaviour
{
    [SerializeField, Tooltip("The text for the player button name.")] private TextMeshProUGUI playerButtonNameText;
    [SerializeField, Tooltip("The input text for the name.")] private TextMeshProUGUI namepadText;
    [SerializeField, Tooltip("The text for the current character count.")] private TextMeshProUGUI characterCountText;
    [SerializeField, Tooltip("The maximum amount of characters in the name.")] private int maximumCharacters = 10;
    [SerializeField, Tooltip("The delay for pressing a letter button and letting the button replace the current character instead of adding a new one.")] private float addCharacterDelay = 0.75f;
    [Space]
    [SerializeField, Tooltip("The RectTransform for the player name button.")] private RectTransform playerNameButtonRectTransform;
    [SerializeField, Tooltip("The RectTransform for the custom namepad.")] private RectTransform namepadRectTransform;
    [SerializeField, Tooltip("The RectTransform for the letter buttons container.")] private RectTransform letterButtonsContainer;

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

    private bool customNameActive;
    private PlayerInput currentPlayer;
    private GamepadCursor playerCursor;

    private string savedFileName = "SavedPlayerNames.json";

    private void Awake()
    {
        isShiftActive = false;
        isCapsLockOn = false;
        customNameActive = false;
        letterButtons = letterButtonsContainer.GetComponentsInChildren<LetterButton>();
    }

    public void AssignPlayerToGamepad(PlayerInput playerInput)
    {
        currentPlayer = playerInput;
        playerCursor = currentPlayer.GetComponent<GamepadCursor>();
        GetComponentInChildren<GenericGamepadButton>()?.AssignValidPlayer(playerInput);
        playerNameButtonRectTransform.gameObject.SetActive(true);
        namepadRectTransform.gameObject.SetActive(false);
        GameManager.Instance.SetPlayerCursorActive(playerCursor, true);
        UpdatePlayerButtonNameDisplay();
    }

    public void InitializeGamepad()
    {
        PlayerData playerData = PlayerData.ToPlayerData(currentPlayer);
        playerData.SetPlayerState(PlayerData.PlayerState.SettingUp);
        playerData.SetNamepad(this);

        playerNameButtonRectTransform.gameObject.SetActive(false);
        GameManager.Instance?.SetPlayerCursorActive(playerCursor, false);
        playerName = string.Empty;
        UpdateName();
        HighlightButton(0, 0);
        customNameActive = true;
        namepadRectTransform.gameObject.SetActive(true);
    }

    public void HideGamepad()
    {
        if (customNameActive)
        {
            PlayerData playerData = PlayerData.ToPlayerData(currentPlayer);
            playerData.SetPlayerState(PlayerData.PlayerState.NameReady);
            playerData.SetNamepad(null);

            customNameActive = false;
            namepadRectTransform.gameObject.SetActive(false);
            playerNameButtonRectTransform.gameObject.SetActive(true);
            GameManager.Instance.SetPlayerCursorActive(playerCursor, true);
        }
    }

    public void OnNavigate(PlayerInput playerInput, Vector2 navigateInput)
    {
        if (customNameActive && playerInput.playerIndex == currentPlayer.playerIndex && !isOnNavigateCooldown)
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
        if(customNameActive)
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
        if (playerName.Length > 0)
        {
            playerData.SetPlayerName(playerName);
            SavePlayerName(playerName);
            Debug.Log("Player Name Registered: " + playerName);
        }

        UpdatePlayerButtonNameDisplay();
        HideGamepad();
    }

    private void SavePlayerName(string name)
    {
        string dataFolderPath = Path.Combine(Application.dataPath, "Resources", "Data");
        string filePath = Path.Combine(dataFolderPath, savedFileName);

        // Check if the folder exists, if not, create it
        if (!Directory.Exists(dataFolderPath))
            Directory.CreateDirectory(dataFolderPath);

        List<string> playerNames = new List<string>();

        // Check if the file exists, if yes, load existing names
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            playerNames = JsonUtility.FromJson<SavedPlayerNameData>(json).playerNames;
        }

        // Add the new player name if it doesn't exist
        if (!playerNames.Contains(name))
        {
            playerNames.Add(name);

            SavedPlayerNameData nameList = new SavedPlayerNameData { playerNames = playerNames };
            string updatedJson = JsonUtility.ToJson(nameList, true);

            // Write the updated JSON back to the file
            File.WriteAllText(filePath, updatedJson);

            Debug.Log("Name Saved In " + filePath + ".");
        }
    }

    private string GetCharacterCase(string newCharacter) => isShiftActive ? newCharacter.ToUpper() : newCharacter.ToLower();

    private void UpdateName()
    {
        namepadText.text = playerName.ToString();
        characterCountText.text = playerName.Length.ToString() + " / " + maximumCharacters.ToString();
    }

    private void UpdatePlayerButtonNameDisplay() => playerButtonNameText.text = PlayerData.ToPlayerData(currentPlayer).GetPlayerName();

    public float GetAddCharacterDelay() => addCharacterDelay;
    public bool IsMaximumLengthReached() => playerName.Length >= maximumCharacters;
    public bool IsShiftActive() => isShiftActive;
    public bool IsCapsLockOn() => isCapsLockOn;
}
