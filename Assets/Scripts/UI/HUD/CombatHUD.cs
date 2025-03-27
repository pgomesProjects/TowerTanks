using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace TowerTanks.Scripts
{
    public class CombatHUD : GameHUD
    {
        [Space]
        [SerializeField, Tooltip("The GameObject for the game over menu.")] private GameObject gameOverMenu;
        [SerializeField, Tooltip("The delay (in seconds) for the game over screen to show.")] private float gameOverDelay;
        [SerializeField, Tooltip("The controller for the session stats menu.")] private SessionStatsController sessionStatsMenu;

        [SerializeField, Tooltip("The alarm animation for incoming enemies.")] private AlarmAnimation alarmAnimation;
        [SerializeField, Tooltip("The text for the current enemy.")] private TextMeshProUGUI enemyText;
        [SerializeField, Tooltip("The characters per second type speed of the enemy name.")] private float charactersPerSecond = 20.0f;

        private TankController currentEnemy;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            LevelManager.OnGameOver += ShowGameOverScreen;
            LevelManager.OnCombatEnded += EndCombat;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            LevelManager.OnGameOver -= ShowGameOverScreen;
            LevelManager.OnCombatEnded -= EndCombat;
        }

        private async void ShowGameOverScreen()
        {
            gameOverMenu.SetActive(true);

            await Task.Delay(Mathf.CeilToInt(gameOverDelay * 1000));

            gameOverMenu.SetActive(false);
            sessionStatsMenu.gameObject.SetActive(true);
            Time.timeScale = 0.0f;
        }

        /// <summary>
        /// The function called to show the enemy alarm.
        /// </summary>
        private void ShowEnemyAlarm()
        {
            alarmAnimation.gameObject.SetActive(true);
        }

        /// <summary>
        /// The function called when combat has ended.
        /// </summary>
        private void EndCombat()
        {

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
