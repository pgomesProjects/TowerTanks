using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    [RequireComponent(typeof(Image))]
    public class ImagePlatformListener : PlatformListener
    {
        [SerializeField, Tooltip("The color for the image if the platform prompt is disabled.")] private Color disabledColor;

        private Image platformImage;
        private Color defaultColor;

        private void Awake()
        {
            platformImage = GetComponent<Image>();
            defaultColor = platformImage.color;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void UpdatePlatformPrompt(GameAction gameAction)
        {
            currentGameAction = gameAction;
            UpdatePlatformVisuals();
        }

        protected override void UpdatePlatformVisuals()
        {
            PlatformPrompt prompt = GameManager.Instance.UIManager.GetButtonPromptSettings().GetPlatformPrompt(currentGameAction, GameSettings.gamePlatform);
            platformImage.sprite = prompt.PromptSprite;

            if (gameActionTypeText != null)
                gameActionTypeText.text = GameManager.Instance.UIManager.GetButtonPromptSettings().GetPromptActionType(currentGameAction);
        }

        protected override void UpdateEnabled()
        {
            platformImage.color = enabled ? defaultColor : disabledColor;
        }
    }
}
