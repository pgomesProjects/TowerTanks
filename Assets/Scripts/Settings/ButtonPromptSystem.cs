using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "New Button Prompt System", menuName = "ScriptableObjects/Button Prompt System")]
    public class ButtonPromptSystem : ScriptableObject
    {
        public List<ButtonAction> actions = new List<ButtonAction>();

        /// <summary>
        /// Gets the action from the list.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <returns>Returns the action from the list if found. Returns null if not found.</returns>
        public ButtonAction GetAction(string actionName)
        {
            //If the name is found in the list, return it
            foreach (var action in actions)
                if (action.name == actionName)
                    return action;

            Debug.LogWarning("'" + actionName + "' not found.");
            return null;
        }

        /// <summary>
        /// Gets the platform prompt.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="platform">The type of platform.</param>
        /// <returns>Returns the prompt info specific to the platform.</returns>
        public PromptInfo GetPlatformPrompt(string actionName, PlatformType platform)
        {
            //Get the action from the list
            ButtonAction currentAction = GetAction(actionName);
            if (currentAction == null)
                return null;

            //Return the prompt info specific to the platform
            return currentAction.promptInfo[(int)platform];
        }
    }

    [System.Serializable]
    public class ButtonAction
    {
        public string name;
        public ActionType actionType;
        public PromptInfo[] promptInfo;

        public ButtonAction(string name)
        {
            this.name = name;
            //Create a list of prompt infos based on the number of platforms
            promptInfo = new PromptInfo[System.Enum.GetNames(typeof(PlatformType)).Length];
        }
    }
}
