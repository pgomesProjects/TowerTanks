using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic structural element which tanks are built from.
/// </summary>
public class Room : MonoBehaviour
{
    //Classes, Enums & Structs:
    /// <summary>
    /// Core catergories which indicate room function and properties.
    /// </summary>
    public enum RoomType {
        /// <summary> Does nothing (has not been given a type). </summary>
        Null,
        /// <summary> Governs tank behavior and makes decisions. </summary>
        Command,
        /// <summary> Maintains tank propulsion and integrity. </summary>
        Engineering,
        /// <summary> Acquires and attacks other tanks. </summary>
        Weapons,
        /// <summary> Prevents and mitigates damage. </summary>
        Defense,
        /// <summary> Manages crew and cargo. </summary>
        Logistics
    }

    //Objects & Components:
    private Room parentRoom;                                   //The room this room was mounted to
    private List<Coupler> couplers = new List<Coupler>();      //Couplers connecting this room to other rooms
    private List<Coupler> ghostCouplers = new List<Coupler>(); //Ghost couplers created while moving room before it is mounted
    private Cell[] cells;                                      //Individual square sections which make up the room

    [Header("Prefabs:")]
    [SerializeField, Tooltip("Reference to coupler object spawned when mounting this room.")] private GameObject couplerPrefab;

    //Settings:
    [Header("Template Settings:")]
    [SerializeField, Tooltip("Width of coupler prefab (determines how pieces fit together).")] private float couplerWidth = 0.9f;
    [SerializeField, Tooltip("Default integrity of this room template.")]                      private float baseIntegrity = 100;
    public bool debugButton;

    //Runtime Variables:
    public RoomType type;         //Which broad purpose this room serves
    private float integrity;      //Health of the room. When reduced to zero, room becomes inoperable
    private bool mounted = false; //Whether or not this room has been attached to another room yet

    //RUNTIME METHODS:
    private void Awake()
    {
        //Setup runtime variables:
        CalculateIntegrity();                    //Set base integrity (will be modified by other scripts)
        cells = GetComponentsInChildren<Cell>(); //Get references to cells in room
    }
    private void Update()
    {
        if (debugButton) { debugButton = false; SnapMove(transform.position); }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Moves unmounted room as close as possible to target position while snapping to grid.
    /// </summary>
    /// <param name="targetPoint"></param>
    public void SnapMove(Vector2 targetPoint)
    {
        //Validity checks:
        if (mounted) //Room is already mounted
        {
            Debug.LogError("Tried to move room while it is mounted!"); //Log error
            return;                                                    //Cancel move
        }

        //Constrain to grid:
        Vector2 newPoint = targetPoint * 4;                                       //Multiply position by four so that it can be rounded to nearest quarter unit
        newPoint = new Vector2(Mathf.Round(newPoint.x), Mathf.Round(newPoint.y)); //Round position to nearest unit
        newPoint /= 4;                                                            //Divide result after rounding to get actual value
        transform.position = newPoint;                                            //Apply new position

        //Clear ghosts:
        foreach (Coupler coupler in ghostCouplers) Destroy(coupler.gameObject); //Destroy each ghost coupler
        ghostCouplers.Clear();                                                  //Clear list of references to ghosts

        //Generate new couplers:
        foreach (Cell cell in cells) //Check adjacency for every cell in room
        {
            for (int x = 0; x < 4; x++) //Iterate four times, once for each cardinal direction
            {
                if (cell.neighbors[x] == null) //Cell does not have a neighbor at this position
                {
                    //Check for coupling opportunities:
                    bool lat = (x % 2 == 1); //If true, cells are next to each other. If false, one cell is on top of the other
                    Vector2 cellPos = cell.transform.position;                      //Get position of current cell
                    Vector2 posOffset = (lat ? Vector2.up : Vector2.right) * (couplerWidth / 2); //Get positional offset to apply to cell in order to guarantee coupler overlaps with target
                    RaycastHit2D hit1 = Physics2D.Raycast(cellPos + posOffset, Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell"));
                    RaycastHit2D hit2 = Physics2D.Raycast(cellPos - posOffset, Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell"));
                    Debug.DrawRay(cellPos + posOffset, Cell.cardinals[x], Color.green, 1);
                    Debug.DrawRay(cellPos - posOffset, Cell.cardinals[x], Color.green, 1);

                    //Try placing coupler:
                    if (hit1.collider != null || hit2.collider != null) //Cell side at least partially overlaps with another untaken cell side
                    {
                        //Inverse checks:
                        if ((hit1.collider == null ? hit2 : hit1).collider.GetComponent<Cell>().room == this) continue; //Ignore if hit block is part of this room (happens before potential inverse check)
                        if (hit1.collider == null || hit2.collider == null) //Only one hit made contact with a cell
                        {
                            cellPos = (hit1.collider == null ? hit2 : hit1).transform.position;                                        //Get position of partially-hit cell
                            hit1 = Physics2D.Raycast(cellPos + posOffset, -Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell")); //Reverse raycast from partially-hit cell
                            hit2 = Physics2D.Raycast(cellPos - posOffset, -Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell")); //Reverse raycast from partially-hit cell
                            Debug.DrawRay(cellPos + posOffset, -Cell.cardinals[x], Color.red, 1);
                            Debug.DrawRay(cellPos - posOffset, -Cell.cardinals[x], Color.red, 1);
                        }

                        //Validity checks:
                        if (hit1.collider == null || hit2.collider == null) continue; //Ignore if open cell sides do not fully overlap
                        if (hit1.distance < 0.75f) continue;                          //Ignore if hit block is too close for a coupler to be placed
                        if (hit1.transform != hit2.transform)
                        {
                            if (hit1.collider.GetComponent<Cell>().room != hit2.collider.GetComponent<Cell>().room) continue; //Ignore if hitting two cells from different rooms
                            if (Vector2.Distance(hit1.transform.position, hit2.transform.position) > 1) continue;             //Ignore if hitting two cells separated by a connector
                        }

                        //Add ghost coupler:
                        Coupler newCoupler = Instantiate(couplerPrefab, transform).GetComponent<Coupler>(); //Instantiate new coupler object
                        newCoupler.transform.position = cellPos + (0.625f * (cellPos == (Vector2)cell.transform.position ? 1 : -1) * Cell.cardinals[x]); //Move coupler to target position between current cell and the cell it has struck
                        if (lat) newCoupler.transform.rotation = Quaternion.Euler(0, 0, 90);                //Rotate coupler if it is facing east or west
                        ghostCouplers.Add(newCoupler);                                                      //Add new coupler to ghost list
                    }
                }
            }
        }
    }
    /// <summary>
    /// Rotates unmounted room around its pivot.
    /// </summary>
    public void Rotate(bool clockwise = true)
    {

    }
    /// <summary>
    /// Attaches this room to another room or the tank base (based on current position of the room).
    /// </summary>
    public void Mount()
    {

    }

    //UTILITY METHODS:
    /// <summary>
    /// Sets integrity to base level with room type modifiers applied.
    /// </summary>
    public void CalculateIntegrity()
    {
        integrity = baseIntegrity; //TEMP: Use flat base integrity
    }
}
