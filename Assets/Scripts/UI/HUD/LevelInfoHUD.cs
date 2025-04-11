using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class LevelInfoHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("The text that shows the level name.")] private TextMeshProUGUI levelNameText;

        private void Start()
        {
            levelNameText.text = CampaignManager.Instance.GetCurrentLevelEvent().levelName;
        }
    }
}
