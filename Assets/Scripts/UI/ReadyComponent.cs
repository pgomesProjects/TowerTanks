using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerTanks.Scripts
{
    public class ReadyComponent : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI readyComponentText;
        [SerializeField] private Image readyImage;

        [SerializeField] private Color notReadyColor;
        [SerializeField] private Color readyColor;
        private bool isReady;

        private GameObject readyPrompt;

        private void Awake()
        {
            readyPrompt = GameManager.Instance.UIManager.AddButtonPrompt(gameObject, new Vector2(50f, 0f), 85f, GameAction.ReadyUp, PlatformType.Gamepad, GameUIManager.PromptDisplayType.Button, false);
        }

        public void UpdatePlayerNumber(int playerIndex)
        {
            readyComponentText.text = "Player " + (playerIndex + 1).ToString() + " Status:";
        }

        public void UpdateReadyStatus(bool isPlayerReady)
        {
            isReady = isPlayerReady;
            readyImage.color = isReady ? readyColor : notReadyColor;
            readyPrompt.SetActive(!isReady);
        }

        public bool IsReady() => isReady;
    }
}
