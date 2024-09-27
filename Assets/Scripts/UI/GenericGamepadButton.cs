using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GenericGamepadButton : GamepadSelectable
{
    [SerializeField, Tooltip("The image for the gamepad button.")] private Image buttonImage;
    [SerializeField, Tooltip("The color for the image when the selectable is hovered.")] private Color hoverColor; 
    [SerializeField, Tooltip("The color for the image when the selectable is selected.")] private Color selectColor;
    [Space]
    public UnityEvent OnSelected;

    private Color defaultColor;

    private void Awake()
    {
        if(buttonImage == null)
            buttonImage = GetComponent<Image>();

        defaultColor = buttonImage.color;
    }

    private void OnDisable()
    {
        buttonImage.color = defaultColor;
        isSelected = false;
    }

    public override void OnCursorEnter(PlayerInput playerInput)
    {
        if (IsValidPlayer(playerInput.playerIndex))
            OnSelect();
    }

    public void OnSelect()
    {
        isSelected = true;
        buttonImage.color = hoverColor;
        Debug.Log("Button " + gameObject.name + " Selected and Color Changed to " + buttonImage.color);
    }

    public override void OnCursorExit(PlayerInput playerInput)
    {
        if (IsValidPlayer(playerInput.playerIndex))
            OnDeselect();
    }

    public void OnDeselect()
    {
        isSelected = false;
        buttonImage.color = defaultColor;
        Debug.Log("Button " + gameObject.name + " Deselected and Color Changed to " + buttonImage.color);

    }

    public override void OnSelectObject(PlayerInput playerInput)
    {
        if (IsValidPlayer(playerInput.playerIndex) && isSelected)
        {
            OnSelected?.Invoke();
            buttonImage.color = selectColor;
        }
    }

    private void OnDestroy()
    {
        OnSelected?.RemoveAllListeners();
    }
}
