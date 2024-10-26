using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class GameUIManager : SerializedMonoBehaviour
    {
        private const string CANVAS_TAG_NAME = "GameUI";
        public enum UIType { TaskBar, RadialTaskBar, TimingGauge, RadialTimingGauge, ButtonPrompt, Damage }

        [SerializeField, Tooltip("The prefabs of the different UI elements (only one of each type should exist).")] private UIPrefab[] uiPrefabs;
        [SerializeField, Tooltip("The button prompt settings.")] private ButtonPromptSettings buttonPromptSettings;

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

        private PlayerControlSystem playerControls;

        private void Awake()
        {
            isVisible = true;
            playerControls = new PlayerControlSystem();
            playerControls.Debug.ToggleUI.started += _ => DebugToggleGameUI();
        }

        private void OnEnable()
        {
            playerControls?.Enable();
        }

        private void OnDisable()
        {
            playerControls?.Disable();
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
            {
                CanvasGroup canvasGroup = gameMultiplayerUI.GetComponent<CanvasGroup>();

                //Adds a CanvasGroup if not already added
                if (canvasGroup == null)
                {
                    canvasGroup = gameMultiplayerUI.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }

                gameMultiplayerUI.GetComponent<CanvasGroup>().alpha = isVisible ? 1 : 0;
            }

            CombatHUD combatHUD = FindObjectOfType<CombatHUD>();
            if (combatHUD != null)
                combatHUD.GetComponent<CanvasGroup>().alpha = isVisible ? 1 : 0;
        }

        /// <summary>
        /// Adds a task bar to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the task bar to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="duration">The duration of the task.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the task bar created.</returns>
        public TaskProgressBar AddTaskBar(GameObject gameObject, Vector2 position, float duration, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

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
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="duration">The duration of the task.</param>
        /// <param name="barColor">The color of the task bar.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the task bar created.</returns>
        public TaskProgressBar AddTaskBar(GameObject gameObject, Vector2 position, float duration, Color barColor, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.TaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.ChangeColor(barColor);
            taskBar.StartTask(duration);
            return taskBar;
        }

        /// <summary>
        /// Adds a radial task bar to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the task bar to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="duration">The duration of the task.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the task bar created.</returns>
        public TaskProgressBar AddRadialTaskBar(GameObject gameObject, Vector2 position, float duration, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.RadialTaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.StartTask(duration);
            return taskBar;
        }

        /// <summary>
        /// Adds a radial task bar to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the task bar to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="duration">The duration of the task.</param>
        /// <param name="barColor">The color of the task bar.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the task bar created.</returns>
        public TaskProgressBar AddRadialTaskBar(GameObject gameObject, Vector2 position, float duration, Color barColor, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.RadialTaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.ChangeColor(barColor);
            taskBar.StartTask(duration);
            return taskBar;
        }

        /// <summary>
        /// Adds a timing gauge to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="tickSpeed">The time it takes for the tick bar to go from one end to another (in seconds).</param>
        /// <param name="minimumRange">The minimum value in the zone range.</param>
        /// <param name="maximumRange">The maximum value in the zone range.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the timing gauge created.</returns>
        public TimingGauge AddTimingGauge(GameObject gameObject, Vector2 position, float tickSpeed, float minimumRange, float maximumRange, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.TimingGauge), canvas.transform).GetComponent<TimingGauge>();
            timingGauge.CreateTimer(tickSpeed, minimumRange, maximumRange);
            return timingGauge;
        }

        /// <summary>
        /// Adds a timing gauge to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="tickSpeed">The time it takes for the tick bar to go from one end to another (in seconds).</param>
        /// <param name="minimumRange">The minimum value in the zone range.</param>
        /// <param name="maximumRange">The maximum value in the zone range.</param>
        /// <param name="hitZoneColor">The color of the zone to hit.</param>
        /// <param name="noHitZoneColor">The color of the zone to not hit.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the timing gauge created.</returns>
        public TimingGauge AddTimingGauge(GameObject gameObject, Vector2 position, float tickSpeed, float minimumRange, float maximumRange, Color hitZoneColor, Color noHitZoneColor, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.TimingGauge), canvas.transform).GetComponent<TimingGauge>();
            timingGauge.ChangeColor(hitZoneColor, noHitZoneColor);
            timingGauge.CreateTimer(tickSpeed, minimumRange, maximumRange);
            return timingGauge;
        }

        /// <summary>
        /// Adds a radial timing gauge to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="tickSpeed">The time it takes for the tick bar to go from one end to another (in seconds).</param>
        /// <param name="minimumRange">The minimum value in the zone range.</param>
        /// <param name="maximumRange">The maximum value in the zone range.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the timing gauge created.</returns>
        public RadialTimingGauge AddRadialTimingGauge(GameObject gameObject, Vector2 position, float tickSpeed, float minimumRange, float maximumRange, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            RadialTimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.RadialTimingGauge), canvas.transform).GetComponent<RadialTimingGauge>();
            timingGauge.CreateTimer(tickSpeed, minimumRange, maximumRange);
            return timingGauge;
        }

        /// <summary>
        /// Adds a radial timing gauge to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="tickSpeed">The time it takes for the tick bar to go from one end to another (in seconds).</param>
        /// <param name="minimumRange">The minimum value in the zone range.</param>
        /// <param name="maximumRange">The maximum value in the zone range.</param>
        /// <param name="hitZoneColor">The color of the zone to hit.</param>
        /// <param name="noHitZoneColor">The color of the zone to not hit.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the timing gauge created.</returns>
        public RadialTimingGauge AddRadialTimingGauge(GameObject gameObject, Vector2 position, float tickSpeed, float minimumRange, float maximumRange, Color hitZoneColor, Color noHitZoneColor, bool inGameWorld)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            RadialTimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.RadialTimingGauge), canvas.transform).GetComponent<RadialTimingGauge>();
            timingGauge.ChangeColor(hitZoneColor, noHitZoneColor);
            timingGauge.CreateTimer(tickSpeed, minimumRange, maximumRange);
            return timingGauge;
        }

        /// <summary>
        /// Adds a button prompt to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="gameAction">The action to display the prompt for.</param>
        /// <param name="platform">The platform the game is being played on.</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the GameObject created.</returns>
        public GameObject AddButtonPrompt(GameObject gameObject, Vector2 position, GameAction gameAction, PlatformType platform, bool inGameWorld)
        {
            //If there are no button settings, return null
            if (buttonPromptSettings == null)
                return null;

            Canvas canvas = GetCanvasFromGameObject(gameObject, position, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            GameObject buttonPrompt = Instantiate(GetPrefabFromList(UIType.ButtonPrompt), canvas.transform);
            Image buttonImage = buttonPrompt.GetComponentInChildren<Image>();
            TextMeshProUGUI buttonText = buttonPrompt.GetComponentInChildren<TextMeshProUGUI>();

            PlatformPrompt currentPrompt = buttonPromptSettings.GetButtonPrompt(gameAction, platform);

            if (currentPrompt.PromptSprite == null)
                buttonImage.color = new Color(0, 0, 0, 0);
            else
                buttonImage.sprite = currentPrompt.PromptSprite;

            buttonText.text = currentPrompt.PromptText;

            return buttonPrompt.gameObject;
        }

        /// <summary>
        /// Displays a number on top of a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="textColor">The color of the damage text.</param>
        /// <param name="damage">The amount of damage taken by the GameObject.</param>
        /// <param name="duration">The amount of time the text should last on the screen for.</param>
        public void AddDamageNumber(GameObject gameObject, Vector2 position, Color textColor, float damage, float duration)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, position, true);

            //If no canvas is found, return
            if (canvas == null)
                return;

            GameObject damageObj = Instantiate(GetPrefabFromList(UIType.Damage), canvas.transform);
            TextMeshProUGUI damageText = damageObj.GetComponentInChildren<TextMeshProUGUI>();
            damageText.color = textColor;
            damageText.text = damage.ToString("F0");

            LeanTween.scale(gameObject, Vector3.one + (Vector3.one * 1.2f), 0.2f).setEase(LeanTweenType.punch);

            Destroy(damageObj, duration);
        }

        /// <summary>
        /// Gets the canvas with the standard UI tag name on a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to check the canvas of.</param>
        /// <param name="position">The position of the canvas.</param>
        /// <param name="inGameWorld">If true, the canvas is in the game world. If false, the canvas is in the UI.</param>
        /// <returns>Returns a found or newly created canvas component.</returns>
        private Canvas GetCanvasFromGameObject(GameObject gameObject, Vector2 position, bool inGameWorld)
        {
            //Find the canvas on the GameObject
            foreach (Canvas canvas in gameObject.GetComponentsInChildren<Canvas>())
            {
                if (canvas.tag == CANVAS_TAG_NAME)
                {
                    canvas.GetComponent<RectTransform>().anchoredPosition = position;
                    return canvas;
                }
            }

            //If there is no canvas, create one
            GameObject newCanvasObject = new GameObject("InGameCanvas");
            newCanvasObject.transform.SetParent(gameObject.transform);
            Canvas newCanvas = newCanvasObject.AddComponent<Canvas>();

            //Settings
            newCanvas.renderMode = inGameWorld ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            newCanvas.GetComponent<RectTransform>().localScale = Vector3.one * (inGameWorld ? 0.01f : 1f);
            newCanvas.tag = CANVAS_TAG_NAME;
            newCanvas.GetComponent<RectTransform>().anchoredPosition = position;
            newCanvas.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            if (inGameWorld)
            {
                newCanvas.sortingLayerName = "Foreground";
                newCanvas.sortingOrder = 0;
            }

            return newCanvas;
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
