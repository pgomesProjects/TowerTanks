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
    internal BoxCollider2D c;  //Cell's local collider
    internal SpriteRenderer r; //Renderer for cell's primary back wall

    internal Room room; //The room this cell is a part of.
    /// <summary>
    /// Array of up to four cells which directly neighbor this cell.
    /// Includes cells in other rooms.
    /// Order is North (0), West (1), South (2), then East (3).
    /// </summary>
    public Cell[] neighbors = new Cell[4];
    /// <summary>
    /// Array of up to four optional connector (spacer) pieces which attach this room to its corresponding neighbor.
    /// Connectors indicate splits between room sections.
    /// </summary>
    public Connector[] connectors = new Connector[4]; //Connectors adjacent to this cell (in NESW order)
    public GameObject[] walls;                        //Pre-assigned cell walls (in NESW order) which confine players inside the tank
    public int section;                               //Which section this cell is in inside its parent room

    //Runtime Variables:
    /// <summary>
    /// If true, this cell will be populated with an interactable when its room is placed.
    /// </summary>
    internal bool hasInteractableSlot = false;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        room = GetComponentInParent<Room>(); //Get room cell is connected to
        c = GetComponent<BoxCollider2D>();   //Get local collider
        r = GetComponent<SpriteRenderer>();  //Get local primary sprite renderer
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
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, cardinals[x], 1, (LayerMask.GetMask("Cell") | LayerMask.GetMask("Connector"))); //Check for adjacent cell in given direction
            foreach (RaycastHit2D hit in hits) //Iterate through hit objects
            {
                //Update neighbors:
                if (hit.collider.TryGetComponent(out Cell cell) && hit.collider != c) //Adjacent cell found in current direction
                {
                    if (excludeExternal && hit.transform.parent != transform.parent) continue; //If told to, ignore cells which don't share a parent with this cell
                    neighbors[x] = cell;                                  //Indicate that cell is a neighbor
                    neighbors[x].neighbors[(x + 2) % 4] = this;           //Let neighbor know it has a neighbor (coming from the opposite direction)
                    walls[x].SetActive(false);                            //Disable wall facing neighbor
                    neighbors[x].walls[(x + 2) % 4].SetActive(false);     //Disable neighbor's wall facing this cell
                    neighbors[x].connectors[(x + 2) % 4] = connectors[x]; //Update neighbor on known connector status
                }
                
                //Update connectors:
                else if (hit.transform.parent.TryGetComponent(out Connector connector)) //Adjacent connector found in current direction
                {
                    if (connector.room != room) continue;                                       //Skip connectors from other rooms
                    connectors[x] = connector;                                                  //Save information about connector to slot in current direction
                    if (neighbors[x] != null) neighbors[x].connectors[(x + 2) % 4] = connector; //Give information to neighbor if valid
                }
            }
        }
    }
    /// <summary>
    /// Resets adjacency data (must be done to all cells before re-updating adjacency to prevent bugs).
    /// </summary>
    public void ClearAdjacency()
    {
        neighbors = new Cell[4];                                 //Clear neighbors
        connectors = new Connector[4];                           //Clear connectors
        foreach (GameObject wall in walls) wall.SetActive(true); //Reset walls
    }
    /// <summary>
    /// Indicate that this cell will contain an interactable upon room placement.
    /// </summary>
    public void DesignateInteractableSlot()
    {
        hasInteractableSlot = true;                                                                                      //Indicate that cell has an interactable slot
        Transform slotIndicator = Instantiate(GetComponentInParent<Room>().roomData.slotIndicator, transform).transform; //Instantiate slot indicator object
        slotIndicator.localPosition = new Vector3(0, 0, -5);                                                             //Move indicator to be centered on cell
    }
}
