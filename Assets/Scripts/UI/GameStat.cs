using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TowerTanks.Scripts
{
    public class GameStat : MonoBehaviour
    {
        [SerializeField, Tooltip("The stat header text.")] private TextMeshProUGUI statHeaderText;
        [SerializeField, Tooltip("The stat data text.")] private TextMeshProUGUI statDataText;

        /// <summary>
        /// Adds data to the game stat component text.
        /// </summary>
        /// <param name="header">The header information.</param>
        /// <param name="data">The data information.</param>
        public void AddData(string header, string data)
        {
            statHeaderText.text = header + ":";
            statDataText.text = data;
        }
    }
}
