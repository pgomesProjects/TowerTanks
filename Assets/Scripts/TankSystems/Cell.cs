using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Square component which rooms are made of.
/// </summary>
public class Cell : MonoBehaviour
{
    //Static Variables:
    public static Vector2[] cardinals = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

    //Objects & Components:
    internal Room room; //The room this cell is a part of.
    /// <summary>
    /// Array of up to four cells which directly neighbor this cell.
    /// Includes cells in other rooms.
    /// Order is North (0), West (1), South (2), then East (3).
    /// </summary>
    public Cell[] neighbors = new Cell[4];
    internal bool[] connectors = new bool[4]; //Indicates whether or not cell is separated from corresponding neighbor by a connector piece
    private BoxCollider2D c;                  //Cell's local collider

    //Runtime Variables:


    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        room = GetComponentInParent<Room>(); //Get room cell is connected to
        c = GetComponent<BoxCollider2D>();   //Get local collider
    }

    //UTILITY METHODS:
    /// <summary>
    /// Updates list indicating which sides are open and which are adjacent to other cells.
    /// </summary>
    /// <param name="excludeExternal">If true, cell will ignore cells from other rooms.</param>
    public void UpdateAdjacency(bool excludeExternal = true)
    {
        //Get new neighbors:
        for (int x = 0; x < 4; x++) //Loop for four iterations (once for each direction)
        {
            if (neighbors[x] != null) continue; //Don't bother checking directions which are already occupied by neighbors
            RaycastHit2D hit = Physics2D.Raycast(transform.position, cardinals[x], 1, ~LayerMask.NameToLayer("Cell")); //Check for adjacent cell in given direction
            if (hit.collider != null && hit.collider != c) //Adjacent cell found in current direction
            {
                //Update neighbors:
                if (excludeExternal && hit.transform.parent != transform.parent) continue; //If told to, ignore cells which don't share a parent with this cell
                neighbors[x] = hit.collider.GetComponent<Cell>(); //Indicate that cell is a neighbor
                neighbors[x].neighbors[(x + 2) % 4] = this;       //Let neighbor know it has a neighbor (coming from the opposite direction)
                if (hit.distance > 0.55f) connectors[x] = true;   //Indicate locations of connectors separating neighbors (by 0.25 units)
            }
        }
    }
    /// <summary>
    /// Resets adjacency data (must be done to all cells before re-updating adjacency to prevent bugs).
    /// </summary>
    public void ClearAdjacency()
    {
        neighbors = new Cell[4];  //Clear neighbors
        connectors = new bool[4]; //Clear connectors
    }
}
