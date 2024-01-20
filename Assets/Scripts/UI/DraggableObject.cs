using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DraggableObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color hoverColor;
    [SerializeField] private Color selectColor;

    private Image draggableImage;
    private Color defaultColor;

    private void Awake()
    {
        draggableImage = GetComponent<Image>();
        defaultColor = draggableImage.color;
    }

    private int hoveringCursorCount = 0;

    // Implement the IPointerEnterHandler interface
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Cursor entered the object, increase the count
        hoveringCursorCount++;
        draggableImage.color = hoverColor;
    }

    // Implement the IPointerExitHandler interface
    public void OnPointerExit(PointerEventData eventData)
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

    public void OnSelectObject(PlayerInput playerInput)
    {
        draggableImage.color = selectColor;
        Debug.Log("Selected By Player " + (playerInput.playerIndex + 1).ToString());
    }
}
