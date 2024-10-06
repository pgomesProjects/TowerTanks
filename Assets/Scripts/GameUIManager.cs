using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class GameUIManager : SerializedMonoBehaviour
    {
        private const string CANVAS_TAG_NAME = "GameUI";
        public enum UIType { TaskBar, TimingGauge }

        [SerializeField, Tooltip("The prefabs of the different UI elements (only one of each type should exist).")] private UIPrefab[] uiPrefabs;

        [System.Serializable]
        public class UIPrefab
        {
            public UIType type;
            public GameObject gameObject;

            public UIPrefab(UIType type, GameObject gameObject)
            {
                this.type = type;
                this.gameObject = gameObject;
            }
        }

        internal bool isVisible { get; private set; }

        [Button(ButtonSizes.Medium)]
        private void DebugToggleGameUI()
        {
            ToggleGameUI(!isVisible);
        }

        private void Awake()
        {
            isVisible = true;
        }

        /// <summary>
        /// Toggles the visibility of game UI.
        /// </summary>
        /// <param name="isVisible">If visible, true. If not visible, false.</param>
        public void ToggleGameUI(bool isVisible)
        {
            this.isVisible = isVisible;

            //Toggle the visibility of all in-game UI objects
            foreach (GameObject canvas in GameObject.FindGameObjectsWithTag(CANVAS_TAG_NAME))
            {
                CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();

                //Adds a CanvasGroup if not already added
                if (canvasGroup == null)
                {
                    canvasGroup = canvas.AddComponent<CanvasGroup>();
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }

                canvasGroup.alpha = isVisible ? 1 : 0;
            }

            //Toggle the visibility of all player names
            foreach (GameObject canvas in GameObject.FindGameObjectsWithTag("PlayerName"))
                canvas.GetComponent<CanvasGroup>().alpha = isVisible ? 1 : 0;

            //Toggle the visibility of any main UI objects
            GameMultiplayerUI gameMultiplayerUI = FindObjectOfType<GameMultiplayerUI>();
            if (gameMultiplayerUI != null)
                gameMultiplayerUI.GetComponent<CanvasGroup>().alpha = isVisible ? 1 : 0;

            CombatHUD combatHUD = FindObjectOfType<CombatHUD>();
            if (combatHUD != null)
                combatHUD.GetComponent<CanvasGroup>().alpha = isVisible ? 1 : 0;
        }

        /// <summary>
        /// Adds a task bar to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the task bar to (must have a canvas).</param>
        /// <param name="duration">The duration of the task.</param>
        /// <returns>Returns the task bar created.</returns>
        public TaskProgressBar AddTaskBar(GameObject gameObject, float duration)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject);

            //If no canvas is found, return
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.TaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.StartTask(duration);
            return taskBar;
        }

        /// <summary>
        /// Adds a task bar to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the task bar to (must have a canvas).</param>
        /// <param name="duration">The duration of the task.</param>
        /// <param name="barColor">The color of the task bar.</param>
        /// <returns>Returns the task bar created.</returns>
        public TaskProgressBar AddTaskBar(GameObject gameObject, float duration, Color barColor)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.TaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.ChangeColor(barColor);
            taskBar.StartTask(duration);
            return taskBar;
        }

        /// <summary>
        /// Adds a timing gauge to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="tickSpeed">The time it takes for the tick bar to go from one end to another (in seconds).</param>
        /// <param name="minimumRange">The minimum value in the zone range.</param>
        /// <param name="maximumRange">The maximum value in the zone range.</param>
        /// <returns>Returns the timing gauge created.</returns>
        public TimingGauge AddTimingGauge(GameObject gameObject, float tickSpeed, float minimumRange, float maximumRange)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.TimingGauge), canvas.transform).GetComponent<TimingGauge>();
            timingGauge.CreateTimer(tickSpeed, minimumRange, maximumRange);
            return timingGauge;
        }

        /// <summary>
        /// Gets the canvas with the standard UI tag name on a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to check the canvas of.</param>
        /// <returns>Returns a canvas component, if found. Otherwise, returns null.</returns>
        private Canvas GetCanvasFromGameObject(GameObject gameObject)
        {
            //Find the canvas on the GameObject
            foreach (Canvas canvas in gameObject.GetComponentsInChildren<Canvas>())
            {
                if (canvas.tag == CANVAS_TAG_NAME)
                    return canvas;
            }

            //If there is no canvas, return null
            Debug.LogError("Canvas could not be found on " + gameObject.name);
            return null;
        }

        /// <summary>
        /// Gets the prefab GameObject from the list of UI prefabs.
        /// </summary>
        /// <param name="type">The type of prefab to get.</param>
        /// <returns>Returns the GameObject associated with the type.</returns>
        private GameObject GetPrefabFromList(UIType type)
        {
            foreach (UIPrefab prefab in uiPrefabs)
            {
                if (prefab.type == type)
                    return prefab.gameObject;
            }
            return null;
        }
    }
}
