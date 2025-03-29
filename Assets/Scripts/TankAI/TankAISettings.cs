using System;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Internal;
#if UNITY_EDITOR 
using UnityEditor; 
using Sirenix.OdinInspector.Editor;
#endif

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "TankAISettings", menuName = "ScriptableObjects/TankAISettings", order = 1)]
    public class TankAISettings : SerializedScriptableObject
    {
        public float viewRange;
        public float maxEngagementRange;
        public float preferredFightDistance;
        public int tankEconomy;
        [Range(0, 100)]
        [Tooltip("The accuracy of the tank's aim. 0 is completely inaccurate, 100 is perfectly accurate, in theory.")]
        public float tankAccuracy = 100; //default value is complete accuracy

        [Range(0, 1)]
        [Tooltip("The amount of time to take between shots. 0% aggression won't shoot weapons at all, 1% will use " +
                 "the max fire cooldown, 100% will have no cooldown.")]
        public float aggression = 1;
        
        [Tooltip("With extremely small aggression, this cooldown will be used. At .5 aggression, half this value will be used. At 100% aggression, this value is basically just ignored, and no cooldown is used.")]
        public float maxFireCooldown;

        [Tooltip("Offset used to make aggression pattern more randomized. Higher value = more range of values for aggro cooldown.")]
        public float aggressionCooldownOffset;

        [Title("Interactable Weights")]
        [InfoBox("Key: Interactable type to populate with tokens, \nValue: percentage of our tokens to give to this interactable type.")]

        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight")]
        public Dictionary<INTERACTABLE, float> patrolStateInteractableWeights = new();
        
        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight")]
        public Dictionary<INTERACTABLE, float> pursueStateInteractableWeights = new();
        
        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight")]
        public Dictionary<INTERACTABLE, float> engageStateInteractableWeights = new();

        
        /// <summary>
        /// Will return a dictionary containing how many tokens to give to interactables.
        /// KEY: Interactable Type
        /// VALUE: Tokens to give to this type
        /// </summary>
        /// <param name="weights"></param>
        /// <returns></returns>
        public Dictionary<INTERACTABLE, int> GetTokenDistribution(Dictionary<INTERACTABLE, float> weights)
        {
            float totalWeight = weights.Values.Sum();
            Dictionary<INTERACTABLE, int> tokensToDistribute = new Dictionary<INTERACTABLE, int>();
    
            if (totalWeight == 0)
            {
                // if no weights are assigned, return a dictionary with zero tokens
                foreach (var kvp in weights)
                {
                    tokensToDistribute[kvp.Key] = 0;
                }
                return tokensToDistribute;
            }

            // we are now saving the integer and remainder of each raw token distribution to implement fair share allocation
            Dictionary<INTERACTABLE, (int integerPart, float remainder)> tokenParts = new();
            int allocatedTokens = 0;

            foreach (var kvp in weights)
            {
                float exactTokens = (kvp.Value / totalWeight) * tankEconomy;
                int integerPart = Mathf.FloorToInt(exactTokens);
                float remainder = exactTokens - integerPart; // Store remainder

                tokenParts[kvp.Key] = (integerPart, remainder);
                tokensToDistribute[kvp.Key] = integerPart; // Start with using the integer part to give tokens
                allocatedTokens += integerPart;
            }

            // if there are remaining tokens leftover from rounding, distribute them based on the largest remainders
            int remainingTokens = tankEconomy - allocatedTokens;
            var sortedByRemainder = tokenParts.OrderByDescending(kvp => kvp.Value.remainder).Select(kvp => kvp.Key).ToList();

            for (int i = 0; i < remainingTokens; i++)
            {
                tokensToDistribute[sortedByRemainder[i]]++;
            }

            return tokensToDistribute;
        }
        
        #if UNITY_EDITOR
        [OnInspectorGUI]
        private void DisplayTokenDistribution()
        {
            DisplayTokenDistributionForDictionary(patrolStateInteractableWeights, "Patrol State");
            DisplayTokenDistributionForDictionary(pursueStateInteractableWeights, "Pursue State");
            DisplayTokenDistributionForDictionary(engageStateInteractableWeights, "Engage State");
        }

        
        private void DisplayTokenDistributionForDictionary(Dictionary<INTERACTABLE, float> dictionary, string stateName)
        {
            if (dictionary == null || dictionary.Count == 0) return;

            GUILayout.Space(10);
            GUILayout.Label($"{stateName} Token Distribution", EditorStyles.boldLabel);
            
            // Display the token distribution
            foreach (var kvp in GetTokenDistribution(dictionary))
            {
                GUILayout.Label($"{kvp.Key}: {kvp.Value} tokens");
            }
        }
        #endif
        [PropertyOrder(1)]
        [Title("Engage State Settings")]
        [MinValue(1)]
        [Tooltip("While in engage state, the tank re-distributes it's tokens intermittently to it's distribution list every X seconds.")]
        public float redistributeTokensCooldown;
        
    }
}