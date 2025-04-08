using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public abstract class PlatformListener : SerializedMonoBehaviour
    {
        public bool isEnabled = true;
        [SerializeField, Tooltip("The text to display the type of action to perform.")] protected TextMeshProUGUI gameActionTypeText;

        [Button(ButtonSizes.Medium, ButtonStyle.FoldoutButton)]
        private void DebugChangeGameAction(GameAction gameAction)
        {
            currentGameAction = gameAction;
            UpdatePlatformPrompt(currentGameAction);
        }

        protected GameAction currentGameAction;

        protected virtual void OnEnable()
        {
            GameManager.OnPlatformUpdated += OnPlatformUpdated;
            UpdatePlatformVisuals();
        }

        protected virtual void OnDisable()
        {
            GameManager.OnPlatformUpdated -= OnPlatformUpdated;
        }

        private void OnPlatformUpdated() => UpdatePlatformVisuals();

        public abstract void UpdatePlatformPrompt(GameAction gameAction);
        protected abstract void UpdatePlatformVisuals();
        protected abstract void UpdateEnabled();
    }
}
