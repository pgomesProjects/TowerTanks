using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.IO;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class SavedPlayerNameData
    {
        public List<string> playerNames = new List<string>();
    }

    public class NamepadController : MonoBehaviour
    {
        [SerializeField, Tooltip("The text for the player button name.")] private TextMeshProUGUI playerButtonNameText;
        [Space]
        [SerializeField, Tooltip("The RectTransform for the select player names.")] private RectTransform selectPlayerNameRectTransform;
        [SerializeField, Tooltip("The container for the player names.")] private RectTransform playerNamesContainer;
        [SerializeField, Tooltip("The prefab for the player name buttons.")] private GameObject playerNamePrefab;
        [Space]
        [SerializeField, Tooltip("The input text for the name.")] private TextMeshProUGUI namepadText;
        [SerializeField, Tooltip("The text for the current character count.")] private TextMeshProUGUI characterCountText;
        [SerializeField, Tooltip("The maximum amount of characters in the name.")] private int maximumCharacters = 10;
        [SerializeField, Tooltip("The delay for pressing a letter button and letting the button replace the current character instead of adding a new one.")] private float addCharacterDelay = 0.75f;
        [Space]
        [SerializeField, Tooltip("The RectTransform for the player name button.")] private RectTransform playerNameButtonRectTransform;
        [SerializeField, Tooltip("The RectTransform for the custom namepad.")] private RectTransform namepadRectTransform;
        [SerializeField, Tooltip("The RectTransform for the letter buttons container.")] private RectTransform letterButtonsContainer;

        private enum NamepadState { IDLE, SAVEDNAMES, CUSTOMNAME };
        private NamepadState currentNamepadState;

        private GenericGamepadButton currentSavedNameButton;
        private int savedNameButtonIndex;
        private float playerNameContainerHeight;
        private float playerNamePrefabHeight;

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

        private PlayerInput currentPlayer;
        private GamepadCursor playerCursor;

        private string savedFileName = "SavedPlayerNames.json";

        private void Awake()
        {
            isShiftActive = false;
            isCapsLockOn = false;
            letterButtons = letterButtonsContainer.GetComponentsInChildren<LetterButton>();
            playerNameContainerHeight = GetComponent<RectTransform>().sizeDelta.y;
            playerNamePrefabHeight = playerNamePrefab.GetComponent<RectTransform>().sizeDelta.y;
        }

        public void AssignPlayerToGamepad(PlayerInput playerInput)
        {
            currentPlayer = playerInput;
            playerCursor = currentPlayer.GetComponent<GamepadCursor>();

            playerNameButtonRectTransform.gameObject.SetActive(true);
            selectPlayerNameRectTransform.gameObject.SetActive(false);
            namepadRectTransform.gameObject.SetActive(false);

            foreach (GenericGamepadButton gamepadButton in GetComponentsInChildren<GenericGamepadButton>())
                gamepadButton.AssignValidPlayer(playerInput);

            GameManager.Instance.SetPlayerCursorActive(playerCursor, true);
            UpdatePlayerButtonNameDisplay();
        }

        public void InitializeGamepad()
        {
            PlayerData playerData = PlayerData.ToPlayerData(currentPlayer);
            playerData.SetPlayerState(PlayerData.PlayerState.SettingUp);
            playerData.SetNamepad(this);
            GameManager.Instance?.SetPlayerCursorActive(playerCursor, false);
            ChangeNamepadState(NamepadState.SAVEDNAMES);
            currentPlayer.SwitchCurrentActionMap("GameCursor");
        }

        private void ChangeNamepadState(NamepadState newNamepadState)
        {
            switch (newNamepadState)
            {
                case NamepadState.SAVEDNAMES:
                    InstantiatePlayerNames();
                    savedNameButtonIndex = 0;
                    SelectSavedNameButton(0);

                    selectPlayerNameRectTransform.gameObject.SetActive(true);
                    namepadRectTransform.gameObject.SetActive(false);
                    playerNameButtonRectTransform.gameObject.SetActive(false);
                    break;
                case NamepadState.CUSTOMNAME:
                    playerName = string.Empty;
                    UpdateName();
                    HighlightGridButton(0, 0);

                    selectPlayerNameRectTransform.gameObject.SetActive(false);
                    namepadRectTransform.gameObject.SetActive(true);
                    playerNameButtonRectTransform.gameObject.SetActive(false);
                    break;
                default:
                    ClearSavedPlayerNames();
                    ExitNamepad();
                    selectPlayerNameRectTransform.gameObject.SetActive(false);
                    namepadRectTransform.gameObject.SetActive(false);
                    playerNameButtonRectTransform.gameObject.SetActive(true);
                    break;
            }

            currentNamepadState = newNamepadState;
        }

        public void EnterCustomName()
        {
            ChangeNamepadState(NamepadState.CUSTOMNAME);
        }

        public void Cancel()
        {
            switch (currentNamepadState)
            {
                case NamepadState.SAVEDNAMES:
                    ChangeNamepadState(NamepadState.IDLE);
                    break;
                case NamepadState.CUSTOMNAME:
                    ChangeNamepadState(NamepadState.SAVEDNAMES);
                    break;
            }
        }

        private void ExitNamepad()
        {
            PlayerData playerData = PlayerData.ToPlayerData(currentPlayer);
            playerData.SetPlayerState(PlayerData.PlayerState.NameReady);
            playerData.SetNamepad(null);
            GameManager.Instance.SetPlayerCursorActive(playerCursor, true);
            currentPlayer.SwitchCurrentActionMap("Player");
        }

        public void InstantiatePlayerNames()
        {
            List<string> playerNames = LoadPlayerNames();

            playerNamesContainer.GetChild(0).GetComponent<GenericGamepadButton>().AssignValidPlayer(currentPlayer);

            for (int i = 0; i < playerNames.Count; i++)
            {
                string savedName = playerNames[i];

                TextMeshProUGUI playerNameText = Instantiate(playerNamePrefab, playerNamesContainer).GetComponentInChildren<TextMeshProUGUI>();
                playerNameText.text = savedName;
                playerNameText.transform.parent.name = savedName;

                GenericGamepadButton gamepadButton = playerNamesContainer.GetChild(i + 1).GetComponent<GenericGamepadButton>();
                gamepadButton.AssignValidPlayer(currentPlayer);
                gamepadButton.OnSelected.AddListener(() => SetSavedName(savedName));
            }
        }

        private void ClearSavedPlayerNames()
        {
            //Clear any existing list except for the first button
            foreach (GenericGamepadButton gamepadButton in playerNamesContainer.GetComponentsInChildren<GenericGamepadButton>())
            {
                if (gamepadButton.transform.GetSiblingIndex() != 0)
                    Destroy(gamepadButton.gameObject);
            }

            currentSavedNameButton = null;
        }

        public void OnNavigate(PlayerInput playerInput, Vector2 navigateInput)
        {
            if (playerInput.playerIndex == currentPlayer.playerIndex && !isOnNavigateCooldown)
            {
                switch (currentNamepadState)
                {
                    case NamepadState.SAVEDNAMES:
                        int verticalInput = Mathf.RoundToInt(-navigateInput.y);
                        NavigateSavedNames(verticalInput);
                        break;
                    case NamepadState.CUSTOMNAME:
                        int navigateRow = Mathf.RoundToInt(-navigateInput.y);
                        int navigateCol = Mathf.RoundToInt(navigateInput.x);

                        currentRow = Mathf.Clamp(currentRow + navigateRow, 0, gridRows - 1);
                        currentColumn = Mathf.Clamp(currentColumn + navigateCol, 0, gridColumns - 1);
                        HighlightGridButton(currentRow, currentColumn);
                        break;
                }

                isOnNavigateCooldown = true;
                currentNavigateCooldown = 0f;
            }
        }

        private void NavigateSavedNames(int direction)
        {
            savedNameButtonIndex = Mathf.Clamp(savedNameButtonIndex + direction, 0, playerNamesContainer.childCount - 1);

            //Deselect the previous button, if any
            currentSavedNameButton?.OnDeselect();

            SelectSavedNameButton(savedNameButtonIndex);
        }

        private void SelectSavedNameButton(int index)
        {
            // Highlight the new button
            currentSavedNameButton = playerNamesContainer.GetChild(index).GetComponentInChildren<GenericGamepadButton>();
            currentSavedNameButton.OnSelect();

            float currentButtonYPos = currentSavedNameButton.GetComponent<RectTransform>().anchoredPosition.y;

            float newContainerYPos = Mathf.Max(0, -(currentButtonYPos - (playerNamePrefabHeight / 2)) - playerNameContainerHeight);
            playerNamesContainer.anchoredPosition = new Vector2(selectPlayerNameRectTransform.anchoredPosition.x, newContainerYPos);
        }

        private void HighlightGridButton(int row, int column)
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
            switch (currentNamepadState)
            {
                case NamepadState.SAVEDNAMES:
                    currentSavedNameButton?.OnSelectObject(playerInput);
                    break;
                case NamepadState.CUSTOMNAME:
                    currentHighlightedButton?.OnClick(playerInput);
                    break;
            }
        }

        private void SetSavedName(string savedName)
        {
            playerName = savedName;
            FinalizeName(PlayerData.ToPlayerData(currentPlayer), false);
        }

        private void Update()
        {
            if (isOnNavigateCooldown)
            {
                currentNavigateCooldown += Time.deltaTime;
                if (currentNavigateCooldown >= navigateCooldown)
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
            if (playerName.Length > 0)
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
                foreach (LetterButton button in letterButtons)
                {
                    if (button.GetButtonType() == LetterButton.LetterButtonType.LETTER)
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
            if (isShiftActive && !isCapsLockOn)
            {
                isShiftActive = false;
                foreach (LetterButton button in letterButtons)
                {
                    if (button.GetButtonType() == LetterButton.LetterButtonType.LETTER)
                        button.UpdateLetterDisplay();
                }
            }
        }

        public void FinalizeName(PlayerData playerData, bool saveNameToFile = false)
        {
            if (playerName.Length > 0)
            {
                if (saveNameToFile)
                    SavePlayerName(playerName);

                playerData.SetPlayerName(playerName);
                Debug.Log("Player Name Registered: " + playerName);
            }

            UpdatePlayerButtonNameDisplay();
            ChangeNamepadState(NamepadState.IDLE);
        }

        private void SavePlayerName(string name)
        {
            string dataFolderPath = Path.Combine(Application.dataPath, "Resources", "Data");
            string filePath = GetSavedNamesFilePath();

            // Check if the folder exists, if not, create it
            if (!Directory.Exists(dataFolderPath))
                Directory.CreateDirectory(dataFolderPath);

            //Load in the list of existing names
            List<string> playerNames;
            playerNames = LoadPlayerNames();

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

        private List<string> LoadPlayerNames()
        {
            string filePath = GetSavedNamesFilePath();

            // Check if the file exists, if yes, load existing names
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<SavedPlayerNameData>(json).playerNames;
            }

            //If nothing found, return an empty list
            return new List<string>();
        }

        private string GetSavedNamesFilePath()
        {
            string dataFolderPath = Path.Combine(Application.dataPath, "Resources", "Data");
            return Path.Combine(dataFolderPath, savedFileName);
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
}
