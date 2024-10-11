using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ButtonPrompt
    {
        public GameAction action;
        public List<PlatformPrompt> prompts;

        public ButtonPrompt(GameAction action)
        {
            this.action = action;
            prompts = new List<PlatformPrompt>();
            foreach (PlatformType platform in System.Enum.GetValues(typeof(PlatformType)))
                prompts.Add(new PlatformPrompt(platform, ""));
        }
    }

    [System.Serializable]
    public class PlatformPrompt
    {
        [SerializeField]
        private PlatformType platform; // We no longer use { get; private set; }.

        [SerializeField]
        private string prompt; // Ensure this field is serialized

        public PlatformPrompt(PlatformType platform, string prompt)
        {
            this.platform = platform;
            this.prompt = prompt;
        }

        // Public getters so that we can still access these fields if needed
        public PlatformType Platform => platform;
        public string Prompt
        {
            get => prompt;
            set => prompt = value; // Ensures that prompt is still editable
        }
    }

    public enum GameAction
    {
        Interact,
        Build,
        Cancel,
        Pause,
        Jetpack
    }

    public enum PlatformType
    {
        PC,
        Gamepad,
        PlayStation,
        Switch,
        Xbox
    }

    [CreateAssetMenu(fileName = "New Button Settings", menuName = "ScriptableObjects/Button Prompt Settings")]
    public class ButtonPromptSettings : ScriptableObject
    {
        public List<ButtonPrompt> buttonPrompts;

        private void OnEnable()
        {
            // Initialize only if the list is null or empty to avoid overwriting on recompilation
            if (buttonPrompts == null || buttonPrompts.Count == 0)
            {
                buttonPrompts = new List<ButtonPrompt>();

                foreach (GameAction gameAction in System.Enum.GetValues(typeof(GameAction)))
                    buttonPrompts.Add(new ButtonPrompt(gameAction));
            }
        }
    }
}
