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

        [Title("Interactable Weights")]
        [InfoBox("Key: Interactable type to populate with tokens, \nValue: percentage of our tokens to give to this interactable type.")]

        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight")]
        public Dictionary<INTERACTABLE, float> patrolStateInteractableWeights = new();
        
        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight")]
        public Dictionary<INTERACTABLE, float> pursueStateInteractableWeights = new();
        
        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight")]
        public Dictionary<INTERACTABLE, float> engageStateInteractableWeights = new();


        public Dictionary<INTERACTABLE, int> GetTokenDistribution(Dictionary<INTERACTABLE, float> weights)
        {
            float totalWeight = weights.Values.Sum();
            Dictionary<INTERACTABLE, int> tokensToDistribute = new Dictionary<INTERACTABLE, int>();

            // distributes tokens proportionally based on weights
            foreach (var kvp in weights)
            {
                if (kvp.Value > 0)
                {
                    float percentage = kvp.Value / totalWeight;
                    int tokens = Mathf.FloorToInt(tankEconomy * percentage);
                    tokensToDistribute[kvp.Key] = tokens;
                }
            }

            int remainingTokens = tankEconomy - tokensToDistribute.Values.Sum();

            // accounts for any leftover tokens from rounding down
            while (remainingTokens > 0)
            {
                foreach (var kvp in weights.OrderByDescending(kvp => kvp.Value)) // distributes leftover tokens to the interactables with the highest weights
                {
                    if (kvp.Value == 0) continue;
                    if (remainingTokens <= 0) break;
                    tokensToDistribute[kvp.Key]++;
                    remainingTokens--;
                }
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