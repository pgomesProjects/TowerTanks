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

        [SerializeField] private PlatformFaceButtonsSettings platformFaceButtons;

        internal PlatformType currentPlatform { get; private set; }

        private Dictionary<string, int> activeFaceInputs = new Dictionary<string, int>();

        // Start is called before the first frame update
        void Start()
        {
            BuildFaceButtons(platformFaceButtons);
            RefreshFaceButtons();
        }

        private void BuildFaceButtons(PlatformFaceButtonsSettings platformFaceButtonsSettings)
        {
            activeFaceInputs.Clear();

            activeFaceInputs.Add(platformFaceButtonsSettings.northPrompt.name, 0);
            activeFaceInputs.Add(platformFaceButtonsSettings.eastPrompt.name, 0);
            activeFaceInputs.Add(platformFaceButtonsSettings.southPrompt.name, 0);
            activeFaceInputs.Add(platformFaceButtonsSettings.westPrompt.name, 0);

            northFaceButton.sprite = platformFaceButtonsSettings.northPrompt.promptSprite;
            eastFaceButton.sprite = platformFaceButtonsSettings.eastPrompt.promptSprite;
            southFaceButton.sprite = platformFaceButtonsSettings.southPrompt.promptSprite;
            westFaceButton.sprite = platformFaceButtonsSettings.westPrompt.promptSprite;

            currentPlatform = platformFaceButtonsSettings.platformType;
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
            northFaceButton.color = activeFaceInputs[platformFaceButtons.northPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
            eastFaceButton.color = activeFaceInputs[platformFaceButtons.eastPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
            southFaceButton.color = activeFaceInputs[platformFaceButtons.southPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
            westFaceButton.color = activeFaceInputs[platformFaceButtons.westPrompt.name] > 0 ? Color.white : disabledFaceInputColor;
        }

        public void ClearFaceButtons()
        {
            activeFaceInputs[platformFaceButtons.northPrompt.name] = 0;
            activeFaceInputs[platformFaceButtons.eastPrompt.name] = 0;
            activeFaceInputs[platformFaceButtons.southPrompt.name] = 0;
            activeFaceInputs[platformFaceButtons.westPrompt.name] = 0;
        }
    }
}
