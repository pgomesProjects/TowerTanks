using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerTanks.Scripts.Deprecated;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("The border that shows the player's color.")] private Image playerBorder;
        [SerializeField, Tooltip("The background behind the player's name.")] private Image playerNameBackground;
        [SerializeField, Tooltip("The player avatar.")] private Image playerAvatar;
        [SerializeField, Tooltip("The ending color for the character avatar when they have no more health")] private Color maxDamageColor = Color.red;
        [SerializeField, Tooltip("The image for when the player dies.")] private Image playerDeathImage;

        [SerializeField, Tooltip("The text for the player's name.")] private TextMeshProUGUI playerNameText;
        [SerializeField, Tooltip("The fuel bar.")] private ProgressBar fuelBar;

        [SerializeField, Tooltip("The animation curve for when the player HUD shakes.")] private AnimationCurve shakeIntensityCurve;

        [SerializeField, Tooltip("The respawn timer transform.")] private RectTransform respawnTransform;
        [SerializeField, Tooltip("The fill of the respawn timer.")] private Image respawnBar;
        [SerializeField, Tooltip("The text for the respawn timer.")] private TextMeshProUGUI respawnText;

        [Space]
        [SerializeField, Tooltip("The RectTransform of the minigame screen.")] private RectTransform miniGameRectTransform;
        [SerializeField, Tooltip("The duration of the minigame screen animation.")] private float miniGameScreenStartDuration;

        private Color playerColor;

        private Color startingColor;
        private RectTransform hudRectTransform;
        private Vector3 hudPosition;
        private float shakeTimer = 0f;
        private float currentShakeDuration;
        private float currentShakeAmount;

        private float startingMinigameScreenYPos;
        private bool isMinigameActive;

        [Button]
        private void DebugStartMinigame()
        {
            StartMinigame();
        }

        [Button]
        private void DebugStopMinigame()
        {
            StopMinigame();
        }

        private void Awake()
        {
            hudRectTransform = GetComponent<RectTransform>();
            startingColor = playerAvatar.color;
            startingMinigameScreenYPos = miniGameRectTransform.anchoredPosition.y;
            isMinigameActive = false;
            ShowRespawnTimer(false);
        }

        /// <summary>
        /// Initializes the player HUD to its default stats.
        /// </summary>
        /// <param name="characterIndex">The index number for the character.</param>
        /// <param name="playerName">The name of the player.</param>
        public void InitializeHUD(int characterIndex, string playerName = "")
        {
            playerColor = GameManager.Instance.MultiplayerManager.GetPlayerColors()[characterIndex];
            transform.name = playerName + "HUD";
            hudRectTransform.anchoredPosition = new Vector2((hudRectTransform.sizeDelta.x + 35f) * characterIndex, 0f);
            hudPosition = hudRectTransform.localPosition;

            playerNameBackground.color = playerColor;
            playerBorder.color = playerColor;
            playerNameText.text = playerName;
            playerDeathImage.color = new Color(0, 0, 0, 0);
        }

        private void Update()
        {
            CheckForHUDShake();
        }

        private void CheckForHUDShake()
        {
            if (shakeTimer > 0)
            {
                // Calculate shake amount using Perlin noise and animation curve
                float shakeProgress = 1 - Mathf.Clamp01(shakeTimer / currentShakeDuration);
                float shakeIntensity = shakeIntensityCurve.Evaluate(shakeProgress);

                // Calculate separate Perlin noise values for X and Y axes
                float noiseX = Mathf.PerlinNoise(Time.time * currentShakeAmount, 0f);
                float noiseY = Mathf.PerlinNoise(0f, Time.time * currentShakeAmount);

                // Map noise values to X and Y offsets
                float offsetX = (noiseX * 2 - 1) * currentShakeAmount;
                float offsetY = (noiseY * 2 - 1) * currentShakeAmount;

                // Apply the shake to the RectTransform's position
                hudRectTransform.localPosition = hudPosition + new Vector3(offsetX, offsetY, 0f) * shakeIntensity;

                // Decrease shake timer
                shakeTimer -= Time.deltaTime;
            }
            else
            {
                // Reset position after shake duration is over
                hudRectTransform.localPosition = hudPosition;
            }
        }

        /// <summary>
        /// Updates the fuel bar on the player HUD.
        /// </summary>
        /// <param name="fuelBarPercentage">The percentage of fuel (from 0 to 100).</param>
        public void UpdateFuelBar(float fuelBarPercentage)
        {
            fuelBar.UpdateProgressValue(fuelBarPercentage);
        }

        public void ShakePlayerHUD(float shakeDuration, float shakeAmount)
        {
            currentShakeDuration = shakeDuration;
            currentShakeAmount = shakeAmount;
            shakeTimer = currentShakeDuration;
        }

        /// <summary>
        /// Gradually changes the color of the player avatar's image from the starting color to red over a specified duration.
        /// </summary>
        /// <param name="percentage">The percentage between the starting color and red (0 to 1).</param>
        /// <param name="duration">The amount of time to take to reach the specified percentage.</param>
        public void DamageAvatar(float percentage, float duration)
        {
            StartCoroutine(ChangeAvatarColorCoroutine(percentage, duration));
        }

        private IEnumerator ChangeAvatarColorCoroutine(float percentage, float duration)
        {
            Color startColor = playerAvatar.color;
            Color targetColor = Color.Lerp(startingColor, maxDamageColor, percentage);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                // Calculate the interpolation factor based on the elapsed time and duration
                float t = elapsedTime / duration;

                // Interpolate between the start color and the target color based on the percentage
                Color lerpedColor = Color.Lerp(startColor, targetColor, percentage);

                // Apply the interpolated color to the player avatar's image
                playerAvatar.color = lerpedColor;

                // Wait for the next frame
                yield return null;

                // Update the elapsed time
                elapsedTime += Time.deltaTime;
            }

            // Ensure the avatar color ends up exactly at the target color
            playerAvatar.color = targetColor;
        }

        public void StartMinigame()
        {
            isMinigameActive = true;
            LeanTween.moveY(miniGameRectTransform, 0f, miniGameScreenStartDuration);
        }

        public void StopMinigame()
        {
            isMinigameActive = false;
            LeanTween.moveY(miniGameRectTransform, startingMinigameScreenYPos, miniGameScreenStartDuration);
        }

        public void ShowRespawnTimer(bool showTimer)
        {
            respawnTransform.gameObject.SetActive(showTimer);
        }

        public void UpdateRespawnBar(float respawnFill, float time)
        {
            respawnBar.fillAmount = respawnFill;
            respawnText.text = (Mathf.Ceil(time)).ToString();
        }

        public void KillPlayerHUD()
        {
            playerDeathImage.color = new Color(1, 1, 1, 1);
        }
    }
}
