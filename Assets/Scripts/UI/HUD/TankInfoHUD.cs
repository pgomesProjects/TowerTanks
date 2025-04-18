using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankInfoHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("The text for the tank name.")] private TextMeshProUGUI tankNameText;
        [SerializeField, Tooltip("The lost connection screen.")] private RectTransform lostConnectionScreen;
        private HeatmapGenerator tankHeatmap;
        private Speedometer tankSpeedometer;

        private void Awake()
        {
            tankHeatmap = GetComponentInChildren<HeatmapGenerator>();
            tankSpeedometer = GetComponentInChildren<Speedometer>();
        }

        private void OnEnable()
        {
            TankManager.OnPlayerTankAssigned += InitializeTankInfo;
            TankManager.OnPlayerTankDying += RemoveTankInfo;
        }

        private void OnDisable()
        {
            TankManager.OnPlayerTankAssigned -= InitializeTankInfo;
        }

        /// <summary>
        /// Initializes the tank information onto the screen.
        /// </summary>
        /// <param name="playerTank">The player tank.</param>
        public void InitializeTankInfo(TankController playerTank)
        {
            tankNameText.text = playerTank.TankName;
            
            tankHeatmap?.AssignTank(playerTank);

            tankSpeedometer?.AssignTank(playerTank);
            lostConnectionScreen.gameObject.SetActive(false);
        }

        private void RemoveTankInfo()
        {
            lostConnectionScreen?.gameObject.SetActive(true);
            tankHeatmap?.ClearMap();
        }
    }
}
