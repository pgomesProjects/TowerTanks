using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CombatHUD : GameHUD
{
    [Space]
    [SerializeField, Tooltip("The GameObject for the game over menu.")] private GameObject gameOverMenu;
    [SerializeField, Tooltip("The delay (in seconds) for the game over screen to show.")] private float gameOverDelay;
    [SerializeField, Tooltip("The controller for the session stats menu.")] private SessionStatsController sessionStatsMenu;

    [SerializeField, Tooltip("The alarm animation for incoming enemies.")] private AlarmAnimation alarmAnimation;

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
        EnemySpawnManager.OnEnemyInRange += ShowEnemyAlarm;
        LevelManager.OnCombatEnded += EndCombat;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        LevelManager.OnGameOver -= ShowGameOverScreen;
        EnemySpawnManager.OnEnemyInRange -= ShowEnemyAlarm;
        LevelManager.OnCombatEnded -= EndCombat;
    }

    private async void ShowGameOverScreen()
    {
        gameOverMenu.SetActive(true);

        await Task.Delay(Mathf.CeilToInt(gameOverDelay * 1000));

        gameOverMenu.SetActive(false);
        sessionStatsMenu.gameObject.SetActive(true);
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
