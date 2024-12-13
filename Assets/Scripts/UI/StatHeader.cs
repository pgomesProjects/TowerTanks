using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TowerTanks.Scripts
{
    public class StatHeader : MonoBehaviour
    {
        [SerializeField, Tooltip("The text for the stat section header.")] private TextMeshProUGUI statSectionHeader;

        public void CreateSectionHeader(string sectionName)
        {
            statSectionHeader.text = "---" + sectionName + "---";
        }
    }
}
