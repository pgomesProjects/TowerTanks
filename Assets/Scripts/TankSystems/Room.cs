using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
    private Cell[] cells;                                      //Individual square units which make up the room
    private Cell[][] sections;                                 //Groups of cells separated by connectors
    private Transform connectorParent;                         //Parent object which contains all connectors
    private SpriteRenderer[] renderers;                        //All renderers for visualizing this room
    private Color[] spriteColors;                              //Target color for each spriteRenderer (normal when not modified by various effects)
    internal RoomData roomData;                                //ScriptableObject containing data about rooms and objects spawned by them

    //Settings:
    [Header("Template Settings:")]
    [SerializeField, Tooltip("Width of coupler prefab (determines how pieces fit together).")] private float couplerWidth = 0.9f;
    [SerializeField, Tooltip("Default integrity of this room template.")]                      private float baseIntegrity = 100;
    [Space()]
    public bool debugPlace;
    public bool debugRotate;
    public bool debugMount;

    //Runtime Variables:
    public RoomType type;         //Which broad purpose this room serves
    private float integrity;      //Health of the room. When reduced to zero, room becomes inoperable
    private bool mounted = false; //Whether or not this room has been attached to another room yet

    //RUNTIME METHODS:
    private void Awake()
    {
        //Setup runtime variables:
        CalculateIntegrity();                                                  //Set base integrity (will be modified by other scripts)
        cells = GetComponentsInChildren<Cell>();                               //Get references to cells in room
        connectorParent = transform.Find("Connectors");                        //Find object containing connectors
        renderers = GetComponentsInChildren<SpriteRenderer>();                 //Get references to renderers in room
        spriteColors = renderers.Select(renderer => renderer.color).ToArray(); //Get array of origin colors of renderers
        roomData = Resources.Load<RoomData>("RoomData");                       //Get roomData object from resources folder

        //Set up cells:
        foreach (Cell cell in cells) cell.UpdateAdjacency();   //Have all cells in room get to know each other
        List<List<Cell>> newSections = new List<List<Cell>>(); //Initialize lists to store section data
        List<Cell> ungroupedCells = new List<Cell>(cells);     //Create list of ungrouped cells to pull cells from
        while (ungroupedCells.Count > 0) //Iterate for as long as there are ungrouped cells
        {
            //Initialize group:
            List<Cell> thisGroup = new List<Cell>(); //Create new list for this group
            Cell currentCell = ungroupedCells[0];    //Get marker for first ungrouped cell
            thisGroup.Add(currentCell);              //Add first ungrouped cell to new list
            ungroupedCells.Remove(currentCell);      //Remove cell from ungrouped list

            //Search for other cells in group:
            for (int x = 0; x < thisGroup.Count; x++) //Iterate through current group as new items are added
            {
                currentCell = thisGroup[x]; //Get current cell
                for (int y = 0; y < 4; y++) //Iterate through neighbors in each cell in group
                {
                    Cell neighbor = currentCell.neighbors[y]; //Get current neighbor
                    if (neighbor != null &&                //Current neighbor exists...
                        !thisGroup.Contains(neighbor) &&   //Is not already in this group...
                        currentCell.connectors[y] == null) //And is not separated by a connector
                    {
                        thisGroup.Add(neighbor);         //Add neighbor to current group
                        ungroupedCells.Remove(neighbor); //Remove neighbor from list of ungrouped cells
                    }
                }
            }
            newSections.Add(thisGroup); //Add group to sections list
        }
        sections = newSections.Select(eachList => eachList.ToArray()).ToArray(); //Convert lists into stored array
        cells[Random.Range(0, cells.Length)].DesignateInteractableSlot(); //Pick one random cell to contain the room's interactable
    }
    private void Update()
    {
        if (debugPlace) { debugPlace = false; SnapMove(transform.position); UpdateRoomType(type); }
        if (debugRotate) { debugRotate = false; Rotate(); UpdateRoomType(type); }
        if (debugMount) { debugMount = false; Mount(); }
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

        //Check for obstruction:
        foreach (Cell cell in cells) //Iterate through cells in room to check for overlaps with other rooms
        {
            cell.c.size = Vector2.one * 1.1f; //Make collider slightly bigger so it can detect colliders directly next to it
            Collider2D[] overlapColliders = Physics2D.OverlapBoxAll(cell.transform.position, cell.c.size, 0, ~LayerMask.NameToLayer("Cell")); //Get an array of all colliders overlapping current cell
            foreach (Collider2D collider in overlapColliders) //Iterate through colliders overlapping cell
            {
                if (collider.TryGetComponent(out Cell otherCell) && otherCell.room != this) //Collider overlaps with a cell from another room
                {
                    print("Cell obstructed");
                    return; //Generate no new couplers
                }
            }
        }
        

        //Generate new couplers:
        foreach (Cell cell in cells) //Check adjacency for every cell in room
        {
            for (int x = 0; x < 4; x++) //Iterate four times, once for each cardinal direction
            {
                if (cell.neighbors[x] == null) //Cell does not have a neighbor at this position
                {
                    //Check for coupling opportunities:
                    bool lat = (x % 2 == 1); //If true, cells are next to each other. If false, one cell is on top of the other
                    Vector2 cellPos = cell.transform.position; //Get position of current cell
                    Vector2 posOffset = (lat ? Vector2.up : Vector2.right) * (couplerWidth / 2); //Get positional offset to apply to cell in order to guarantee coupler overlaps with target
                    RaycastHit2D hit1 = Physics2D.Raycast(cellPos + posOffset, Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell"));
                    RaycastHit2D hit2 = Physics2D.Raycast(cellPos - posOffset, Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell"));

                    //Try placing coupler:
                    if (hit1.collider != null || hit2.collider != null) //Cell side at least partially overlaps with another untaken cell side
                    {
                        //Inverse checks:
                        Room otherRoom = (hit1.collider == null ? hit2 : hit1).collider.GetComponent<Cell>().room; //Get other room hit by either raycast (works even if only one raycast hit a room)
                        if (otherRoom == this) { continue; } //Ignore if hit block is part of this room (happens before potential inverse check)
                        if (hit1.collider == null || hit2.collider == null) //Only one hit made contact with a cell
                        {
                            cellPos = (hit1.collider == null ? hit2 : hit1).transform.position;                                        //Get position of partially-hit cell
                            hit1 = Physics2D.Raycast(cellPos + posOffset, -Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell")); //Reverse raycast from partially-hit cell
                            hit2 = Physics2D.Raycast(cellPos - posOffset, -Cell.cardinals[x], 0.875f, ~LayerMask.NameToLayer("Cell")); //Reverse raycast from partially-hit cell
                        }

                        //Validity checks:
                        if (hit1.collider == null || hit2.collider == null) { continue; } //Ignore if open cell sides do not fully overlap
                        if (hit1.transform != hit2.transform)
                        {
                            if (hit1.collider.GetComponent<Cell>().room != hit2.collider.GetComponent<Cell>().room) { continue; } //Ignore if hitting two cells from different rooms
                            if (Vector2.Distance(hit1.transform.position, hit2.transform.position) > 1) { continue; }             //Ignore if hitting two cells separated by a connector
                        }

                        //Add new coupler:
                        Vector2 newCouplerPos = cellPos + (0.625f * (cellPos == (Vector2)cell.transform.position ? 1 : -1) * Cell.cardinals[x]);                //Find target position of new coupler (between origin cell and struck surface)
                        IEnumerable<Coupler> results = from coupler in ghostCouplers where (Vector2)coupler.transform.position == newCouplerPos select coupler; //Look for ghost couplers which already occupy this position
                        if (results.FirstOrDefault() != null) continue;                                                                                         //Do not place couplers where couplers already exist

                        Coupler newCoupler = Instantiate(roomData.couplerPrefab).GetComponent<Coupler>(); //Instantiate new coupler object
                        newCoupler.transform.position = newCouplerPos;                                    //Move coupler to target position
                        if (lat) newCoupler.transform.eulerAngles = Vector3.forward * 90;                 //Rotate coupler if it is facing east or west
                        newCoupler.transform.parent = transform;                                          //Child coupler to this room
                        ghostCouplers.Add(newCoupler);                                                    //Add new coupler to ghost list

                        newCoupler.roomA = this;      //Give coupler information about this room
                        newCoupler.roomB = otherRoom; //Give coupler information about opposing room
                        newCoupler.cellA = Physics2D.Raycast(newCoupler.transform.position, -Cell.cardinals[x], 0.25f, ~LayerMask.NameToLayer("Cell")).collider.GetComponent<Cell>(); //Get cell in roomA closest to coupler
                        newCoupler.cellB = Physics2D.Raycast(newCoupler.transform.position, Cell.cardinals[x], 0.25f, ~LayerMask.NameToLayer("Cell")).collider.GetComponent<Cell>();  //Get cell in roomB closest to coupler
                    }
                }
            }
        }

        //Cull redundant couplers:
        for (int x = 0; x < ghostCouplers.Count; x++) //Iterate through coupler list, allowing some couplers to be removed during the process
        {
            //Find coupler group:
            Coupler coupler = ghostCouplers[x]; //Get current coupler
            IEnumerable<Coupler> group = from otherCoupler in ghostCouplers //Look through list of couplers
                                         where otherCoupler.transform.rotation == coupler.transform.rotation &&                                                                     //Find coupler with matching orientation (including self)...
                                                 (coupler.transform.rotation.z == 0 ? RoundToGrid(otherCoupler.transform.position.y) == RoundToGrid(coupler.transform.position.y) : //With matching latitudinal position (if horizontal)...
                                                                                      RoundToGrid(otherCoupler.transform.position.x) == RoundToGrid(coupler.transform.position.x))  //With matching longitudinal position (if vertical)...
                                         select otherCoupler; //Get other couplers which fit these criteria

            //Exclude separated cells from group:
            if (group.Count() == 1) continue; //Skip couplers which have no redundancies
            group = group.Where(otherCoupler => GetSection(coupler.cellA) == GetSection(otherCoupler.cellA));                                                                    //Only include couplers in group which are on the same section of origin room
            group = group.Where(otherCoupler => coupler.roomB == otherCoupler.roomB && coupler.roomB.GetSection(coupler.cellB) == coupler.roomB.GetSection(otherCoupler.cellB)); //Make sure included couplers are connecting to the same room, and the same section of that room
            group = group.OrderBy(otherCoupler => coupler.transform.rotation.z == 0 ? otherCoupler.transform.position.x : otherCoupler.transform.position.y);                    //Organize list from down to up and left to right
            for (int y = 1; y < group.Count();) { Coupler redundantCoupler = group.ElementAt(y); ghostCouplers.Remove(redundantCoupler); Destroy(redundantCoupler.gameObject); } //Delete all other couplers in group
        }
    }
    /// <summary>
    /// Rotates unmounted room around its pivot.
    /// </summary>
    public void Rotate(bool clockwise = true)
    {
        //Validity checks:
        if (mounted) { Debug.LogError("Tried to rotate room while mounted!"); return; } //Do not allow mounted rooms to be rotated

        //Move cells:
        Vector3 eulers = 90 * (clockwise ? -1 : 1) * Vector3.forward;                                  //Get euler to rotate assembly with
        transform.Rotate(eulers);                                                                      //Rotate entire assembly on increments of 90 degrees
        connectorParent.parent = null;                                                                 //Unchild connector object before reverse rotation
        Vector2[] newCellPositions = cells.Select(cell => (Vector2)cell.transform.position).ToArray(); //Get array of cell positions after rotation
        transform.Rotate(-eulers);                                                                     //Rotate assembly back
        connectorParent.parent = transform;                                                            //Re-child connector object after reverse rotation
        for (int x = 0; x < cells.Length; x++) { cells[x].transform.position = newCellPositions[x]; }  //Move cells to their rotated positions

        //Cleanup:
        foreach (Cell cell in cells) cell.ClearAdjacency();  //Clear all cell adjacency statuses first (prevents false neighborhood bugs)
        foreach (Cell cell in cells) cell.UpdateAdjacency(); //Have all cells in room get to know each other        
        SnapMove(transform.position);                        //Snap to grid at current position

        foreach (Cell cell in cells) cell.transform.position = new Vector2(Mathf.Round(cell.transform.position.x * 4), Mathf.Round(cell.transform.position.y * 4)) / 4; //Round position to nearest unit //Cells need to be rounded back into position to prevent certain parts from bugging out
    }
    /// <summary>
    /// Attaches this room to another room or the tank base (based on current position of the room and couplers).
    /// </summary>
    public void Mount()
    {
        //Validity checks:
        if (mounted) { Debug.LogError("Tried to mount room which is already mounted!"); return; }                              //Cannot mount rooms which are already mounted
        if (ghostCouplers.Count == 0) { Debug.Log("Tried to mount room which is not connected to any other rooms!"); return; } //Cannot mount rooms which are not connected to the tank

        //Un-ghost couplers:
        List<Cell> ladderCells = new List<Cell>(); //Initialize list to keep track of cells which need ladders added to them
        foreach (Coupler coupler in ghostCouplers) //Iterate through ghost couplers list
        {
            //Mount coupler:
            coupler.Mount();                     //Tell coupler it is being mounted
            couplers.Add(coupler);               //Add coupler to master list
            coupler.roomB.couplers.Add(coupler); //Add coupler to other room's master list

            //Add ladders & platforms:
            if (coupler.transform.rotation.z == 0) //Coupler is horizontal
            {
                //Place initial ladder:
                Cell cell = coupler.cellA.transform.position.y > coupler.cellB.transform.position.y ? coupler.cellB : coupler.cellA; //Pick whichever cell is below coupler
                GameObject ladder = Instantiate(roomData.ladderPrefab, cell.transform);                                              //Instantiate ladder as child of lower cell
                ladder.transform.position = new Vector2(coupler.transform.position.x, cell.transform.position.y);                    //Move ladder to horizontal position of coupler and vertical position of cell

                print("Found horizontal coupler above cell " + cell.name + ", placing ladder.");

                //Place extra ladders:
                if (cell.neighbors[2] != null && RoundToGrid(cell.neighbors[2].transform.position.x) == RoundToGrid(coupler.transform.position.x)) ladderCells.Add(cell); //Add cell to list of cells which need more ladders below them if cell has more southern neighbors
            }
        }

        //Place extra ladders:
        for (int x = 0; x < ladderCells.Count; x++) //Iterate through list of cells which need ladders
        {
            Cell cell = ladderCells[x]; //Get cell from list of cells which need ladders
            while (cell.neighbors[2] != null) //As long as currently-focused cell has a southern neighbor, keep placing ladders
            {
                //Place ladder:
                cell = cell.neighbors[2];                                               //Move to southern neighbor of previous cell
                GameObject ladder = Instantiate(roomData.ladderPrefab, cell.transform); //Generate a new ladder childed to cell
                ladder.transform.position = cell.transform.position;                    //Match ladder to cell position
                ladder.transform.eulerAngles = Vector3.zero;                            //Make sure ladder is not rotated

                //Place short ladder:
                if (cell.connectors[0]) //Cell is separated from previous cell by a separator
                {
                    ladder = Instantiate(roomData.shortLadderPrefab, cell.transform);                                              //Generate a new short ladder, childed to the cell below it
                    ladder.transform.position = Vector2.Lerp(cell.transform.position, cell.neighbors[0].transform.position, 0.5f); //Place ladder directly between cell and prev cell
                    ladder.transform.eulerAngles = Vector3.zero;                                                                   //Make sure ladder is not rotated
                }
            }
        }
        
        //Cleanup:
        ghostCouplers.Clear(); //Clear ghost couplers list
        mounted = true; //Indicate that room is now mounted
    }
    /// <summary>
    /// Changes room type to given value.
    /// </summary>
    public void UpdateRoomType(RoomType newType)
    {
        //Change room color:
        Color newColor = roomData.roomTypeColors[(int)newType]; //Get new color for room backwall
        foreach (Cell cell in cells) //Iterate through each cell in room
        {
            cell.r.color = newColor;                                                                                     //Set cell color to new type
            foreach (Connector connector in cell.connectors) if (connector != null) connector.backWall.color = newColor; //Set color of connector back wall
        }
        

        //Cleanup:
        type = newType; //Mark that room is now given type
    }

    //UTILITY METHODS:
    /// <summary>
    /// Rounds value to quarter-unit grid.
    /// </summary>
    public float RoundToGrid(float value) { return Mathf.Round(value * 4) / 4; }
    /// <summary>
    /// Sets integrity to base level with room type modifiers applied.
    /// </summary>
    public void CalculateIntegrity()
    {
        integrity = baseIntegrity; //TEMP: Use flat base integrity
    }
    /// <summary>
    /// Returns index of section given cell is in.
    /// </summary>
    /// <returns></returns>
    public int GetSection(Cell cell)
    {
        for (int x = 0; x < sections.Count(); x++) //Iterate through array of section arrays
        {
            for (int y = 0; y < sections[x].Length; y++) //Iterate through section
            {
                if (sections[x][y] == cell) return x; //Return section index once found
            }
        }
        Debug.LogError("GetSection error! Cell " + cell.name + " was not found in room " + name); //Post error if section was not found
        return -1;                                                                                //Return bogus value
    }
}
