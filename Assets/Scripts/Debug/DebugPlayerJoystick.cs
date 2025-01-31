using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TowerTanks.Scripts
{
    public class DebugPlayerJoystick : MonoBehaviour
    {
        [SerializeField, Tooltip("The center transform.")] private RectTransform centerTransform;
        [SerializeField, Tooltip("The joystick position transform.")] private RectTransform joystickTransform;
        [SerializeField, Tooltip("The radius of the joystick circle.")] private float radius;
        [SerializeField, Tooltip("The refresh rate for the angle check.")] private float angleRefreshRate;
        [SerializeField, Tooltip("The player information text.")] private TextMeshProUGUI playerInfo;

        private PlayerData currentPlayer;
        private Vector2 lastJoystickPos;
        private Vector2 joystickPos;
        private float angle;
        private float currentRefreshRate;

        private void Awake()
        {
            currentRefreshRate = angleRefreshRate;
        }

        public void LinkPlayerData(PlayerData playerData)
        {
            currentPlayer = playerData;
        }

        private void Update()
        {
            if (currentPlayer == null || !GameSettings.debugMode)
                return;

            joystickPos = new Vector2(currentPlayer.playerMovementData.x, currentPlayer.playerMovementData.y) * radius;
            joystickTransform.anchoredPosition = joystickPos;

            if (currentRefreshRate >= angleRefreshRate)
            {
                angle = Vector2.SignedAngle(lastJoystickPos, currentPlayer.playerMovementData);
                currentRefreshRate = 0f;
                lastJoystickPos = currentPlayer.playerMovementData;
            }
            else
                currentRefreshRate += Time.unscaledDeltaTime;

            UpdateText(currentPlayer.GetPlayerName(), currentPlayer.playerMovementData, angle);
        }

        private void UpdateText(string playerName, Vector2 movementData, float angle)
        {
            playerInfo.text = playerName + " Information<br>X: " + movementData.x.ToString("F2") + "<br>Y: " + movementData.y.ToString("F2") + "<br>Angle: " + angle.ToString("F2");

            if (angle > 0)
                playerInfo.text += "<br>Spin: Counter-Clockwise";
            else if(angle < 0)
                playerInfo.text += "<br>Spin: Clockwise";
            else
                playerInfo.text += "<br>No Spin Detected";
        }
    }
}
