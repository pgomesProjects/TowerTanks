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
        [SerializeField] private Image iconImage;

        [SerializeField] private Color notReadyColor;
        [SerializeField] private Color readyColor;
        
        private bool isReady;

        private GameObject readyPrompt;
        [SerializeField] private Sprite[] iconSprites;

        [SerializeField] private Animator iconAnimator;
        private float _playerIndex;

        private void Awake()
        {
            //readyPrompt = GameManager.Instance.UIManager.AddButtonPrompt(gameObject, new Vector2(50f, 0f), 65f, GameAction.ReadyUp, PlatformType.Gamepad, GameUIManager.PromptDisplayType.Button, false);
        }

        public void UpdatePlayerNumber(int playerIndex)
        {
            readyComponentText.text = "PLAYER " + (playerIndex + 1).ToString() + ":";
            //iconImage.sprite = iconSprites[playerIndex];
            _playerIndex = playerIndex;
        }

        public void UpdateReadyStatus(bool isPlayerReady)
        {
            isReady = isPlayerReady;
            readyImage.color = isReady ? readyColor : notReadyColor;
            readyPrompt?.SetActive(!isReady);

            if (isReady)
            {
                GameManager.Instance.AudioManager.Play("UseSFX");
                iconAnimator.Play("IconPulse", 0, 0);
                float direction = 0.6f;
                if (_playerIndex == 1 || _playerIndex == 3) direction *= -1f;
                iconAnimator.SetFloat("direction", direction);
            }
            else
            {
                GameManager.Instance.AudioManager.Play("ButtonCancel");
                iconAnimator.Play("Default", 0, 0);
            }
        }

        public bool IsReady() => isReady;
    }
}
