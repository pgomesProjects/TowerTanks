using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ButtonPrompt
    {
        public GameAction action;
        public ActionType actionType;
        public List<PlatformPrompt> prompts;

        public ButtonPrompt(GameAction action)
        {
            this.action = action;
            this.actionType = ActionType.Press;
            prompts = new List<PlatformPrompt>();
            GeneratePromptList();
        }

        public ButtonPrompt(GameAction action, ActionType actionType)
        {
            this.action = action;
            this.actionType = actionType;
            GeneratePromptList();
        }

        private void GeneratePromptList()
        {
            prompts = new List<PlatformPrompt>();
            foreach (PlatformType platform in System.Enum.GetValues(typeof(PlatformType)))
            {
                if (platform == PlatformType.PC)
                    prompts.Add(new PlatformPrompt(platform, ""));
                else
                    prompts.Add(new PlatformPrompt(platform, 0, null));
            }
        }
    }

    [System.Serializable]
    public class PlatformPrompt
    {
        [SerializeField] private PlatformType platform;

        [SerializeField] private int spriteID;
        [SerializeField] private Sprite promptSprite;

        [SerializeField] private string promptText;

        public PlatformPrompt(PlatformType platform, int spriteID, Sprite promptSprite)
        {
            this.platform = platform;
            this.spriteID = spriteID;
            this.promptSprite = promptSprite;
        }

        public PlatformPrompt(PlatformType platform, string promptText)
        {
            this.platform = platform;
            this.promptText = promptText;
        }

        public PlatformType Platform => platform;
        public int SpriteID
        {
            get => spriteID;
            set => spriteID = value;
        }
        public Sprite PromptSprite
        {
            get => promptSprite;
            set => promptSprite = value;
        }

        public string PromptText
        {
            get => promptText;
            set => promptText = value;
        }
    }

    public enum GameAction
    {
        Interact,
        Build,
        Cancel,
        Pause,
        Jetpack,
        Mount
    }

    public enum ActionType
    {
        Press,
        Hold,
        Rotate
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
                    AddButtonPrompt(gameAction);
            }
        }

        public void AddButtonPrompt(GameAction gameAction)
        {
            buttonPrompts.Add(new ButtonPrompt(gameAction));
        }

        public PlatformPrompt GetButtonPrompt(GameAction gameAction, PlatformType platform)
        {
            foreach(ButtonPrompt prompt in buttonPrompts)
            {
                //If the action is found in the list
                if (prompt.action == gameAction)
                {
                    //Search for the prompt data based on the platform given
                    foreach (PlatformPrompt platformPrompt in prompt.prompts)
                        if (platformPrompt.Platform == platform)
                            return platformPrompt;
                }
            }

            return null;
        }
    }
}
