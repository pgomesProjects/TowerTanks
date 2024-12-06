using System;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;
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

        [Title("Interactable Weights")]
        [InfoBox("Key: Interactable type to populate with tokens, \nValue: percentage of our tokens to give to this interactable type.")]

        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight (0-100)")]
        public Dictionary<INTERACTABLE, float> patrolStateInteractableWeights = new();
        
        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight (0-100)")]
        public Dictionary<INTERACTABLE, float> pursueStateInteractableWeights = new();
        
        [DictionaryDrawerSettings(KeyLabel = "Interactable", ValueLabel = "Weight (0-100)")]
        public Dictionary<INTERACTABLE, float> engageStateInteractableWeights = new();

        

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

            foreach (var kvp in dictionary)
            {
                float percentage = kvp.Value / 100f;
                int tokens = Mathf.RoundToInt(tankEconomy * percentage);
                GUILayout.Label($"{kvp.Key}: {tokens} tokens");
            }
        }
        #endif
        [PropertyOrder(1)]
        [Title("Engage State Settings")]
        [MinValue(5)]
        public float redistributeTokensCooldown;
        
    }
}