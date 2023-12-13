using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Structural element which connects two rooms of a tank.
/// </summary>
public class Coupler : MonoBehaviour
{
    //Objects & Components:
    internal Room roomA; //First room linked to this coupler
    internal Room roomB; //Second room linked to this coupler

    //UTILITY FUNCTIONS:
    /// <summary>
    /// If thisRoom is connected to this coupler, returns room on the other end of the coupler.
    /// </summary>
    public Room GetConnectedRoom(Room thisRoom)
    {
        if (thisRoom == null) //Room given does not exist
        {
            Debug.LogError("Tried to GetConnectedRoom with a room that does not exist!"); //Indicate error
            return null;                                                                  //Return nothing
        }
        else if (thisRoom == roomA) return roomB; //Return room B if given room is room A
        else if (thisRoom == roomB) return roomA; //Return room A if given room is room B
        else
        {
            Debug.LogError("Tried to GetConnectedRoom with a room not connected to coupler!"); //Indicate error
            return null;                                                                       //Return nothing
        }
    }
}
