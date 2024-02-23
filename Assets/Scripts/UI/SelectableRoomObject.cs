using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using UnityEngine.Events;

public class SelectableRoomObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color hoverColor;
    [SerializeField] private Color selectColor;

    private Image draggableImage;
    private Color defaultColor;
    private bool isSelected;

    internal UnityEvent<PlayerInput, SelectableRoomObject> OnSelected = new UnityEvent<PlayerInput, SelectableRoomObject>();

    private void Awake()
    {
        draggableImage = GetComponent<Image>();
        defaultColor = draggableImage.color;
        isSelected = false;
    }

    private int hoveringCursorCount = 0;

    // Implement the IPointerEnterHandler interface
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected)
        {
            // Cursor entered the object, increase the count
            hoveringCursorCount++;
            draggableImage.color = hoverColor;
        }
    }

    // Implement the IPointerExitHandler interface
    public void OnPointerExit(PointerEventData eventData)
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

    public void OnSelectObject(PlayerInput playerInput)
    {
        draggableImage.color = selectColor;
        isSelected = true;
        Debug.Log("Selected By Player " + (playerInput.playerIndex + 1).ToString());
        OnSelected?.Invoke(playerInput, this);
    }
}
