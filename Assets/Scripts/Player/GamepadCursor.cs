using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class GamepadCursor : MonoBehaviour
    {
        [SerializeField, Tooltip("The Gamepad Cursor asset.")] private RectTransform cursorTransform;
        [SerializeField, Tooltip("The speed of the cursor movement.")] private float cursorSpeed = 1000f;
        [SerializeField, Tooltip("The screen padding for the cursor.")] private float padding = 50f;
        [SerializeField, Tooltip("The Gamepad Cursor settings.")] private GamepadCursorSettings gamepadCursorSettings;

        //Components
        private Camera mainCamera;
        private PlayerInput playerInput;

        //Cursor variables
        private Mouse virtualMouse;
        private Vector2 cursorHotSpot = Vector2.zero;
        private CursorMode cursorMode = CursorMode.Auto;
        private GamepadCursorState currentCursorState;
        private bool cursorActive;
        private bool cursorCanMove;

        //Selection variables
        private bool previousButtonSouthState;
        private GamepadSelectable lastHoveredObject;

        //Visual variables
        private Canvas canvas;
        private RectTransform canvasRectTransform;
        private RectTransform localGamepadCursorTransform;
        private Image cursorHand, cursorHandOutline;
        private TextMeshProUGUI cursorText;
        private Color cursorColor;

        private void OnEnable()
        {
            //If there is no player connected, return
            if (playerInput == null)
                return;

            Init(playerInput);
        }

        /// <summary>
        /// Initializes the cursor.
        /// </summary>
        /// <param name="playerInput">The player input connected to the cursor.</param>
        private void Init(PlayerInput playerInput)
        {
            mainCamera = Camera.main;

            //Adds the the virtual mouse to the input system
            if (virtualMouse == null)
                virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");

            else if (!virtualMouse.added)
                InputSystem.AddDevice(virtualMouse);

            //Enables the virtual mouse
            InputSystem.EnableDevice(virtualMouse);
            Cursor.SetCursor(null, cursorHotSpot, cursorMode);

            //Connects the virtual mouse to the player input component
            InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);
            MoveCursorToCenterScreen();

            InputSystem.onAfterUpdate += UpdateMotion;
        }

        /// <summary>
        /// Moves the gamepad cursor to the center of the screen.
        /// </summary>
        private void MoveCursorToCenterScreen()
        {
            //Resets the cursor to the center of the screen
            Vector2 position = new Vector2(Screen.width / 2, Screen.height / 2);
            InputState.Change(virtualMouse.position, position);
        }

        private void OnDisable()
        {
            if (playerInput == null)
                return;

            InputSystem.onAfterUpdate -= UpdateMotion;

            if (virtualMouse != null && virtualMouse.added)
                InputSystem.DisableDevice(virtualMouse);
        }

        /// <summary>
        /// Updates the position of the gamepad cursor.
        /// </summary>
        private void UpdateMotion()
        {
            //If the cursor is inactive or can't move, return
            if (!cursorActive || !cursorCanMove)
                return;

            //Get the device from the player
            InputDevice currentDevice = null;
            foreach(InputDevice device in playerInput.devices)
            {
                if (device != null && device != virtualMouse)
                {
                    currentDevice = device;
                    break;
                }
            }

            //If there is no device or canvas found, return
            if (virtualMouse == null || currentDevice == null || canvas == null)
                return;

            // Delta of the cursor movement
            Vector2 deltaValue = GetComponent<PlayerData>().playerMovementData;
            deltaValue *= cursorSpeed * Time.deltaTime;

            Vector2 currentPosition = virtualMouse.position.ReadValue();
            Vector2 newPosition = currentPosition + deltaValue;
            AdjustCursorPosition(newPosition, deltaValue);

            //Check for any UI interactions
            UIInteractions(currentDevice);
        }

        /// <summary>
        /// Checks to see if there are any interactions being made on the UI.
        /// </summary>
        /// <param name="playerDevice">The device being used to check for UI interactions.</param>
        private void UIInteractions(InputDevice playerDevice)
        {
            // Use EventSystem to perform UI selection based on cursor position
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = localGamepadCursorTransform.position;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            GamepadSelectable newHoveredObject = null;

            foreach (RaycastResult result in results)
            {
                GameObject selectedObject = result.gameObject;

                // Check if the selected object has a GamepadSelectable component
                GamepadSelectable draggableObject = selectedObject.GetComponent<GamepadSelectable>();

                if (draggableObject != null)
                {
                    //Debug.Log("Gamepad Cursor Selecting " + selectedObject);
                    newHoveredObject = draggableObject;
                    break;
                }
            }

            // Check if the cursor just entered a new object
            if (newHoveredObject != lastHoveredObject)
            {
                if (lastHoveredObject != null)
                {
                    //Debug.Log("Hover Exited");
                    lastHoveredObject.RemovePlayerSelecting(playerInput);
                    lastHoveredObject.OnCursorExit(playerInput);
                }

                if (newHoveredObject != null)
                {
                    //Debug.Log("Hover Entered");
                    newHoveredObject.AddPlayerSelecting(playerInput);
                    newHoveredObject.OnCursorEnter(playerInput);
                }

                // Update the lastHoveredObject
                lastHoveredObject = newHoveredObject;
            }

            // Checks to see if the select button is pressed
            bool selectButtonPressed = false;

            if (playerDevice is Keyboard)
                selectButtonPressed = ((Keyboard)playerDevice).spaceKey.isPressed;
            else if (playerDevice is Gamepad)
                selectButtonPressed = ((Keyboard)playerDevice).spaceKey.isPressed;

            // Check for the transition from not pressed to pressed
            if (!previousButtonSouthState && selectButtonPressed)
            {
                virtualMouse.CopyState<MouseState>(out var mouseState);
                mouseState.WithButton(MouseButton.Left, true);
                InputState.Change(virtualMouse, mouseState);

                // Get the currently hovered object
                GameObject clickedObject = GetCurrentHoveredObject();

                if (clickedObject != null)
                {
                    // Check if the clicked object has a GamepadSelectable component
                    GamepadSelectable draggableObject = clickedObject.GetComponent<GamepadSelectable>();
                    if (draggableObject != null)
                    {
                        // Perform actions for the clicked GamepadSelectable
                        // Debug.Log("Pointer Down!");
                        draggableObject.OnSelectObject(playerInput);
                    }
                }
            }
            // Check for the transition from pressed to not pressed
            else if (previousButtonSouthState && !selectButtonPressed)
            {
                // Get the currently hovered object
                GameObject clickedObject = GetCurrentHoveredObject();

                if (clickedObject != null)
                {
                    // Check if the clicked object has a GamepadSelectable component
                    GamepadSelectable draggableObject = clickedObject.GetComponent<GamepadSelectable>();
                    if (draggableObject != null)
                    {
                        // Perform actions for the clicked GamepadSelectable
                        // Debug.Log("Pointer Up!");
                    }
                }
            }

            previousButtonSouthState = selectButtonPressed;
        }

        /// <summary>
        /// Gets the object currently hovered by the gamepad cursor.
        /// </summary>
        /// <returns>The GameObject being hovered by the gamepad cursor.</returns>
        private GameObject GetCurrentHoveredObject()
        {
            // Use EventSystem to perform UI selection based on cursor position
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = localGamepadCursorTransform.position;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                GameObject selectedObject = result.gameObject;

                // Check if the selected object has a GamepadSelectable component
                GamepadSelectable draggableObject = selectedObject.GetComponent<GamepadSelectable>();

                // Return the first object with a DraggableObject component
                if (draggableObject != null)
                    return selectedObject;
            }

            return null;
        }

        /// <summary>
        /// Moves the cursor anchored based on the screen point position.
        /// </summary>
        /// <param name="position">The position to move the cursor to.</param>
        private void AnchorCursor(Vector2 position)
        {
            Vector2 anchoredPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, position, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera, out anchoredPosition);
            localGamepadCursorTransform.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Adjust the position of the cursor.
        /// </summary>
        /// <param name="position">The new position of the cursor.</param>
        /// <param name="deltaValue">The new delta value of the cursor.</param>
        public void AdjustCursorPosition(Vector2 position, Vector2 deltaValue)
        {
            //Clamp the position within the bounds of the screen
            Vector2 newPosition = position;
            newPosition.x = Mathf.Clamp(newPosition.x, padding, Screen.width - padding);
            newPosition.y = Mathf.Clamp(newPosition.y, padding, Screen.height - padding);

            //Change the virtual mouse position and delta
            InputState.Change(virtualMouse.position, newPosition);
            InputState.Change(virtualMouse.delta, deltaValue);

            //Update the cursor transform
            AnchorCursor(newPosition);
        }

        /// <summary>
        /// Instantiates the gamepad cursor on the appropriate canvas.
        /// </summary>
        /// <param name="newColor">The color for the gamepad cursor (which corresponds to the player color).</param>
        public void CreateGamepadCursor(Color newColor)
        {
            InitializeCursor();

            cursorColor = newColor;
            localGamepadCursorTransform = Instantiate(cursorTransform, canvasRectTransform);
            cursorHand = localGamepadCursorTransform.Find("Hand").GetComponent<Image>();
            cursorHandOutline = localGamepadCursorTransform.Find("Outline").GetComponent<Image>();
            cursorHandOutline.color = cursorColor;
            cursorText = localGamepadCursorTransform.GetComponentInChildren<TextMeshProUGUI>();
            cursorText.text = string.Empty;

            SetGamepadCursorState(GamepadCursorState.DEFAULT);
            RefreshCursor(GameSettings.showGamepadCursors);
        }

        /// <summary>
        /// Creates the canvas and player input for the gamepad cursor.
        /// </summary>
        private void InitializeCursor()
        {
            playerInput = GetComponent<PlayerInput>();
            Init(playerInput);

            canvas = GameObject.FindGameObjectWithTag("CursorCanvas")?.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No canvas with the CursorCanvas tag can be found. Please ensure there is a canvas with this tag to use gamepad cursors.");
                return;
            }

            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Refreshes the visuals of the cursor.
        /// </summary>
        /// <param name="isCursorActive">The state of whether the cursor is active or not.</param>
        public void RefreshCursor(bool isCursorActive)
        {
            cursorActive = isCursorActive;

            //If there is no local gamepad cursor, create one
            if (localGamepadCursorTransform == null)
                CreateGamepadCursor(cursorColor);

            Image[] images = localGamepadCursorTransform.GetComponentsInChildren<Image>();
            images[1].color = new Color(images[1].color.r, images[1].color.g, images[1].color.b, cursorActive ? 1 : 0);
            images[0].color = new Color(cursorColor.r, cursorColor.g, cursorColor.b, cursorActive ? 1 : 0);
            cursorCanMove = cursorActive;

            if (cursorActive)
            {
                //Change the player's action map to game cursor
                PlayerData.ToPlayerData(playerInput).ChangePlayerActionMap("GameCursor");
            }
            else
            {
                //Remove cursor text
                RemoveCursorText();
            }
        }

        /// <summary>
        /// Adds text to the cursor.
        /// </summary>
        /// <param name="text">The text to show.</param>
        public void AddCursorText(string text)
        {
            cursorText.text = text;
            cursorText.color = Color.white;
        }

        /// <summary>
        /// Adds text to the cursor.
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <param name="textColor">The color of the text.</param>
        public void AddCursorText(string text, Color textColor)
        {
            cursorText.text = text;
            cursorText.color = textColor;
        }

        /// <summary>
        /// Removes the cursor text.
        /// </summary>
        public void RemoveCursorText() => cursorText.text = string.Empty;

        /// <summary>
        /// Changes the gamepad cursor sprite to a specific state.
        /// </summary>
        /// <param name="gamepadCursorState">The state to change the sprite into.</param>
        public void SetGamepadCursorState(GamepadCursorState gamepadCursorState)
        {
            //If there are no settings, return
            if (gamepadCursorSettings == null)
                return;

            currentCursorState = gamepadCursorState;

            //Change the sprites based on the cursor state of the cursor
            switch (gamepadCursorState)
            {
                case GamepadCursorState.DEFAULT:
                    cursorHand.sprite = gamepadCursorSettings.defaultSprite.mainSprite;
                    cursorHandOutline.sprite = gamepadCursorSettings.defaultSprite.outlineSprite;
                    break;
                case GamepadCursorState.SELECT:
                    cursorHand.sprite = gamepadCursorSettings.selectSprite.mainSprite;
                    cursorHandOutline.sprite = gamepadCursorSettings.selectSprite.outlineSprite;
                    break;
                case GamepadCursorState.GRAB:
                    cursorHand.sprite = gamepadCursorSettings.grabSprite.mainSprite;
                    cursorHandOutline.sprite = gamepadCursorSettings.grabSprite.outlineSprite;
                    break;
                case GamepadCursorState.DISABLED:
                    cursorHand.sprite = gamepadCursorSettings.disabledSprite.mainSprite;
                    cursorHandOutline.sprite = gamepadCursorSettings.disabledSprite.outlineSprite;
                    break;
            }
        }

        public void SetCursorMove(bool cursorMove) => cursorCanMove = cursorMove;
        public GamepadCursorState GetGamepadCursorState() => currentCursorState;
        public RectTransform GetCursorTransform() => localGamepadCursorTransform;
        public int GetOwnerIndex() => playerInput.playerIndex;
    }
}
