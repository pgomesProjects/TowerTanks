using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class Speedometer : MonoBehaviour
    {
        [SerializeField, Tooltip("The minimum and maximum angles for the arrow.")] private Vector2 angleConstraints;
        [SerializeField, Tooltip("The arrow RectTransform.")] private RectTransform arrow;
        [SerializeField, Tooltip("The speedometer text.")] private TextMeshProUGUI mphText;

        private TankController playerTank;
        private float currentSpeed, maxSpeed;

        /// <summary>
        /// Links the player tank to the speedometer.
        /// </summary>
        /// <param name="playerTank">The tank controller of the tank to monitor.</param>
        public void AssignTank(TankController playerTank)
        {
            this.playerTank = playerTank;
            maxSpeed = 160f;
        }

        public void AssignMaxSpeed(float maxSpeed)
        {
            this.maxSpeed = maxSpeed;
        }

        private void Update()
        {
            //If there is no tank, return
            if (playerTank == null)
                return;

            currentSpeed = playerTank.treadSystem.GetMPH();
            mphText.text = Mathf.FloorToInt(currentSpeed).ToString() + " MPH";

            float speedPercent = currentSpeed / maxSpeed;
            //Change the arrow's rotation based on its speed compared to the max speed
            arrow.transform.eulerAngles = new Vector3(0f, 0f, Mathf.Lerp(angleConstraints.x, angleConstraints.y, speedPercent));
        }
    }
}
