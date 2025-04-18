using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class PlayerAnalyticsTracker : MonoBehaviour
    {
        private PlayerData playerData;

        //Interaction variables
        public Dictionary<string, List<(System.DateTime, System.DateTime)>> interactions { get; private set; }
        private string currentInteractionName;
        private System.DateTime interactionStartTime;

        private void Awake()
        {
            playerData = GetComponent<PlayerData>();
            interactions = new Dictionary<string, List<(System.DateTime, System.DateTime)>>();
            currentInteractionName = string.Empty;
        }

        /// <summary>
        /// Starts an interaction.
        /// </summary>
        /// <param name="interactionName">The name of the interaction.</param>
        public void StartInteraction(string interactionName)
        {
            //If the scene is not the combat scene, return
            if (GameManager.Instance.currentSceneState != SCENESTATE.CombatScene)
                return;

            //If there is an interaction already active, return
            if (currentInteractionName != string.Empty)
                return;

            //Store the interaction name and interaction
            currentInteractionName = interactionName;
            interactionStartTime = System.DateTime.Now;
        }

        /// <summary>
        /// Ends an interaction.
        /// </summary>
        /// <param name="interactionName">The name of the interaction.</param>
        public void EndInteraction(string interactionName)
        {
            //If the scene is not the combat scene, return
            if (GameManager.Instance.currentSceneState != SCENESTATE.CombatScene)
                return;

            //If the interaction name is not the one currently stored, return
            if (interactionName != currentInteractionName)
                return;

            //Add the finished interaction to the list
            if(!interactions.ContainsKey(interactionName))
                interactions.Add(interactionName, new List<(System.DateTime, System.DateTime)>());
            interactions[interactionName].Add((interactionStartTime, System.DateTime.Now));

            currentInteractionName = string.Empty;

            Debug.Log(playerData.playerName + " " + interactionName + " Interaction Time: " + (interactions[interactionName][interactions[interactionName].Count - 1].Item2 - interactions[interactionName][interactions[interactionName].Count - 1].Item1).TotalSeconds.ToString("F0") + " seconds");
        }

        /// <summary>
        /// Gets a list of interactions the player has completed.
        /// </summary>
        /// <returns>A list of interactions and the total amount of seconds the player has interacted with it.</returns>
        public List<(string, float)> GetInteractionsList()
        {
            List<(string, float)> interactionsList = new List<(string, float)>();

            //Go through the dictionary
            foreach(KeyValuePair<string, List<(System.DateTime, System.DateTime)>> interaction in interactions)
            {
                //Add all of the seconds from the current interaction and store it
                double seconds = 0;
                for(int i = 0; i < interaction.Value.Count; i++)
                    seconds += (interaction.Value[i].Item2 - interaction.Value[i].Item1).TotalSeconds;

                interactionsList.Add((interaction.Key, (float)seconds));
            }

            return interactionsList;
        }
    }
}
