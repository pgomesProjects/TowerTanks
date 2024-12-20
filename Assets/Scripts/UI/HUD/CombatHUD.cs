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

        [SerializeField, Tooltip("The name of the player tank in the health bar.")] private TextMeshProUGUI healthPlayerTankName;
        [SerializeField, Tooltip("The health bar for the player tank's core.")] private ProgressBar playerCoreHealth;
        [SerializeField, Tooltip("The alarm animation for incoming enemies.")] private AlarmAnimation alarmAnimation;
        [SerializeField, Tooltip("The text for the current enemy.")] private TextMeshProUGUI enemyText;
        [SerializeField, Tooltip("The canvas group for the current enemy info.")] private CanvasGroup enemyCanvasGroup;
        [SerializeField, Tooltip("The characters per second type speed of the enemy name.")] private float charactersPerSecond = 20.0f;
        [SerializeField, Tooltip("The health bar mask RectTransform.")] private RectTransform healthBarMask;
        [SerializeField, Tooltip("The time it takes for the health bar to completely show.")] private float healthBarAniDuration = 1.5f;

        private ProgressBar enemyProgressBar;
        private TankController currentEnemy;
        private Vector2 healthBarSize;

        protected override void Awake()
        {
            base.Awake();
            enemyProgressBar = enemyCanvasGroup.GetComponentInChildren<ProgressBar>();
            healthBarSize = healthBarMask.sizeDelta;
            enemyCanvasGroup.alpha = 0f;
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
            TankManager.OnPlayerTankAssigned += CreatePlayerTankHealth;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            LevelManager.OnGameOver -= ShowGameOverScreen;
            LevelManager.OnCombatEnded -= EndCombat;
            TankManager.OnPlayerTankAssigned -= CreatePlayerTankHealth;
        }

        private void CreatePlayerTankHealth(TankController playerTank)
        {
            playerTank.OnCoreDamaged += UpdatePlayerTankHealth;
            healthPlayerTankName.text = playerTank.TankName + "'s Health:";
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

        public void DisplayEnemyTankInformation(TankController newTank)
        {
            currentEnemy = newTank;
            currentEnemy.OnCoreDamaged += UpdateEnemyTankHealth;
            enemyCanvasGroup.alpha = 1f;
            StartCoroutine(TypeEnemyName(newTank.TankName));
            StartCoroutine(ShowHealthBar());
        }

        private IEnumerator TypeEnemyName(string enemyName)
        {
            string enemyNameString = "";
            enemyText.text = enemyNameString;

            int characterCounter = 0;
            while (enemyNameString != enemyName)
            {
                enemyNameString += enemyName[characterCounter];
                enemyText.text = enemyNameString;
                yield return new WaitForSeconds(1.0f / charactersPerSecond);
                characterCounter++;
            }
        }

        private IEnumerator ShowHealthBar()
        {
            Vector2 healthBarWidth = new Vector2(0, healthBarSize.y);
            healthBarMask.sizeDelta = healthBarWidth;
            enemyProgressBar.OverrideProgressBar(1.0f);

            float elapsedTime = 0f;

            while (elapsedTime / healthBarAniDuration < 1)
            {
                elapsedTime += Time.deltaTime;

                float t = elapsedTime / healthBarAniDuration;
                healthBarWidth.x = Mathf.Lerp(0, healthBarSize.x, t);
                healthBarMask.sizeDelta = healthBarWidth;
                yield return null;
            }
        }

        private void UpdatePlayerTankHealth(float amount)
        {
            playerCoreHealth.UpdateProgressValue(amount);

            if (amount <= 0)
                playerCoreHealth.gameObject.SetActive(false);
        }

        private void UpdateEnemyTankHealth(float amount)
        {
            enemyProgressBar.UpdateProgressValue(amount);

            if (amount <= 0)
                RemoveEnemyTankInformation();
        }

        private void RemoveEnemyTankInformation()
        {
            enemyCanvasGroup.alpha = 0f;
            currentEnemy.OnCoreDamaged -= UpdateEnemyTankHealth;
            currentEnemy = null;
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
