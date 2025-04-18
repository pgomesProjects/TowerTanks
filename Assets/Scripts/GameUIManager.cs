using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class GameUIManager : SerializedMonoBehaviour
    {
        private const string CANVAS_TAG_NAME = "GameUI";
        public enum UIType { TaskBar, RadialTaskBar, TimingGauge, RadialTimingGauge, ButtonPrompt, Damage, SymbolDisplay }
        public enum PromptDisplayType { Button, Text };

        [SerializeField, Tooltip("The prefabs of the different UI elements (only one of each type should exist).")] private UIPrefab[] uiPrefabs;
        [SerializeField, Tooltip("The button prompt settings.")] private ButtonPromptSettings buttonPromptSettings;
        [SerializeField, Tooltip("A list of the face button settings.")] private PlatformFaceButtonsSettings[] platformFaceButtonsSettings;

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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.TaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.GetComponent<RectTransform>().anchoredPosition = position;
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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.TaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.GetComponent<RectTransform>().anchoredPosition = position;
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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.RadialTaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.GetComponent<RectTransform>().anchoredPosition = position;
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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TaskProgressBar taskBar = Instantiate(GetPrefabFromList(UIType.RadialTaskBar), canvas.transform).GetComponent<TaskProgressBar>();
            taskBar.GetComponent<RectTransform>().anchoredPosition = position;
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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.TimingGauge), canvas.transform).GetComponent<TimingGauge>();
            timingGauge.GetComponent<RectTransform>().anchoredPosition = position;
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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            TimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.TimingGauge), canvas.transform).GetComponent<TimingGauge>();
            timingGauge.GetComponent<RectTransform>().anchoredPosition = position;
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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            RadialTimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.RadialTimingGauge), canvas.transform).GetComponent<RadialTimingGauge>();
            timingGauge.GetComponent<RectTransform>().anchoredPosition = position;
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
            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            RadialTimingGauge timingGauge = Instantiate(GetPrefabFromList(UIType.RadialTimingGauge), canvas.transform).GetComponent<RadialTimingGauge>();
            timingGauge.GetComponent<RectTransform>().anchoredPosition = position;
            timingGauge.ChangeColor(hitZoneColor, noHitZoneColor);
            timingGauge.CreateTimer(tickSpeed, minimumRange, maximumRange);
            return timingGauge;
        }

        /// <summary>
        /// Adds a button prompt to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the button prompt to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="imageSize">The size of the button image in pixels (applies to both width and height).</param>
        /// <param name="gameAction">The action to display the prompt for.</param>
        /// <param name="platform">The platform the game is being played on.</param>
        /// <param name="promptDisplayType">The type of display for the button prompt (button or text).</param>
        /// <param name="inGameWorld">Determines whether the canvas is to be treated as an overlay or in the game world.</param>
        /// <returns>Returns the GameObject created.</returns>
        public GameObject AddButtonPrompt(GameObject gameObject, Vector2 position, float imageSize, GameAction gameAction, PlatformType platform, PromptDisplayType promptDisplayType, bool inGameWorld)
        {
            //If there are no button settings, return null
            if (buttonPromptSettings == null)
                return null;

            Canvas canvas = GetCanvasFromGameObject(gameObject, inGameWorld);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            GameObject buttonPrompt = Instantiate(GetPrefabFromList(UIType.ButtonPrompt), canvas.transform);
            Image buttonImage = buttonPrompt.GetComponentInChildren<Image>();
            buttonPrompt.GetComponent<RectTransform>().anchoredPosition = position;
            buttonPrompt.GetComponent<RectTransform>().sizeDelta = new Vector2(imageSize, imageSize);
            TextMeshProUGUI buttonText = buttonPrompt.transform.Find("Text").GetComponent<TextMeshProUGUI>();

            PlatformPrompt currentPrompt = buttonPromptSettings.GetPlatformPrompt(gameAction, platform);

            if (currentPrompt.PromptSprite == null || promptDisplayType == PromptDisplayType.Text)
            {
                buttonImage.color = new Color(0, 0, 0, 0);
                buttonText.text = currentPrompt.GetPromptText();
            }
            else
            {
                if (buttonImage.TryGetComponent(out ImagePlatformListener platformListener))
                    platformListener.UpdatePlatformPrompt(gameAction);
                else
                    buttonImage.sprite = currentPrompt.PromptSprite;
            }

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
            Canvas canvas = GetCanvasFromGameObject(gameObject, true);

            //If no canvas is found, return
            if (canvas == null)
                return;

            GameObject damageObj = Instantiate(GetPrefabFromList(UIType.Damage), canvas.transform);
            damageObj.GetComponent<RectTransform>().anchoredPosition = position;
            TextMeshProUGUI damageText = damageObj.GetComponentInChildren<TextMeshProUGUI>();
            damageText.color = textColor;
            damageText.text = damage.ToString("F0");

            LeanTween.scale(gameObject, Vector3.one + (Vector3.one * 1.2f), 0.2f).setEase(LeanTweenType.punch);

            Destroy(damageObj, duration);
        }

        /// <summary>
        /// Displays a symbol with text on a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to add the timing gauge to (must have a canvas).</param>
        /// <param name="position">The position of the UI object relative to the GameObject.</param>
        /// <param name="symbolSprite">The sprite to show on the display.</param>
        /// <param name="displayText">The initial text to show on the display.</param>
        /// <returns>Returns the symbol display created.</returns>
        public SymbolDisplay AddSymbolDisplay(GameObject gameObject, Vector2 position, Sprite symbolSprite, string displayText, Color textColor)
        {
            Canvas canvas = GetCanvasFromGameObject(gameObject, true);

            //If no canvas is found, return null
            if (canvas == null)
                return null;

            SymbolDisplay symbolObj = Instantiate(GetPrefabFromList(UIType.SymbolDisplay), canvas.transform).GetComponent<SymbolDisplay>();
            symbolObj.GetComponent<RectTransform>().anchoredPosition = position;
            symbolObj.Init(symbolSprite, displayText, textColor);

            return symbolObj;
        }

        /// <summary>
        /// Gets the canvas with the standard UI tag name on a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to check the canvas of.</param>
        /// <param name="inGameWorld">If true, the canvas is in the game world. If false, the canvas is in the UI.</param>
        /// <returns>Returns a found or newly created canvas component.</returns>
        private Canvas GetCanvasFromGameObject(GameObject gameObject, bool inGameWorld)
        {
            //Find the canvas on the GameObject
            foreach (Canvas canvas in gameObject.GetComponentsInChildren<Canvas>())
            {
                if (canvas.tag == CANVAS_TAG_NAME)
                {
                    if (canvas.GetComponent<GyroscopeComponent>() == null)
                        canvas.gameObject.AddComponent<GyroscopeComponent>();

                    canvas.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    return canvas;
                }
            }

            //If there is no canvas, create one
            GameObject newCanvasObject = new GameObject("InGameCanvas");
            newCanvasObject.transform.SetParent(gameObject.transform);
            Canvas newCanvas = newCanvasObject.AddComponent<Canvas>();
            newCanvasObject.AddComponent<GyroscopeComponent>();

            //Settings
            newCanvas.renderMode = inGameWorld ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            newCanvas.GetComponent<RectTransform>().localScale = Vector3.one * (inGameWorld ? 0.01f : 1f);
            newCanvas.tag = CANVAS_TAG_NAME;
            newCanvas.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
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

        /// <summary>
        /// Gets a set of face buttons for a specific platform.
        /// </summary>
        /// <param name="platformType">The platform to display for.</param>
        /// <returns>The prompt info for all of the face buttons.</returns>
        public PlatformFaceButtonsSettings GetPlatformFaceButtons(PlatformType platformType)
        {
            foreach(PlatformFaceButtonsSettings platformFaceButtons in platformFaceButtonsSettings)
            {
                //If the platform has been found in the list, return it
                if (platformFaceButtons.platformType == platformType)
                    return platformFaceButtons;
            }

            //If not, just use the first platform settings
            return platformFaceButtonsSettings[0];
        }

        public ButtonPromptSettings GetButtonPromptSettings() => buttonPromptSettings;
    }
}
