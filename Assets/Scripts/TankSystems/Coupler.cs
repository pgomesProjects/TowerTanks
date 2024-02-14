using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Structural element which connects two rooms of a tank (not necessarily associated with any given cell).
/// </summary>
public class Coupler : MonoBehaviour
{
    //Objects & Components:
    private SpriteRenderer r; //Local renderer component

    [Tooltip("First room linked to this coupler.")]                    internal Room roomA;
    [Tooltip("Second room linked to this coupler.")]                   internal Room roomB;
    [Tooltip("Cell closest to this coupler on the first room.")]       internal Cell cellA; //NOTE: Should probably be changed to "AdjacentCellsA"
    [Tooltip("Cell closest to this coupler on the second room.")]      internal Cell cellB;               
    [Tooltip("Array of walls touching this coupler (on both sides).")] internal Collider2D[] adjacentWalls;

    //Runtime Variables:
    [Tooltip("True if coupler is vertically oriented (hatch). False if coupler is horizontally oriented (door).")] internal bool vertical = true;

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
        //Get adjacent walls:
        List<Collider2D> overlaps = Physics2D.OverlapBoxAll(transform.position, new Vector2(0.79f, 0.25f + 0.1f), transform.rotation.z, LayerMask.GetMask("Ground")).ToList(); //Get list of walls near coupler (using box which extends laterally from coupler)
        foreach (Collider2D ownCollider in GetComponentsInChildren<Collider2D>()) overlaps.Remove(ownCollider); //Remove own colliders from list of overlapping walls
        adjacentWalls = overlaps.ToArray();                                                                     //Store found walls in local array

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
