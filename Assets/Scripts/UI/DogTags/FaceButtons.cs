using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class FaceButtons : MonoBehaviour
    {
        [SerializeField] private Image northFaceButton;
        [SerializeField] private Image eastFaceButton;
        [SerializeField] private Image southFaceButton;
        [SerializeField] private Image westFaceButton;
        [SerializeField] private Color disabledFaceInputColor;

        private Dictionary<string, int> activeFaceInputs = new Dictionary<string, int>();
        private PlatformFaceButtonsSettings currentPlatformFaceButtons;

        private void Awake()
        {
            currentPlatformFaceButtons = GameManager.Instance.UIManager.GetPlatformFaceButtons(GameSettings.gamePlatform);
            BuildFaceButtons(currentPlatformFaceButtons);
            RefreshFaceButtons();
        }

        private void OnEnable()
        {
            GameManager.OnPlatformUpdated += OnPlatformUpdated;
        }

        private void OnDisable()
        {
            GameManager.OnPlatformUpdated -= OnPlatformUpdated;
        }

        private void OnPlatformUpdated() => UpdatePlatformSprites(GameManager.Instance.UIManager.GetPlatformFaceButtons(GameSettings.gamePlatform));

        private void BuildFaceButtons(PlatformFaceButtonsSettings platformFaceButtonsSettings)
        {
            activeFaceInputs.Clear();

            activeFaceInputs.Add(platformFaceButtonsSettings.northPrompt.name, 0);
            activeFaceInputs.Add(platformFaceButtonsSettings.eastPrompt.name, 0);
            activeFaceInputs.Add(platformFaceButtonsSettings.southPrompt.name, 0);
            activeFaceInputs.Add(platformFaceButtonsSettings.westPrompt.name, 0);
            UpdatePlatformSprites(platformFaceButtonsSettings);
        }

        private void UpdatePlatformSprites(PlatformFaceButtonsSettings platformFaceButtonsSettings)
        {
            northFaceButton.sprite = platformFaceButtonsSettings.northPrompt.promptSprite;
            eastFaceButton.sprite = platformFaceButtonsSettings.eastPrompt.promptSprite;
            southFaceButton.sprite = platformFaceButtonsSettings.southPrompt.promptSprite;
            westFaceButton.sprite = platformFaceButtonsSettings.westPrompt.promptSprite;
            currentPlatformFaceButtons = platformFaceButtonsSettings;
        }

        public void AddFaceInput(string faceButtonName)
        {
            if (activeFaceInputs.ContainsKey(faceButtonName))
                activeFaceInputs[faceButtonName] += 1;

            RefreshFaceButtons();
        }

        public void RemoveFaceInput(string faceButtonName)
        {
            if (activeFaceInputs.ContainsKey(faceButtonName))
                activeFaceInputs[faceButtonName] = Mathf.Max(0, activeFaceInputs[faceButtonName] - 1);

            RefreshFaceButtons();
        }

        private void RefreshFaceButtons()
        {
            northFaceButton.color = activeFaceInputs[currentPlatformFaceButtons.northPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
            eastFaceButton.color = activeFaceInputs[currentPlatformFaceButtons.eastPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
            southFaceButton.color = activeFaceInputs[currentPlatformFaceButtons.southPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
            westFaceButton.color = activeFaceInputs[currentPlatformFaceButtons.westPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
        }

        public void ClearFaceButtons()
        {
            activeFaceInputs[currentPlatformFaceButtons.northPrompt.name] = 0;
            activeFaceInputs[currentPlatformFaceButtons.eastPrompt.name] = 0;
            activeFaceInputs[currentPlatformFaceButtons.southPrompt.name] = 0;
            activeFaceInputs[currentPlatformFaceButtons.westPrompt.name] = 0;

            RefreshFaceButtons();
        }
    }
}
