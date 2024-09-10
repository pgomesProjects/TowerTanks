using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CustomEnums;
using Cinemachine;

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
    internal List<Coupler> couplers = new List<Coupler>();     //Couplers connecting this room to other rooms
    private List<Coupler> ghostCouplers = new List<Coupler>(); //Ghost couplers created while moving room before it is mounted
    internal List<Cell> cells = new List<Cell>();              //Individual square units which make up the room
    private Cell[][] sections;                                 //Groups of cells separated by connectors
    private Transform connectorParent;                         //Parent object which contains all connectors
    internal RoomData roomData;                                //ScriptableObject containing data about rooms and objects spawned by them

    //Settings:
    [Header("Template Settings:")]
    [Tooltip("Indicates whether or not this is the tank's indestructible core room.")]                     public bool isCore = false;
    [SerializeField, Tooltip("If true, room type will be randomized upon spawn (IF spawn type is null).")] public bool randomizeType = false; 
    [Header("Debug Moving:")]
    public bool debugRotate;
    public int debugRotation = 0;
    public bool debugMoveUp;
    public bool debugMoveDown;
    public bool debugMoveLeft;
    public bool debugMoveRight;
    public bool flipOnStart;
    [Space()]
    public bool debugMount;
    [Space()]

    //Runtime Variables:
    [Tooltip("Which broad purpose this room serves.")]                                                     public RoomType type;
    [Tooltip("Whether or not this room has been attached to another room yet.")]                           private bool mounted = false;
    [Tooltip("The only tank this room can be mounted to (who's home grid will be used during mounting).")] internal TankController targetTank; //NOTE: This is important for distinguishing between rooms auto-spawned for prefab tanks, and rooms which are spawned in scrap menu for mounting on an existing tank
    private bool initialized = false; //Becomes true once one-time initial room setup has been completed (indicates room is ready to be used)

    private float maxBurnTime = 24f;
    private float minBurnTime = 12f;
    private float burnTimer = 0;

    //RUNTIME METHODS:
    private void Awake()
    {
        Initialize(); //Set everything up
    }
    private void Update()
    {
        if (debugRotate) { debugRotate = false; Rotate(); UpdateRoomType(type); }
        if (debugMoveUp) { debugMoveUp = false; SnapMoveTick(Vector2.up); UpdateRoomType(type); }
        if (debugMoveDown) { debugMoveDown = false; SnapMoveTick(Vector2.down); UpdateRoomType(type); }
        if (debugMoveLeft) { debugMoveLeft = false; SnapMoveTick(Vector2.left); UpdateRoomType(type); }
        if (debugMoveRight) { debugMoveRight = false; SnapMoveTick(Vector2.right); UpdateRoomType(type); }
        if (debugMount) { debugMount = false; Mount(); }

        if (cells.Count > 0) CheckFire();
    }

    private void CheckFire()
    {
        foreach(Cell cell in cells)
        {
            if (cell.isOnFire == false)
            {
                burnTimer = Random.Range(minBurnTime, maxBurnTime);
                return;
            }
        }

        burnTimer -= Time.deltaTime;
        if (burnTimer <= 0)
        {
            foreach(Cell cell in cells)
            {
                cell.Extinguish();
            }
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Performs all necessary setup so that room can be immediately manipulated and mounted.
    /// </summary>
    public void Initialize()
    {
        //Initialization check:
        if (initialized) return; //Do not attempt to re-initialize a room
        initialized = true;      //Indicate that room has been initialized

        //Setup runtime variables:
        cells = new List<Cell>(GetComponentsInChildren<Cell>()); //Get references to cells in room
        connectorParent = transform.Find("Connectors");          //Find object containing connectors
        roomData = Resources.Load<RoomData>("RoomData");         //Get roomData object from resources folder
        targetTank = GetComponentInParent<TankController>();     //Get tank controller from current parent (only applicable if room spawns with tank)

        //old spot for interactable slot code

        //Set up child components:
        foreach (Connector connector in connectorParent.GetComponentsInChildren<Connector>()) connector.Initialize(); //Initialize all connectors before setting up cells
        foreach (Cell cell in cells) cell.Initialize();                                                               //Initialize each cell before checking adjacency
        foreach (Cell cell in cells) cell.UpdateAdjacency();                                                          //Have all cells in room get to know each other

        //Identify sections:
        List<List<Cell>> newSections = new List<List<Cell>>(); //Initialize lists to store section data
        List<Cell> ungroupedCells = new List<Cell>(cells);     //Create list of ungrouped cells to pull cells from
        while (ungroupedCells.Count > 0) //Iterate for as long as there are ungrouped cells
        {
            //Initialize group:
            List<Cell> thisGroup = new List<Cell>(); //Create new list for this group
            Cell currentCell = ungroupedCells[0];    //Get marker for first ungrouped cell
            thisGroup.Add(currentCell);              //Add first ungrouped cell to new list
            ungroupedCells.Remove(currentCell);      //Remove cell from ungrouped list
            currentCell.section = newSections.Count; //Tell cell which section it is in

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
                        thisGroup.Add(neighbor);              //Add neighbor to current group
                        ungroupedCells.Remove(neighbor);      //Remove neighbor from list of ungrouped cells
                        neighbor.section = newSections.Count; //Tell cell which section it is in
                    }
                }
            }
            newSections.Add(thisGroup); //Add group to sections list
        }
        sections = newSections.Select(eachList => eachList.ToArray()).ToArray(); //Convert lists into stored array

        //Designate type:
        if (randomizeType && type == RoomType.Null) //Room is being spawned with a random type
        {
            //NOTE: A smarter randomization engine needs to be created here so that rooms are always spawned in the scrap menu with different types (maybe with tetris-style randomness)
            UpdateRoomType((RoomType)Random.Range(1, 6)); //Give room a random type and update immediately
        }
    }

    public void Start()
    {
        //Core room-specific setup:
        if (isCore) //This is the tank's core room
        {
            mounted = true; //Core rooms start mounted
        }
    }

    /// <summary>
    /// Moves the cell one tick (0.25 units) in given direction.
    /// </summary>
    /// <param name="direction">Normalized vector indicating direction to move.</param>
    public void SnapMoveTick(Vector2 direction)
    {
        //Get target position:
        direction = direction.normalized;                                           //Make sure direction is normalized
        Vector2 targetPos = (Vector2)transform.localPosition + (direction * 0.25f); //Get target position based off of current position
        SnapMove(targetPos);                                                        //Use normal snapMove method to place room
    }
    /// <summary>
    /// Moves unmounted room as close as possible to target position (in local space) while snapping to grid.
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
        transform.localPosition = newPoint;                                       //Apply new position
        transform.localEulerAngles = Vector3.zero;                                //Zero out rotation relative to parent tank

        //Clear ghosts:
        foreach (Coupler coupler in ghostCouplers) Destroy(coupler.gameObject); //Destroy each ghost coupler
        ghostCouplers.Clear();                                                  //Clear list of references to ghosts

        //Check for obstruction:
        foreach (Cell cell in cells) //Iterate through cells in room to check for overlaps with other rooms
        {
            cell.c.size = Vector2.one * 1.1f; //Make collider slightly bigger so it can detect colliders directly next to it
            Collider2D[] overlapColliders = Physics2D.OverlapBoxAll(cell.transform.position, cell.c.size, 0, LayerMask.GetMask("Cell")); //Get an array of all cell colliders overlapping current cell
            foreach (Collider2D collider in overlapColliders) //Iterate through colliders overlapping cell
            {
                if (collider.TryGetComponent(out Cell otherCell) && otherCell.room != this) //Collider overlaps with a cell from another room
                {
                    //print("Cell obstructed");
                    return; //Generate no new couplers
                }
            }
            cell.c.size = Vector2.one; //Set collider size back to default
        }

        //Generate new couplers:
        foreach (Cell cell in cells) //Check adjacency for every cell in room
        {
            for (int x = 0; x < 4; x++) //Iterate four times, once for each cardinal direction
            {
                if (cell.neighbors[x] == null) //Cell does not have a neighbor at this position
                {
                    //Check for coupling opportunities:
                    bool lat = (x % 2 == 1);                                                              //If true, cells are next to each other. If false, one cell is on top of the other
                    Vector2 cellPos = cell.transform.position;                                            //Get position of current cell
                    Vector2 posOffset = (lat ? Vector2.up : Vector2.right) * (roomData.couplerWidth / 2); //Get positional offset to apply to cell in order to guarantee coupler overlaps with target
                    RaycastHit2D hit1 = Physics2D.Raycast(cellPos + posOffset, Cell.cardinals[x], 0.875f, LayerMask.GetMask("Cell")); //Search for neighboring external cell (offset to make sure cell fully overlaps)
                    RaycastHit2D hit2 = Physics2D.Raycast(cellPos - posOffset, Cell.cardinals[x], 0.875f, LayerMask.GetMask("Cell")); //Search for neighboring external cell (offset to make sure cell fully overlaps)
                    Cell hitCell1 = hit1.collider != null ? hit1.collider.GetComponent<Cell>() : null;                                //Try to get cell component from hit (null if nothing is hit)
                    Cell hitCell2 = hit2.collider != null ? hit2.collider.GetComponent<Cell>() : null;                                //Try to get cell component from hit (null if nothing is hit)

                    //Try placing coupler:
                    if (hitCell1 != null || hitCell2 != null) //Cell side at least partially overlaps with another untaken cell side
                    {
                        //Inverse check:
                        Room otherRoom = hitCell1 == null ? hitCell2.room : hitCell1.room; //Get other room hit by either raycast (works even if only one raycast hit a room)
                        if (otherRoom == this) { continue; } //Ignore if hit block is part of this room (happens before potential inverse check)
                        if (hitCell1 == null || hitCell2 == null) //Only one hit made contact with a cell
                        {
                            cellPos = (hitCell1 == null ? hitCell2 : hitCell1).transform.position; //Get position of partially-hit cell
                            hit1 = Physics2D.Raycast(cellPos + posOffset, -Cell.cardinals[x], 0.875f, LayerMask.GetMask("Cell")); //Re-check alignment from opposing cell to search for a better connection (eliminates edge cases where no two cells from origin room fall between target cell)
                            hit2 = Physics2D.Raycast(cellPos - posOffset, -Cell.cardinals[x], 0.875f, LayerMask.GetMask("Cell")); //Re-check alignment from opposing cell to search for a better connection
                            hitCell1 = hit1.collider != null ? hit1.collider.GetComponent<Cell>() : null;                         //Try to get cell component from hit (null if nothing is hit)
                            hitCell2 = hit2.collider != null ? hit2.collider.GetComponent<Cell>() : null;                         //Try to get cell component from hit (null if nothing is hit)
                        }

                        //Placement validity check:
                        if (hitCell1 == null || hitCell2 == null) { continue; } //Ignore if open cell sides still do not fully overlap with the same room (despite it all)
                        if (hitCell1 != hitCell2) //Validity checks for when system is trying to place a coupler on wall composed of two cells
                        {
                            if (hitCell1.room != hitCell2.room) { continue; }                                                    //Ignore if hitting two cells from different rooms
                            if (Vector2.Distance(hitCell1.transform.position, hitCell2.transform.position) > 1.1f) { continue; } //Ignore if hitting two cells separated by a connector
                        }

                        //Confirm placement:
                        Vector2 newCouplerPos = cellPos + (0.625f * (cellPos == (Vector2)cell.transform.position ? 1 : -1) * Cell.cardinals[x]);                                                 //Find target position of new coupler (between origin cell and struck surface)
                        IEnumerable<Coupler> results = from coupler in ghostCouplers where RoundToGrid(coupler.transform.position, 0.125f) == RoundToGrid(newCouplerPos, 0.125f) select coupler; //Look for ghost couplers which already occupy this position
                        if (results.FirstOrDefault() != null) continue;                                                                                                                          //Do not place couplers where couplers already exist

                        //Generate new coupler:
                        Coupler newCoupler = Instantiate(roomData.couplerPrefab).GetComponent<Coupler>();             //Instantiate new coupler object
                        newCoupler.transform.parent = transform;                                                      //Child coupler to this room
                        newCoupler.transform.position = newCouplerPos;                                                //Move coupler to target position
                        newCoupler.transform.localPosition = RoundToGrid(newCoupler.transform.localPosition, 0.125f); //Snap coupler to special grid (eighth units instead of quarter units)

                        //Check rotation:
                        newCoupler.transform.localEulerAngles = Vector3.zero; //Zero out coupler rotation relative to tank
                        if (lat) //Coupler should be in door orientation (facing east/west)
                        {
                            newCoupler.transform.localEulerAngles = Vector3.forward * 90; //Rotate 90 degrees
                            newCoupler.vertical = false;                                  //Indicate that coupler is horizontally oriented
                        }

                        //Data cleanup:
                        ghostCouplers.Add(newCoupler); //Add new coupler to ghost list
                        newCoupler.roomA = this;      //Give coupler information about this room
                        newCoupler.roomB = otherRoom; //Give coupler information about opposing room
                        newCoupler.cellA = Physics2D.Raycast(newCoupler.transform.position, -Cell.cardinals[x], 0.25f, LayerMask.GetMask("Cell")).collider.GetComponent<Cell>(); //Get cell in roomA closest to coupler
                        newCoupler.cellB = Physics2D.Raycast(newCoupler.transform.position, Cell.cardinals[x], 0.25f, LayerMask.GetMask("Cell")).collider.GetComponent<Cell>();  //Get cell in roomB closest to coupler
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
                                         where otherCoupler.transform.rotation == coupler.transform.rotation &&                                                                                             //Find coupler with matching orientation (including self)...
                                                 (coupler.transform.rotation.z == 0 ? RoundToGrid(otherCoupler.transform.localPosition.y, 0.25f) == RoundToGrid(coupler.transform.localPosition.y, 0.25f) : //With matching latitudinal position (if horizontal)...
                                                                                      RoundToGrid(otherCoupler.transform.localPosition.x, 0.25f) == RoundToGrid(coupler.transform.localPosition.x, 0.25f))  //With matching longitudinal position (if vertical)...
                                         select otherCoupler; //Get other couplers which fit these criteria

            //Exclude separated cells from group:
            if (group.Count() == 1) continue; //Skip couplers which have no redundancies
            group = group.Where(otherCoupler => coupler.cellA.section == otherCoupler.cellA.section);                                                                            //Only include couplers in group which are on the same section of origin room
            group = group.Where(otherCoupler => coupler.roomB == otherCoupler.roomB && coupler.cellB.section == otherCoupler.cellB.section);                                     //Make sure included couplers are connecting to the same room, and the same section of that room
            group = group.OrderBy(otherCoupler => coupler.transform.rotation.z == 0 ? otherCoupler.transform.position.x : otherCoupler.transform.position.y);                    //Organize list from down to up and left to right
            for (int y = 1; y < group.Count();) { Coupler redundantCoupler = group.ElementAt(y); ghostCouplers.Remove(redundantCoupler); Destroy(redundantCoupler.gameObject); } //Delete all other couplers in group
        }
    }
    /// <summary>
    /// Rotates unmounted room around its pivot.
    /// </summary>
    public void Rotate(bool clockwise = true)
    {
        //Used for detecting rotation in json files
        debugRotation++;
        if (debugRotation > 3) debugRotation = 0;

        //Validity checks:
        if (mounted) { Debug.LogError("Tried to rotate room while mounted!"); return; } //Do not allow mounted rooms to be rotated

        //Move cells:
        Vector3 eulers = 90 * (clockwise ? -1 : 1) * Vector3.forward;                                  //Get euler to rotate assembly with
        transform.Rotate(eulers);                                                                      //Rotate entire assembly on increments of 90 degrees
        connectorParent.parent = null;                                                                 //Unchild connector object before reverse rotation
        Vector2[] newCellPositions = cells.Select(cell => (Vector2)cell.transform.position).ToArray(); //Get array of cell positions after rotation
        transform.Rotate(-eulers);                                                                     //Rotate assembly back
        connectorParent.parent = transform;                                                            //Re-child connector object after reverse rotation
        for (int x = 0; x < cells.Count; x++) { cells[x].transform.position = newCellPositions[x]; }   //Move cells to their rotated positions

        //Cell adjacency updates:
        foreach (Cell cell in cells) cell.ClearAdjacency();  //Clear all cell adjacency statuses first (prevents false neighborhood bugs)
        foreach (Cell cell in cells) cell.UpdateAdjacency(); //Have all cells in room get to know each other        
        SnapMove(transform.localPosition);                   //Snap to grid at current position

        foreach (Cell cell in cells) cell.transform.localPosition = new Vector2(Mathf.Round(cell.transform.localPosition.x * 4), Mathf.Round(cell.transform.localPosition.y * 4)) / 4; //Cells need to be rounded back into position to prevent certain parts from bugging out
    }
    /// <summary>
    /// Attaches this room to another room or the tank base (based on current position of the room and couplers).
    /// </summary>
    public bool Mount()
    {
        //Validity checks:
        if (mounted) { Debug.LogError("Tried to mount room which is already mounted!"); return true; }                              //Cannot mount rooms which are already mounted
        if (ghostCouplers.Count == 0) { Debug.Log("Tried to mount room which is not connected to any other rooms!"); return false; } //Cannot mount rooms which are not connected to the tank

        //Un-ghost couplers:
        List<Cell> ladderCells = new List<Cell>(); //Initialize list to keep track of cells which need ladders added to them
        foreach (Coupler coupler in ghostCouplers) //Iterate through ghost couplers list
        {
            //Mount coupler:
            coupler.Mount();                     //Tell coupler it is being mounted
            couplers.Add(coupler);               //Add coupler to master list
            coupler.roomB.couplers.Add(coupler); //Add coupler to other room's master list
            coupler.cellA.couplers.Add(coupler); //Add coupler to cell A's coupler list
            coupler.cellB.couplers.Add(coupler); //Add coupler to cell B's coupler list

            //Add ladders & platforms:
            if (coupler.transform.localRotation.z == 0) //Coupler is horizontal
            {
                //Place initial ladder:
                Cell cell = coupler.cellA.transform.position.y > coupler.cellB.transform.position.y ? coupler.cellB : coupler.cellA; //Pick whichever cell is below coupler
                GameObject ladder = Instantiate(roomData.ladderPrefab, cell.transform);                                              //Instantiate ladder as child of lower cell
                ladder.transform.position = new Vector2(coupler.transform.position.x, cell.transform.position.y);                    //Move ladder to horizontal position of coupler and vertical position of cell

                //print("Found horizontal coupler above cell " + cell.name + ", placing ladder.");

                //Place extra ladders:
                if (cell.neighbors[2] != null && RoundToGrid(cell.neighbors[2].transform.position.x, 0.25f) == RoundToGrid(coupler.transform.position.x, 0.25f)) ladderCells.Add(cell); //Add cell to list of cells which need more ladders below them if cell has more southern neighbors
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
        if (targetTank == null) targetTank = couplers[0].GetConnectedRoom(this).targetTank; //Get target tank from a mounted room if necessary
        if (!targetTank.rooms.Contains(this)) targetTank.rooms.Add(this);                   //Add to target tank's index of rooms
        targetTank.treadSystem.ReCalculateMass();                                           //Re-calculate mass now that new room has been added
        ghostCouplers.Clear();                                                              //Clear ghost couplers list
        mounted = true;                                                                     //Indicate that room is now mounted
        //GameManager.Instance.AudioManager.Play("BuildRoom");

        //Update tank info:
        transform.parent = couplers[0].roomB.transform.parent; //Child room to parent of the rest of the rooms (home tank)
        targetTank.UpdateHighestCell();                        //Check to see if any added cells are higher than the known highest cell

        return mounted;
    }
    /// <summary>
    /// Changes room type to given value.
    /// </summary>
    public void UpdateRoomType(RoomType newType)
    {
        //Initialization:
        type = newType; //Mark that room is now given type

        //Change room color:
        Color newColor = roomData.roomTypeColors[(int)newType]; //Get new color for room backwall
        foreach (Cell cell in cells) //Iterate through each cell in room
        {
            cell.backWall.GetComponent<SpriteRenderer>().color = newColor;                                               //Set cell color to new type
            foreach (Connector connector in cell.connectors) if (connector != null) connector.backWall.color = newColor; //Set color of connector back wall
        }

        //Update interactable ghosts:
        
    }

    //UTILITY METHODS:
    /// <summary>
    /// Rounds value to grid with given units.
    /// </summary>
    public float RoundToGrid(float value, float gridUnits) { return Mathf.Round(value * (1 / gridUnits)) / (1 / gridUnits); }
    /// <summary>
    /// Rounds vector to grid with given units.
    /// </summary>
    public Vector2 RoundToGrid(Vector2 value, float gridUnits) { return new Vector2(RoundToGrid(value.x, gridUnits), RoundToGrid(value.y, gridUnits)); }
    /// <summary>
    /// Returns true if given interactable prefab can be placed in given cell right now.
    /// </summary>
}
