using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using UnityEngine.Events;

public class SelectableRoomObject : GamepadSelectable
{
    [SerializeField] private Color hoverColor;
    [SerializeField] private Color selectColor;

    private Image draggableImage;
    private Color defaultColor;
    private int roomID;

    internal UnityEvent<PlayerInput, int> OnSelected = new UnityEvent<PlayerInput, int>();

    private void Awake()
    {
        draggableImage = GetComponent<Image>();
        defaultColor = draggableImage.color;
        isSelected = false;
    }

    private int hoveringCursorCount = 0;

    public override void OnCursorEnter(PlayerInput playerInput)
    {
        if (!isSelected)
        {
            // Cursor entered the object, increase the count
            hoveringCursorCount++;
            draggableImage.color = hoverColor;
        }
    }

    public override void OnCursorExit(PlayerInput playerInput)
    {
        if (!isSelected)
        {
            // Cursor exited the object, decrease the count
            hoveringCursorCount--;

            // Check if no cursors are hovering, then return to default state
            if (hoveringCursorCount <= 0)
            {
                hoveringCursorCount = 0;
                draggableImage.color = defaultColor;
            }
        }
    }

    public void SetRoomID(int newID)
    {
        roomID = newID;
    }

    public override void OnSelectObject(PlayerInput playerInput)
    {
        draggableImage.color = selectColor;
        isSelected = true;
        Debug.Log("Selected By Player " + (playerInput.playerIndex + 1).ToString());
        OnSelected?.Invoke(playerInput, roomID);
    }

    public void DeselectRoom()
    {
        draggableImage.color = defaultColor;
        isSelected = false;
    }
}