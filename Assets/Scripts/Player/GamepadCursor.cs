using System.Collections.Generic;
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
        [SerializeField] private RectTransform cursorTransform;
        [SerializeField] private float cursorSpeed = 1000f;
        [SerializeField] private float padding = 50f;

        private bool previousButtonSouthState;
        private Mouse virtualMouse;
        private Camera mainCamera;
        private GamepadSelectable lastHoveredObject;

        private PlayerInput playerInput;
        private Canvas canvas;
        private RectTransform canvasRectTransform;
        private RectTransform localGamepadCursorTransform;
        private Color cursorColor;

        private bool cursorActive;
        private bool cursorCanMove;

        private void OnEnable()
        {
            if (playerInput == null)
                return;

            mainCamera = Camera.main;

            //Adds the the virtual mouse to the input system
            if (virtualMouse == null)
            {
                virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
            }

            else if (!virtualMouse.added)
            {
                InputSystem.AddDevice(virtualMouse);
            }

            //Connects the virtual mouse to the player input component
            InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);

            //Resets the cursor to the position of the cursor rectTransform
            if (localGamepadCursorTransform != null)
            {
                Vector2 position = localGamepadCursorTransform.anchoredPosition;
                InputState.Change(virtualMouse.position, position);
            }

            InputSystem.onAfterUpdate += UpdateMotion;
        }

        private void OnDisable()
        {
            if (playerInput == null)
                return;

            if (virtualMouse != null && virtualMouse.added)
                InputSystem.RemoveDevice(virtualMouse);
            InputSystem.onAfterUpdate -= UpdateMotion;
        }

        /// <summary>
        /// Updates the position of the gamepad cursor.
        /// </summary>
        private void UpdateMotion()
        {
            if (!cursorActive || !cursorCanMove)
                return;

            InputDevice currentDevice = playerInput.devices[0];

            if (virtualMouse == null || currentDevice == null || canvas == null)
                return;

            Vector2 deltaValue = Vector2.zero;

            if (currentDevice is Keyboard)
            {
                // Position of the mouse
                deltaValue = Mouse.current.delta.ReadValue();

                Vector2 newPosition = Mouse.current.position.ReadValue();
                newPosition.x = Mathf.Clamp(newPosition.x, padding, Screen.width - padding);
                newPosition.y = Mathf.Clamp(newPosition.y, padding, Screen.height - padding);

                // Changes the virtual mouse position and delta
                InputState.Change(virtualMouse.position, newPosition);
                InputState.Change(virtualMouse.delta, deltaValue);

                // Update the cursor transform
                AnchorCursor(newPosition);
            }

            else if (currentDevice is Gamepad)
            {
                // Delta of the gamepad cursor
                deltaValue = GetComponent<PlayerData>().movementData;
                deltaValue *= cursorSpeed * Time.deltaTime;

                Vector2 currentPosition = virtualMouse.position.ReadValue();
                Vector2 newPosition = currentPosition + deltaValue;
                newPosition.x = Mathf.Clamp(newPosition.x, padding, Screen.width - padding);
                newPosition.y = Mathf.Clamp(newPosition.y, padding, Screen.height - padding);

                // Changes the virtual mouse position and delta
                InputState.Change(virtualMouse.position, newPosition);
                InputState.Change(virtualMouse.delta, deltaValue);

                // Update the cursor transform
                AnchorCursor(newPosition);
            }

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
            {
                selectButtonPressed = Mouse.current.leftButton.isPressed;
            }

            else if (playerDevice is Gamepad)
            {
                selectButtonPressed = ((Gamepad)playerDevice).buttonSouth.isPressed;
            }

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
        /// Instantiates the gamepad cursor on the appropriate canvas.
        /// </summary>
        /// <param name="newColor">The color for the gamepad cursor (which corresponds to the player color).</param>
        public void CreateGamepadCursor(Color newColor)
        {
            cursorColor = newColor;
            InitializeCursor();
            localGamepadCursorTransform = Instantiate(cursorTransform, canvasRectTransform);
            localGamepadCursorTransform.GetComponent<Image>().color = cursorColor;
            RefreshCursor(GameSettings.showGamepadCursors);
        }

        /// <summary>
        /// Creates the canvas and player input for the gamepad cursor.
        /// </summary>
        private void InitializeCursor()
        {
            playerInput = GetComponent<PlayerInput>();
            canvas = GameObject.FindGameObjectWithTag("CursorCanvas")?.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No canvas with the CursorCanvas tag can be found. Please ensure there is a canvas with this tag to use gamepad cursors.");
                return;
            }

            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        public void RefreshCursor(bool isCursorActive)
        {
            cursorActive = isCursorActive;

            //If there is no local gamepad cursor, create one
            if (localGamepadCursorTransform == null)
                CreateGamepadCursor(cursorColor);

            localGamepadCursorTransform.GetComponent<Image>().color = new Color(cursorColor.r, cursorColor.g, cursorColor.b, cursorActive ? 1 : 0);
            cursorCanMove = cursorActive;
        }

        public void SetCursorMove(bool cursorMove) => cursorCanMove = cursorMove;

        public RectTransform GetCursorTransform() => localGamepadCursorTransform;
        public int GetOwnerIndex() => playerInput.playerIndex;
    }
}
