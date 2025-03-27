using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankInfoHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("The text for the tank name.")] private TextMeshProUGUI tankNameText;
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
        }
    }
}
