using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

namespace TowerTanks.Scripts
{
    public class SessionStatsController : MonoBehaviour
    {
        [SerializeField, Tooltip("The prefab for the stat headers.")] private StatHeader statHeaderPrefab;
        [SerializeField, Tooltip("The prefab for the game stats.")] private GameStat gameStatPrefab;
        [SerializeField, Tooltip("The container for all of the stats.")] private RectTransform statsContainer;
        [SerializeField, Tooltip("The speed in which each text component shows up.")] private float displayStatSpeed;
        [SerializeField, Tooltip("The stats container scroll speed.")] private float statsScrollSpeed;
        [SerializeField, Tooltip("The stats container scroll velocity damping.")] private float statsScrollDamping;

        [SerializeField, Tooltip("The up arrow in the stats menu.")] private GameObject upArrow;
        [SerializeField, Tooltip("The down arrow in the stats menu.")] private GameObject downArrow;

        private PlayerControlSystem playerControlSystem;
        private bool canScroll;

        private float scrollVelocity;
        private float scrollDeltaY;

        private void Awake()
        {
            playerControlSystem = new PlayerControlSystem();
            playerControlSystem.UI.Submit.performed += _ => GoToMain();

            playerControlSystem.UI.Navigate.started += ctx => { scrollDeltaY = ctx.ReadValue<Vector2>().y; };
            playerControlSystem.UI.Navigate.performed += ctx => { scrollDeltaY = ctx.ReadValue<Vector2>().y; };
            playerControlSystem.UI.Navigate.canceled += ctx => { scrollDeltaY = 0f; };
        }

        private void OnEnable()
        {
            playerControlSystem.Enable();
            CreateSessionsData();
        }

        private void OnDisable()
        {
            playerControlSystem.Disable();
        }

        /// <summary>
        /// Creates the session data.
        /// </summary>
        private async void CreateSessionsData()
        {
            canScroll = false;
            ClearStats();

            //Let all players have input in the menu
            foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
                player.playerInput.SwitchCurrentActionMap("UI");

            upArrow.SetActive(false);
            downArrow.SetActive(true);
            statsContainer.anchoredPosition = new Vector2(0f, 0f);

            SessionStats sessionStats = GameManager.Instance.currentSessionStats;

            if (sessionStats == null)
                sessionStats = new SessionStats();

            Instantiate(statHeaderPrefab, statsContainer).CreateSectionHeader(SessionStats.tankHeader);
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Max Height", sessionStats.maxHeight.ToString("F2"));
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Rooms Built", sessionStats.roomsBuilt.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Total Cells", sessionStats.totalCells.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));

            Instantiate(statHeaderPrefab, statsContainer).CreateSectionHeader(SessionStats.resourcesHeader);
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Cargo Sold", sessionStats.cargoSold.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));

            Instantiate(statHeaderPrefab, statsContainer).CreateSectionHeader(SessionStats.interactablesHeader);
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Cannons Built", sessionStats.cannonsBuilt.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Machine Guns Built", sessionStats.machineGunsBuilt.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Mortars Built", sessionStats.mortarsBuilt.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Boilers Built", sessionStats.boilersBuilt.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Throttles Built", sessionStats.throttlesBuilt.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));

            Instantiate(statHeaderPrefab, statsContainer).CreateSectionHeader(SessionStats.enemiesHeader);
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));
            Instantiate(gameStatPrefab, statsContainer).AddData("Enemies Killed", sessionStats.enemiesKilled.ToString());
            await Task.Delay(Mathf.CeilToInt(displayStatSpeed * 1000f));

            canScroll = true;
            RefreshArrows();
        }

        private void Update()
        {
            if (scrollDeltaY != 0)
                scrollVelocity = -scrollDeltaY * statsScrollSpeed;

            if (Mathf.Abs(scrollVelocity) > 0.01f)
            {
                ScrollStats(scrollVelocity * Time.unscaledDeltaTime);
                scrollVelocity *= Mathf.Exp(-statsScrollDamping * Time.unscaledDeltaTime);
            }
        }

        /// <summary>
        /// Scrolls the stats menu so that more information can be viewed.
        /// </summary>
        /// <param name="deltaY">The y movement delta.</param>
        private void ScrollStats(float deltaY)
        {
            //If the stats cannot scroll, return
            if (!canScroll)
                return;

            //Move the stats container using the movement delta, scroll speed, and unscaled delta time
            float newStatsY = Mathf.Clamp(statsContainer.anchoredPosition.y + (deltaY * statsScrollSpeed * Time.unscaledDeltaTime), 0f, statsContainer.sizeDelta.y - 750f);
            statsContainer.anchoredPosition = new Vector2(0, newStatsY);

            RefreshArrows();
        }

        /// <summary>
        /// Refreshes the arrows UI which determines whether the arrows are active or not.
        /// </summary>
        private void RefreshArrows()
        {
            upArrow.SetActive(statsContainer.anchoredPosition.y > 0);
            downArrow.SetActive(statsContainer.sizeDelta.y - 750f > 0 && statsContainer.anchoredPosition.y < statsContainer.sizeDelta.y - 750f);
        }

        /// <summary>
        /// Clears any previous stats from the container.
        /// </summary>
        private void ClearStats()
        {
            foreach (Transform trans in statsContainer)
                Destroy(trans.gameObject);
        }

        /// <summary>
        /// Returns to the main menu.
        /// </summary>
        private void GoToMain()
        {
            canScroll = false;
            GameManager.Instance.LoadScene("Title", LevelTransition.LevelTransitionType.FADE);
            Time.timeScale = 1.0f;
        }
    }
}
