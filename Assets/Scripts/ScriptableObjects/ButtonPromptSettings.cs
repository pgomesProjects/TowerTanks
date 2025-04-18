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
            prompts = new List<PlatformPrompt>();
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
        [SerializeField] private PromptInfo promptInfo;

        public PlatformPrompt(PlatformType platform, int spriteID, Sprite promptSprite)
        {
            this.platform = platform;
            promptInfo = ScriptableObject.CreateInstance<PromptInfo>();
            promptInfo.spriteID = spriteID;
            promptInfo.promptSprite = promptSprite;
        }

        public PlatformPrompt(PlatformType platform, string promptText)
        {
            this.platform = platform;
            promptInfo = ScriptableObject.CreateInstance<PromptInfo>();
            promptInfo.name = promptText;
        }

        public PlatformType Platform => platform;
        public int SpriteID
        {
            get => promptInfo.spriteID;
            set => promptInfo.spriteID = value;
        }
        public Sprite PromptSprite
        {
            get => promptInfo.promptSprite;
            set => promptInfo.promptSprite = value;
        }

        public string GetPromptText() => promptInfo.name;
        public void SetPromptText(string newPromptText) => promptInfo.name = newPromptText;
    }

    public enum GameAction
    {
        Interact,
        Build,
        Cancel,
        Pause,
        Jetpack,
        Mount,
        Fire,
        Repair,
        AddFuel,
        ReleaseSteam,
        ReadyUp,
        AdvanceTutorial,
        MoveG,
        MoveD,
        PumpShield,
        Undo
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

        public PlatformPrompt GetPlatformPrompt(GameAction gameAction, PlatformType platform)
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

        public string GetPromptActionType(GameAction gameAction)
        {
            foreach (ButtonPrompt prompt in buttonPrompts)
            {
                //If the action is found in the list
                if (prompt.action == gameAction)
                {
                    //Return the appropriate type for the action
                    switch (prompt.actionType)
                    {
                        case ActionType.Press:
                            return "Press";
                        case ActionType.Hold:
                            return "Hold";
                        case ActionType.Rotate:
                            return "Rotate";
                        case ActionType.RapidPress:
                            return "Spam";
                        default:
                            return string.Empty;
                    }
                }
            }

            return string.Empty;
        }
    }
}
