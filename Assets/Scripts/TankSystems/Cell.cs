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
    /// <summary>
    /// Array of cells which neighbor this cell.
    /// Includes cells in other rooms.
    /// Order is North (0), West (1), South (2), then East (3).
    /// </summary>
    public Cell[] neighbors = new Cell[4];
    private BoxCollider2D c; //Cell's local collider

    //Runtime Variables:
    public bool testUpdateAdjacency = false;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        c = GetComponent<BoxCollider2D>(); //Get local collider

        //Initialize runtime variables:
        UpdateAdjacency();
    }
    private void Update()
    {
        if (testUpdateAdjacency) { testUpdateAdjacency = false; UpdateAdjacency(); }
    }

    //UTILITY METHODS:
    /// <summary>
    /// Updates list indicating which sides are open and which are adjacent to other cells.
    /// </summary>
    public void UpdateAdjacency()
    {
        for (int x = 0; x < 4; x++) //Loop for four iterations (once for each direction)
        {
            if (neighbors[x] != null) continue; //Don't bother checking directions which are already occupied by neighbors
            RaycastHit2D hit = Physics2D.Raycast(transform.position, cardinals[x], 1, ~LayerMask.NameToLayer("Cell")); //Check for adjacent cell in given direction
            if (hit.collider != null && hit.collider != c) //Adjacent cell found in current direction
            {
                neighbors[x] = hit.collider.GetComponent<Cell>(); //Indicate that cell is a neighbor
                neighbors[x].neighbors[(x + 2) % 4] = this;       //Let neighbor know it has a neighbor (coming from the opposite direction)
            }
        }
    }
}
