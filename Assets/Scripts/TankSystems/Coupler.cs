using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Structural element which connects two rooms of a tank (not necessarily associated with any given cell).
/// </summary>
public class Coupler : MonoBehaviour
{
    //Objects & Components:
    internal Room roomA;      //First room linked to this coupler
    internal Room roomB;      //Second room linked to this coupler
    public Cell cellA;      //Cell closest to this coupler on the first room
    public Cell cellB;      //Cell closest to this coupler on the second room
    private SpriteRenderer r; //Local renderer component

    //Settings:
    [Header("Settings:")]
    [SerializeField, Range(0, 1), Tooltip("")] private float ghostOpacity = 0.5f;

    //Runtime Variables:
    /// <summary>
    /// Coupler prefab spawns as a shadow until mounted.
    /// </summary>
    internal bool mounted = false;

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Sets coupler position and attaches it to two adjoining rooms.
    /// </summary>
    public void Mount()
    {
        //Cleanup:
        Color newColor = r.color; newColor.a = 1; r.color = newColor; //Remove ghost transparency
        mounted = true;                                               //Indicate that coupler is mounted
    }

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        r = GetComponent<SpriteRenderer>(); //Get spriteRenderer component

        //Setup components:
        Color newColor = r.color; newColor.a = ghostOpacity; r.color = newColor; //Have coupler spawn as a ghost
    }

    //UTILITY METHODS:
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
