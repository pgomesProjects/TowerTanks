using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Tooltip("Controller for elements which players (and enemies) interact with to control and use the tank.")]
public class TankInteractable : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("The cell this interactable is currently installed within.")] internal Cell parentCell;

    //Settings:
    [Header("Settings:")]
    [SerializeField, Tooltip("The room type this interactable is designed for.")] protected Room.RoomType type;

    //Runtime Variables:
    internal bool userInteracting; //True if this interactable is currently being used

    //RUNTIME METHODS:

}
