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

    private void OnEnable()
    {
        buttonImage.color = defaultColor;
        isSelected = false;
    }

    public override void OnCursorEnter(PlayerInput playerInput)
    {
        if (IsValidPlayer(playerInput.playerIndex))
        {
            isSelected = true;
            buttonImage.color = hoverColor;
        }
    }

    public override void OnCursorExit(PlayerInput playerInput)
    {
        if (IsValidPlayer(playerInput.playerIndex))
        {
            isSelected = false;
            buttonImage.color = defaultColor;
        }
    }

    public override void OnSelectObject(PlayerInput playerInput)
    {
        if (IsValidPlayer(playerInput.playerIndex) && isSelected)
        {
            OnSelected?.Invoke();
            buttonImage.color = selectColor;
        }
    }
}
