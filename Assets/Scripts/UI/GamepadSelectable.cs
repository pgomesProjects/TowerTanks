using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public abstract class GamepadSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public abstract void OnPointerEnter(PointerEventData eventData);
    public abstract void OnPointerExit(PointerEventData eventData);
    public abstract void OnSelectObject(PlayerInput playerInput);
}
