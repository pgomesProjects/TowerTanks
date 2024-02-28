using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

public class GamepadCursor : MonoBehaviour
{
    [SerializeField] private RectTransform cursorTransform;
    [SerializeField] private float cursorSpeed = 1000f;
    [SerializeField] private float padding = 50f;

    private bool previousMouseState;
    private bool previousButtonSouthState;
    private Mouse virtualMouse;
    private Mouse currentMouse;
    private Camera mainCamera;
    private SelectableRoomObject lastHoveredObject;

    private PlayerInput playerInput;
    private Canvas canvas;
    private RectTransform canvasRectTransform;
    private RectTransform localGamepadCursorTransform;
    private Color cursorColor;

    private string previousControlScheme = "";
    private const string gamepadScheme = "Gamepad";
    private const string mouseScheme = "Keyboard and Mouse";

    private bool cursorActive;

    private void OnEnable()
    {
        if (playerInput == null)
            return;

        mainCamera = Camera.main;
        currentMouse = Mouse.current;

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

        playerInput.onControlsChanged += OnControlsChanged;
    }

    private void OnDisable()
    {
        if (playerInput == null)
            return;

        if (virtualMouse != null && virtualMouse.added)
            InputSystem.RemoveDevice(virtualMouse);
        InputSystem.onAfterUpdate -= UpdateMotion;
        playerInput.onControlsChanged -= OnControlsChanged;
    }

    /// <summary>
    /// Updates the position of the gamepad cursor.
    /// </summary>
    private void UpdateMotion()
    {
        if (!cursorActive)
            return;

        Gamepad currentPlayerGamepad = (Gamepad)playerInput.devices[0];

        if (virtualMouse == null || currentPlayerGamepad == null || canvas == null)
            return;

        // Delta of the gamepad cursor
        Vector2 deltaValue = currentPlayerGamepad.leftStick.ReadValue();
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

        //Check for any UI interactions
        UIInteractions(currentPlayerGamepad);
    }

    /// <summary>
    /// Checks to see if there are any interactions being made on the UI.
    /// </summary>
    /// <param name="playerGamepad">The gamepad being used to check for UI interactions.</param>
    private void UIInteractions(Gamepad playerGamepad)
    {
        // Use EventSystem to perform UI selection based on cursor position
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = localGamepadCursorTransform.position;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        SelectableRoomObject newHoveredObject = null;

        foreach (RaycastResult result in results)
        {
            GameObject selectedObject = result.gameObject;

            // Check if the selected object has a DraggableObject component
            SelectableRoomObject draggableObject = selectedObject.GetComponent<SelectableRoomObject>();

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
                Debug.Log("Hover Exited");
                ExecuteEvents.Execute(lastHoveredObject.gameObject, eventData, ExecuteEvents.pointerExitHandler);
            }

            if (newHoveredObject != null)
            {
                Debug.Log("Hover Entered");
                ExecuteEvents.Execute(newHoveredObject.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
            }

            // Update the lastHoveredObject
            lastHoveredObject = newHoveredObject;
        }

        // Checks to see if the select button is pressed
        bool buttonSouthIsPressed = playerGamepad.buttonSouth.isPressed;

        // Check for the transition from not pressed to pressed
        if (!previousButtonSouthState && buttonSouthIsPressed)
        {
            virtualMouse.CopyState<MouseState>(out var mouseState);
            mouseState.WithButton(MouseButton.Left, true);
            InputState.Change(virtualMouse, mouseState);

            // Get the currently hovered object
            GameObject clickedObject = GetCurrentHoveredObject();

            if (clickedObject != null)
            {
                // Check if the clicked object has a DraggableObject component
                SelectableRoomObject draggableObject = clickedObject.GetComponent<SelectableRoomObject>();
                if (draggableObject != null)
                {
                    // Perform actions for the clicked DraggableObject
                    ExecuteEvents.Execute(clickedObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
                    draggableObject.OnSelectObject(playerInput);
                    Debug.Log("Pointer Down!");
                }
            }
        }
        // Check for the transition from pressed to not pressed
        else if (previousButtonSouthState && !buttonSouthIsPressed)
        {
            // Get the currently hovered object
            GameObject clickedObject = GetCurrentHoveredObject();

            if (clickedObject != null)
            {
                // Check if the clicked object has a DraggableObject component
                SelectableRoomObject draggableObject = clickedObject.GetComponent<SelectableRoomObject>();
                if (draggableObject != null)
                {
                    // Perform actions for the clicked DraggableObject
                    ExecuteEvents.Execute(clickedObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);
                    Debug.Log("Pointer Up!");
                }
            }
        }

        previousButtonSouthState = buttonSouthIsPressed;
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

            // Check if the selected object has a DraggableObject component
            SelectableRoomObject draggableObject = selectedObject.GetComponent<SelectableRoomObject>();

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
    /// Called when the player switches control schemes.
    /// </summary>
    /// <param name="input">The player input component.</param>
    private void OnControlsChanged(PlayerInput input)
    {
/*        
 *        Might be something to use and work on if we want more keyboard and mouse support.
 *        
 *        if (playerInput.currentControlScheme == mouseScheme && previousControlScheme != mouseScheme)
        {
            localGamepadCursorTransform.gameObject.SetActive(false);
            Cursor.visible = true;
            currentMouse.WarpCursorPosition(virtualMouse.position.ReadValue());
            previousControlScheme = mouseScheme;
        }
        else if (playerInput.currentControlScheme == gamepadScheme && previousControlScheme != gamepadScheme)
        {
            localGamepadCursorTransform.gameObject.SetActive(true);
            Cursor.visible = false;
            InputState.Change(virtualMouse.position, currentMouse.position.ReadValue());
            AnchorCursor(currentMouse.position.ReadValue());
            previousControlScheme = gamepadScheme;
        }*/
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
        RefreshCursor();
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

    public void RefreshCursor()
    {
        cursorActive = GameSettings.showGamepadCursors;

        //If there is no local gamepad cursor, create one
        if (localGamepadCursorTransform == null)
            CreateGamepadCursor(cursorColor);

        localGamepadCursorTransform.GetComponent<Image>().color = new Color(cursorColor.r, cursorColor.g, cursorColor.b, cursorActive ? 1: 0);
    }

    public RectTransform GetCursorTransform() => localGamepadCursorTransform;
}
