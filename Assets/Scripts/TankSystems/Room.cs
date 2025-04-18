using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CustomEnums;
using Sirenix.OdinInspector;
using UnityEngine.U2D;
using UnityEngine.Rendering;

namespace TowerTanks.Scripts
{
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
            /// <summary> Normal room in which interactables may be built. </summary>
            Standard,
            /// <summary> Room which provides high defense but cannot contain interactables. </summary>
            Armor,
            /// <summary> Room for storing cargo which cannot contain interactables. </summary>
            Cargo,
            /// <summary> Room which provides lift to tank, at the cost of having lower health and exploding entirely on destruction of a cell. </summary>
            Dirigible
        }

        //Objects & Components:
        private Room parentRoom;                                     //The room this room was mounted to
        internal List<Coupler> couplers = new List<Coupler>();       //Couplers connecting this room to other rooms
        internal List<Coupler> hatches = new List<Coupler>();
        private List<Coupler> ghostCouplers = new List<Coupler>();   //Ghost couplers created while moving room before it is mounted
        internal List<Cell> cells = new List<Cell>();                //Individual square units which make up the room
        internal List<Connector> connectors = new List<Connector>(); //connectors in this room
        internal bool[] cellManifest = new bool[0];                  //Array corresponding to cell list, representing which ones have been destroyed (for the purposes of respawning the tank)
        private Cell[][] sections;                                   //Groups of cells separated by connectors
        private Transform connectorParent;                           //Parent object which contains all connectors
        internal RoomData roomData;                                  //ScriptableObject containing data about rooms and objects spawned by them
        internal PhysicsMaterial2D dummyMat;                         //Material recieved from RoomData when assigning it to the Room upon DummyRoom creation
        internal Animator roomAnimator;                              //Animator component used for room VFX
        
        internal List<GameObject> ladders = new List<GameObject>();       //Ladders that are in this room
        private List<GameObject> leadingLadders = new List<GameObject>(); //Ladders that lead to cells in this room (but are in different rooms)
        [HideInInspector]public List<Cell> ladderCells = new List<Cell>();

        //Settings:
        [Header("Template Settings:")]
        [SerializeField, Tooltip("Room's local asset kit, determines how room looks.")]     private RoomAssetKit assetKit;
        [Tooltip("Indicates whether or not this is the tank's indestructible core room.")]  public bool isCore = false;
        [Tooltip("Indicates whether this room's cells can be lit on fire or not.")]         public bool isFlammable = true;
        [Button("Rotate", ButtonSizes.Small)] private void DebugRotate() { Rotate(); UpdateRoomType(type); }
        [Button("Move Up", ButtonSizes.Small)] private void DebugMoveUp() { SnapMoveTick(Vector2.up); UpdateRoomType(type); }
        [Button("Move Down", ButtonSizes.Small)] private void DebugMoveDown() { SnapMoveTick(Vector2.down); UpdateRoomType(type); }
        [Button("Move Left", ButtonSizes.Small)] private void DebugMoveLeft() { SnapMoveTick(Vector2.left); UpdateRoomType(type); }
        [Button("Move Right", ButtonSizes.Small)] private void DebugMoveRight() { SnapMoveTick(Vector2.right); UpdateRoomType(type); }
        [Button("Mount", ButtonSizes.Medium)] private void DebugMount() { Mount(); }
        [Button("Dismount", ButtonSizes.Medium)] private void DebugDismount() { Dismount(); }

        //Runtime Variables:
        [Tooltip("Which broad purpose this room serves.")]                                                                    public RoomType type;
        [Tooltip("Whether or not this room has been attached to another room yet.")]                                          internal bool mounted = false;
        [Tooltip("If true, this room will not mount normally when initialized.")]                                             public bool ignoreMount = false;
        [Tooltip("The only structure this room can be mounted to (who's home grid will be used during mounting).")]           internal IStructure targetStructure; 
        [Tooltip("The tank this room is mounted to.")]                                                                        internal TankController targetTank; //NOTE: This is important for distinguishing between rooms auto-spawned for prefab tanks, and rooms which are spawned in scrap menu for mounting on an existing tank
        [Tooltip("Value between 0 and 3 indicating which cardinal direction room is rotated in (in NESW order).")]            internal int rotTracker = 0; //NOTE: This is used for saving room rotation in json files
        [Tooltip("Sprite used for the entire back wall of this room, generated by room kit.")]                                internal SpriteRenderer backWallSprite;
        [Tooltip("Sprite shape controller drawing the outer wall of the room (connected to an object childed to the room).")] internal SpriteShapeController outerWallController;
        [Tooltip("Array of vertices (in local space) (in clockwise order) describing outer edge of cell.")]                   internal Vector2[] wallVerts;
        [Tooltip("For debug purposes, set to true to ignore the WallKitGenerator.")]                                          public bool ignoreRoomKit;

        private bool initialized = false;          //Becomes true once one-time initial room setup has been completed (indicates room is ready to be used)
        internal bool heldDuringPlacement = false; //True if this room is currently being manipulated by a player in the build scene
        private bool canBeMounted = false;         //True if the room is not mounted but cannot be mounted

        private float maxBurnTime = 24f;
        private float minBurnTime = 12f;
        private float burnTimer = 0;
        public Dictionary<string, Vector2> hatchPlacements = new(); //Key should be cell name, value should be hatch direction (up, down, left, right)

        //RUNTIME METHODS:
        private void Awake()
        {
            Initialize(); //Set everything up
        }
        public void Start()
        {
            //Check manifest:
            if (cellManifest.Length == 0) //Manifest is uninitialized
            {
                cellManifest = Enumerable.Repeat(true, cells.Count).ToArray(); //Make list match length of cell list and set all bools to true
            }

            //Core room-specific setup:
            if (isCore) //This is the tank's core room
            {
                SetUpCollision();                         //Set up cell colliders on treadsystem
                mounted = true;                           //Core rooms start mounted
                targetStructure.UpdateSizeValues(true);   //Get base structure size
                if (targetStructure.GetStructureType() == IStructure.StructureType.TANK) targetTank.treadSystem.ReCalculateMass(); //Get base tank mass
            }
        }
        private void Update()
        {
            if (cells.Count > 0) CheckFire();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Bounds bounds = GetRoomBounds();
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
            
        }
        /// <summary>
        /// Called whenever collider clone on treadsystem collides with something.
        /// </summary>
        public void OnTankCollision(Collision2D collision)
        {
            if (targetStructure.GetStructureType() == IStructure.StructureType.BUILDING) return; //ignore collision logic for buildings
            if (collision.collider.GetComponentInParent<TreadSystem>() != null) //Room has collided with another tank
            {
                //Get information:
                TreadSystem opposingTreads = collision.collider.GetComponentInParent<TreadSystem>(); //Get treadsystem of opposing tank

                if (collision.contacts.Length > 0) //Hit properties can only be handled if there is an actual contact
                {
                    //Apply collision properties:
                    ContactPoint2D contact = collision.GetContact(0);
                    opposingTreads.HandleImpact(-collision.GetContact(0).normal * 75, contact.point);
                    if (collision.collider.gameObject.layer == LayerMask.GetMask("Treads")) opposingTreads.Damage(20, true); //Room has collided directly with the treadbase
                    else collision.collider.GetComponent<CollisionTransmitter>().target.GetComponent<Cell>().Damage(20, true);
                    collision.otherCollider.GetComponent<CollisionTransmitter>().target.GetComponent<Cell>().Damage(20, true);

                    //Other effects:
                    GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
                    GameManager.Instance.AudioManager.Play("TankImpact", gameObject);
                    for (int x = 0; x < 3; x++) //Spawn particle effect
                    {
                        Vector2 offset = Random.insideUnitCircle * 0.20f;
                        GameManager.Instance.ParticleSpawner.SpawnParticle(25, contact.point + offset, 1f, collision.collider.GetComponent<CollisionTransmitter>().target.GetComponent<Cell>().transform);
                    }
                    float impactSpeed = contact.relativeVelocity.magnitude;                  //Get speed of impact from which to derive screenshake values
                    float duration = Mathf.Lerp(0.1f, 0.5f, impactSpeed / 10);               //Use a clamped lerp to increase duration of screenshake proportionately to speed of impact, up to a certain maximum speed
                    float intensity = Mathf.Lerp(0.1f, 1, impactSpeed / 10);                 //Use a clamped lerp to increase magnitude of screenshake proportionately to speed of impact, up to a certain maximum speed
                    CameraManipulator.main?.ShakeTankCamera(targetTank, intensity, duration); //Send shake command to be handled by camera manipulator (which can find the camera associated with this tank)

                    //Apply Haptics to Players inside this tank
                    foreach (Character character in targetTank.GetCharactersInTank())
                    {
                        PlayerMovement player = character.GetComponent<PlayerMovement>();
                        if (player != null)
                        {
                            HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("ImpactRumble");
                            GameManager.Instance.SystemEffects.ApplyControllerHaptics(player.GetPlayerData().playerInput, setting); //Apply haptics
                        }
                    }
                }
            }
           
            if (collision.collider.GetComponentInParent<DestructibleObject>() != null) //Room has collided with an obstacle
            {
                DestructibleObject obstacle = collision.collider.GetComponentInParent<DestructibleObject>();
                TreadSystem treads = targetTank.treadSystem;

                if (collision.contacts.Length > 0 && obstacle.isObstacle)
                {
                    //Get Point of Contact
                    ContactPoint2D contact = collision.GetContact(0);

                    //Calculate impact magnitude
                    float impactSpeed = contact.relativeVelocity.magnitude; //Get speed of impact
                    //Debug.Log("Speed of Impact: " + impactSpeed);
                    float impactDamage = (10 * Mathf.Abs(impactSpeed)) * obstacle.collisionResistance;
                    if (treads.ramming) impactDamage = 200f; //double the impact damage

                    //Apply Collision Properties
                    float knockbackForce = 75f;
                    
                    if (!treads.ramming && collision.contactCount > 0) treads.HandleImpact(collision.GetContact(0).normal * knockbackForce, contact.point);
                    else targetTank.treadSystem.SetVelocity();

                    obstacle.ApplyImpactDirection(collision.GetContact(0).normal * knockbackForce, contact.point);
                    obstacle.Damage(impactDamage);
                    collision.otherCollider.GetComponent<CollisionTransmitter>().target.GetComponent<Cell>().Damage(10, true);

                    //Other effects:
                    GameManager.Instance.AudioManager.Play("TankImpact", obstacle.gameObject);
                    for (int x = 0; x < 3; x++) //Spawn cloud of particle effects
                    {
                        Vector2 offset = Random.insideUnitCircle * 0.10f;
                        GameManager.Instance.ParticleSpawner.SpawnParticle(25, contact.point + offset, 1f);
                    }
                    float _impactSpeed = contact.relativeVelocity.magnitude;                  //Get speed of impact from which to derive screenshake values
                    float duration = Mathf.Lerp(0.1f, 0.5f, _impactSpeed / 10);               //Use a clamped lerp to increase duration of screenshake proportionately to speed of impact, up to a certain maximum speed
                    float intensity = Mathf.Lerp(0.1f, 1, _impactSpeed / 10);                 //Use a clamped lerp to increase magnitude of screenshake proportionately to speed of impact, up to a certain maximum speed
                    CameraManipulator.main?.ShakeTankCamera(targetTank, intensity, duration); //Send shake command to be handled by camera manipulator (which can find the camera associated with this tank)

                    //Apply Haptics to Players inside this tank
                    foreach (Character character in targetTank.GetCharactersInTank())
                    {
                        PlayerMovement player = character.GetComponent<PlayerMovement>();
                        if (player != null)
                        {
                            HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("ImpactJolt");
                            GameManager.Instance.SystemEffects.ApplyControllerHaptics(player.GetPlayerData().playerInput, setting); //Apply haptics
                        }
                    }
                }
            }
           
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
            print("Initializing room " + name);

            //Setup runtime variables:
            cells = new List<Cell>(GetComponentsInChildren<Cell>());                    //Get references to cells in room
            connectorParent = transform.Find("Connectors");                             //Find object containing connectors
            connectors = connectorParent.GetComponentsInChildren<Connector>().ToList(); //Get list of all connectors in room
            roomData = Resources.Load<RoomData>("RoomData");                            //Get roomData object from resources folder
            targetStructure = GetComponentInParent<IStructure>();                       //Get IStructure parent component
            targetTank = GetComponentInParent<TankController>();                        //Get tank controller from current parent (only applicable if room spawns with tank)
            dummyMat = roomData.dummyMaterials[0];                                      //Get Dummy Material used for DummyRooms

            //Get wall vertices:
            if (!ignoreRoomKit)
            {
                wallVerts = new Vector2[transform.Find("WallVerts").childCount];                                                                           //Resize vert array to fit number of wall vertices in room
                if (wallVerts.Length > 0) for (int x = 0; x < wallVerts.Length; x++) wallVerts[x] = transform.Find("WallVerts").GetChild(x).localPosition; //Get local position of each set vertex and add to vert list (making sure there is at least one present vert
            }

            //Set up child components:
            foreach (Connector connector in connectors) connector.Initialize(); //Initialize all connectors before setting up cells
            for (int x = 0; x < cells.Count; x++) cells[x].manifestIndex = x;   //Indicate to each cell what it's unique id number is
            foreach (Cell cell in cells) cell.Initialize();                     //Initialize each cell before checking adjacency
            foreach (Cell cell in cells) cell.UpdateAdjacency();                //Have all cells in room get to know each other
            ApplyRoomKit();                                                     //Apply room assets to all cells

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
            ChangeRoomColor(roomData.roomTypeColors[(int)type]);                     //Change the color of the room to the matching type
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
        /// Snaps unmounted room to closest grid point, useful for updating coupler adjacency values.
        /// </summary>
        public Vector2 SnapMove()
        {
            return SnapMove(transform.localPosition); //Snapmove using current localPosition
        }
        /// <summary>
        /// Moves unmounted room as close as possible to target position (in local space) while snapping to grid.
        /// </summary>
        /// <param name="targetPoint"></param>
        public Vector2 SnapMove(Vector2 targetPoint)
        {
            //Validity checks:
            if (mounted) //Room is already mounted
            {
                Debug.LogError("Tried to move room while it is mounted!"); //Log error
                return targetPoint;                                        //Cancel move
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
                        if (otherCell.room.targetTank == null) continue; //Collider is overlapping with a ghost room
                        //print("Cell obstructed");
                        ValidateMount();
                        return newPoint; //Generate no new couplers
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
                            if (otherRoom.targetStructure == null) { continue; } //Ignore if other cell is part of a ghost room
                            if (otherRoom == this) { continue; }  //Ignore if hit block is part of this room (happens before potential inverse check)
                            if (heldDuringPlacement && !otherRoom.mounted) { continue; } //Ignore if hit block is part of an unmounted room (prevents players from mounting floating rooms to each other in build scene)
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
                                                                                                                                                                                                     //NOTE: The line below may need edge case debugging
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
                            newCoupler.cellA = Physics2D.Raycast(newCoupler.transform.position, -Cell.cardinals[x], 0.25f,
                                LayerMask.GetMask("Cell")).collider.GetComponent<Cell>(); //Get cell in roomA closest to coupler
                            newCoupler.cellB = Physics2D.Raycast(newCoupler.transform.position, Cell.cardinals[x], 0.25f,
                                LayerMask.GetMask("Cell")).collider.GetComponent<Cell>();  //Get cell in roomB closest to coupler
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

            //Check to see if the room can be mounted
            ValidateMount();

            return newPoint;
        }

        private void ValidateMount()
        {
            //If the room is already mounted, return
            if (mounted)
                return;

            //If there are ghost couplers, it can be mounted
            canBeMounted = ghostCouplers.Count > 0;

            //If the room can be mounted, check to see if other conditions apply
            if (canBeMounted)
            {
                foreach (Coupler coupler in ghostCouplers)
                {
                    //If room B is the core
                    if (coupler.roomB.isCore)
                    {
                        float coreYPos = coupler.roomB.transform.position.y - 0.5f;
                        float marginOfError = 0.01f;    //Allow for a margin of error due to float point approximation

                        foreach (Cell cell in coupler.roomA.cells)
                        {
                            //If a cell is positioned lower than the core, the room cannot be mounted
                            if (cell.transform.position.y < coreYPos - marginOfError)
                            {
                                canBeMounted = false;
                                break;
                            }
                        }
                    }
                }
            }

            //If the room can't be mounted, ensure that the ghost couplers are cleared
            if (!canBeMounted)
            {
                foreach (Coupler coupler in ghostCouplers) Destroy(coupler.gameObject); //Destroy each ghost coupler
                ghostCouplers.Clear();                                                  //Clear list of references to ghosts
            }

            ChangeRoomColor(canBeMounted ? roomData.roomTypeColors[(int)type] : Color.red);
        }

        public void MountCouplers(List<Coupler> couplersToMount, bool mountingHatches = false)
        {
            //Remove null couplers from list:
            if (couplersToMount.Count > 0)
            {
                for (int x = 0; x < couplersToMount.Count;)
                {
                    if (couplersToMount[x] == null) couplersToMount.RemoveAt(x);
                    else x++;
                }
            }
            
            List<Coupler> couplerList = couplersToMount;
            //Mount couplers:
            foreach (Coupler coupler in couplerList) 
            {
                //Mount coupler:
                coupler.Mount();                     
                couplers.Add(coupler);               
                coupler.roomB.couplers.Add(coupler); 
                coupler.cellA.couplers.Add(coupler); 
                coupler.cellB.couplers.Add(coupler);

                if (!mountingHatches)
                {
                    List<int> hatchesToDestroy = new List<int>();
                    for (int i = 0; i < coupler.roomB.hatches.Count; i++)
                    {
                        if (coupler.roomB.hatches[i] != null && !ValidateHatch(coupler.roomB.hatches[i], coupler.roomB.hatches[i].transform.eulerAngles.z))
                        {
                            hatchesToDestroy.Add(i);
                            Debug.Log("Removed hatch");
                        } else if (coupler.roomB.hatches[i] == null) coupler.roomB.hatches.RemoveAt(i);
                    }
                    foreach (int i in hatchesToDestroy)
                    {
                        Destroy(coupler.roomB.hatches[i].gameObject);
                        coupler.roomB.hatchPlacements.Remove(coupler.cellB.name);
                        coupler.roomB.hatches.RemoveAt(i);
                    }
                }
                

                //Add ladders & platforms:
                if (coupler.transform.localRotation.z == 0) //Coupler is horizontal
                {
                    //Place initial ladder:
                    Cell cell = coupler.cellA.transform.position.y > coupler.cellB.transform.position.y ? coupler.cellB : coupler.cellA; //Pick whichever cell is below coupler
                    GameObject ladder = Instantiate(roomData.ladderPrefab, cell.transform);                                              //Instantiate ladder as child of lower cell
                    ladder.transform.position = new Vector2(coupler.transform.position.x, cell.transform.position.y);                    //Move ladder to horizontal position of coupler and vertical position of cell
                    leadingLadders.Add(ladder); //Keep track of ladder with master list
                    coupler.ladders.Add(ladder); 
                    cell.room.ladders.Add(ladder);                                                                                       //Have room ladder is in keep track of it too

                    //print("Found horizontal coupler above cell " + cell.name + ", placing ladder.");

                    //Place extra ladders:
                    if (cell.neighbors[2] != null && RoundToGrid(cell.neighbors[2].transform.position.x, 0.25f) == RoundToGrid(coupler.transform.position.x, 0.25f)) ladderCells.Add(cell); //Add cell to list of cells which need more ladders below them if cell has more southern neighbors
                }
            }
        }
        
        public void PlaceExtraLadders()
        {
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
                    leadingLadders.Add(ladder);                                             //Keep track of ladder with master list
                    cell.room.ladders.Add(ladder);
                    RaycastHit2D hit = Physics2D.Raycast(cell.transform.position, Vector2.up, 100f,
                        1 << LayerMask.NameToLayer("Coupler"));
                    if (hit)
                    {
                        hit.collider.gameObject.GetComponent<Coupler>().ladders.Add(ladder);
                    } //Check if cell has a coupler above it

                    //Place short ladder:
                    if (cell.connectors[0]) //Cell is separated from previous cell by a separator
                    {
                        ladder = Instantiate(roomData.shortLadderPrefab, cell.transform);                                              //Generate a new short ladder, childed to the cell below it
                        ladder.transform.position = Vector2.Lerp(cell.transform.position, cell.neighbors[0].transform.position, 0.5f); //Place ladder directly between cell and prev cell
                        ladder.transform.eulerAngles = Vector3.zero;                                                                   //Make sure ladder is not rotated
                        leadingLadders.Add(ladder);                                                                                    //Keep track of ladder with master list
                        cell.room.ladders.Add(ladder);                                                                                 //Have room ladder is in keep track of it too
                        if (hit)
                        {
                            hit.collider.gameObject.GetComponent<Coupler>().ladders.Add(ladder);
                        }
                    }
                }
            }
        }

        private void DestroyCouplerLadders(Coupler c)
        {
            var ladderlist = c.ladders;
            while (ladderlist.Count > 0) //Iterate until there are no more ladders in room or leading to room
            {
                GameObject ladder = c.ladders[0];                   //Get ladder from ladders list until it is empty, then start getting them from leading ladders list
                ladderlist.Remove(ladder);
                c.ladders.Remove(ladder);
                Destroy(ladder);   
                //Destroy ladder
            }
            if (c != null) c.Kill();
        }

        public void GenerateRandomHatch()
        {
            //get a dictionary pairing cells to their valid cardinals
            Dictionary<Cell, List<Vector2>> cellValidCardinals = new Dictionary<Cell, List<Vector2>>();
            
            foreach (var cell in cells)
            {
                List<Vector2> validCardinals = new List<Vector2>();
                for (int i = 0; i < 4; i++)
                {
                    if (cell.neighbors[i] == null)
                    {
                        validCardinals.Add(Cell.cardinals[i]);
                    } 
                }
                cellValidCardinals.Add(cell, validCardinals);
            }
            System.Random random = new System.Random();
            KeyValuePair<Cell, List<Vector2>> randomKVP = cellValidCardinals.ElementAt(random.Next(cellValidCardinals.Count)); // gives us a random cell

            Cell chosenCell = randomKVP.Key;
            Vector2 chosenDirection = randomKVP.Value[random.Next(randomKVP.Value.Count)]; //gets random valid direction for hatch
            Vector2 cellPos = chosenCell.transform.position; //converts position to a vector2
            Vector2 hatchPos = cellPos + (0.625f * chosenDirection);      
            
            //hatchPlacements.Add(chosenCell.transform.localPosition, chosenDirection); //inverse transform point gets the local position of the cell relative to the room
            
            Coupler newHatch = Instantiate(roomData.hatchPrefab).GetComponent<Coupler>();
            newHatch.transform.parent = transform;
            newHatch.transform.position = hatchPos;
            newHatch.transform.localPosition = RoundToGrid(newHatch.transform.localPosition, 0.125f);

            if (chosenDirection.x == 0)
            {
                newHatch.transform.localEulerAngles = Vector3.zero;
                newHatch.vertical = true;
            }
            else
            {
                newHatch.transform.localEulerAngles = Vector3.forward * 90;
                newHatch.vertical = false;
            }

            newHatch.roomA = this;
            newHatch.roomB = this;
            newHatch.cellA = chosenCell;
            newHatch.cellB = chosenCell;
            
            hatches.Add(newHatch);
            
            //List<Coupler> hatchToMount = new List<Coupler> { newHatch };
            //MountCouplers(hatchToMount);
        }

        public void GenerateTopmostHatch()
        {
            if (couplers[0].GetConnectedRoom(this).targetTank == null) return; //returns if we are not a tank, just a structure
            
            if (targetTank == null) return;
            if (cells.Any(cell => cell.transform.position.y > targetTank.rooms.SelectMany(room => room.cells).Max(tankCell => tankCell.transform.position.y))) // if any cell on this room is above the previous topmost cell
            {
                targetTank.upMostCell = cells.OrderByDescending(cell => cell.transform.position.y).First();
                ///clear previous topmost hatch

                if (targetTank.topHatch != null && targetTank.hatches.Contains(targetTank.topHatch))
                {
                    DestroyCouplerLadders(targetTank.topHatch);
                    targetTank.hatches.Remove(targetTank.topHatch);
                }
                    
                ////////////////////////////////////////////////////////////////////
                //CreateHatch(targetTank.upMostCell, Vector2.up);
                Coupler hatch = Instantiate(roomData.hatchPrefab).GetComponent<Coupler>();
                targetTank.topHatch = hatch;
                targetTank.hatches.Add(hatch);
                hatch.transform.parent = transform;
                Vector2 cellPos = targetTank.upMostCell.transform.position;
                hatch.transform.position= cellPos + (0.625f * Cell.cardinals[0]);
                hatch.transform.localPosition = RoundToGrid(hatch.transform.localPosition, 0.125f);
                hatch.roomA = this;
                hatch.roomB = this;
                
                hatch.cellA = Physics2D.Raycast(hatch.transform.position, -Cell.cardinals[0], 0.25f, LayerMask.GetMask("Cell")).collider.GetComponent<Cell>(); //Get cell in roomA closest to coupler
                hatch.cellB = hatch.cellA;  //Get cell in roomB closest to coupler
                
                List<Coupler> hatchToMount = new List<Coupler> { hatch };
                MountCouplers(hatchToMount);
                PlaceExtraLadders();
            }
        }

        /// <summary>
        /// For adding a hatch to a cell
        /// </summary>
        /// <param name="hatchCell"> The cell to add a hatch to</param>
        /// <param name="hatchDirection">The direction to install this hatch from the cell</param>
        /// <param name="mountAtThisTime">Should we also mount the hatch right after? (This is usually true, but sometimes you want it to be false)</param>
        public void CreateHatch(Cell hatchCell, Vector2 hatchDirection, bool mountAtThisTime = true, bool saveHatchToDesign = false)
        {
            Vector2 cellPosition = hatchCell.transform.position;
            Vector2 hatchPos = cellPosition + (0.625f * hatchDirection);      
            
            Coupler newHatch = Instantiate(roomData.hatchPrefab).GetComponent<Coupler>();
            newHatch.transform.parent = transform;
            newHatch.transform.position = hatchPos;
            newHatch.transform.localPosition = RoundToGrid(newHatch.transform.localPosition, 0.125f);

            if (hatchDirection.x == 0)
            {
                newHatch.transform.localEulerAngles = Vector3.zero;
                newHatch.vertical = true;
            }
            else
            {
                newHatch.transform.localEulerAngles = Vector3.forward * 90;
                newHatch.vertical = false;
            }
            
            newHatch.roomA = this;
            newHatch.roomB = this;
            newHatch.cellA = hatchCell;
            newHatch.cellB = hatchCell;
            
            hatches.Add(newHatch);
            
            if (saveHatchToDesign) hatchPlacements.Add(hatchCell.name, hatchDirection);
            if (mountAtThisTime) MountCouplers(new List<Coupler>{newHatch});
        }
        /// <summary>
        /// Rotates unmounted room around its pivot.
        /// </summary>
        public void Rotate(bool clockwise = true)
        {
            //Validity checks:
            if (mounted) { Debug.LogError("Tried to rotate room while mounted!"); return; } //Do not allow mounted rooms to be rotated

            //Move cells:
            Vector3 eulers = 90 * (clockwise ? -1 : 1) * Vector3.forward;                                                      //Get euler to rotate assembly with
            transform.Rotate(eulers);                                                                                          //Rotate entire assembly on increments of 90 degrees
            Vector2[] newCellPositions = cells.Select(cell => (Vector2)cell.transform.position).ToArray();                     //Get array of cell positions after rotation
            Vector2[] newConnectorPositions = connectors.Select(connector => (Vector2)connector.transform.position).ToArray(); //Get array of connector positions after rotation
            Vector2[] newHatchPositions = hatches.Select(hatch => (Vector2)hatch.transform.position).ToArray();
            transform.Rotate(-eulers);                                                                                         //Rotate assembly back
            for (int x = 0; x < cells.Count; x++) //Iterate through array of cells
            {
                cells[x].transform.position = newCellPositions[x]; //Move cells to their rotated positions
                
            } 
            for (int x = 0; x < connectors.Count; x++) //Iterate through array of connectors
            {
                connectors[x].transform.position = newConnectorPositions[x]; //Move connector to new position
                connectors[x].transform.Rotate(eulers);                      //Rotate connector according to rotation eulers
                //connectors[x].backWall.transform.Rotate(eulers);             //Rotate connector wall according to rotation eulers
            }

            for (int x = 0; x < hatches.Count; x++)
            {
                hatches[x].transform.position = newHatchPositions[x];
                hatches[x].transform.Rotate(eulers);
                hatches[x].vertical = !hatches[x].vertical;
            }

            //Adjust walls:
            if (backWallSprite != null) //Only adjust back wall if it is present
            {
                backWallSprite.transform.Rotate(eulers);                    //Rotate back wall to match room rotation
                backWallSprite.transform.position = GetRoomBounds().center; //Move back wall to center of room bounds

                foreach (Cell cell in cells) cell.backWall.transform.position = cell.transform.position;                          //Match position of cell's back wall to new position of cell
                foreach (Connector connector in connectors) connector.backWall.transform.position = connector.transform.position; //Match position of back wall to new position of connector
            }
            if (outerWallController != null) //Only adjust outer wall if it is present
            {
                outerWallController.transform.Rotate(eulers); //Rotate outer wall by given rotation
            }

            //Cell adjacency updates:
            foreach (Cell cell in cells) cell.ClearAdjacency();  //Clear all cell adjacency statuses first (prevents false neighborhood bugs)
            foreach (Cell cell in cells) cell.UpdateAdjacency(); //Have all cells in room get to know each other        
            SnapMove(transform.localPosition);                   //Snap to grid at current position
            ApplyRoomKit();                                      //Update cell visuals now that room orientation has changed

            foreach (Cell cell in cells) cell.transform.localPosition = new Vector2(Mathf.Round(cell.transform.localPosition.x * 4), Mathf.Round(cell.transform.localPosition.y * 4)) / 4; //Cells need to be rounded back into position to prevent certain parts from bugging out

            //Modify abstract rotation value:
            rotTracker += clockwise ? 1 : -1;      //Adjust rotation tracker depending on rot direction
            if (rotTracker > 3) rotTracker = 0; //Overflow at 3
            if (rotTracker < 0) rotTracker = 3; //Underflow at 0
        }
        
        /// <summary>
        /// Attaches this room to another room or the tank base (based on current position of the room and couplers).
        /// </summary>
        public bool Mount(bool enableSounds = false)
        {
            //Validity checks:
            if (mounted) { Debug.LogError("Tried to mount room which is already mounted!"); return true; }                              //Cannot mount rooms which are already mounted
            if (ghostCouplers.Count == 0) { Debug.Log("Tried to mount room which is not connected to any other rooms!"); return false; } //Cannot mount rooms which are not connected to the tank

            //Un-ghost couplers:
            List<Cell> ladderCells = new List<Cell>(); //Initialize list to keep track of cells which need ladders added to them
            //targetTank

            foreach (Coupler coupler in hatches)
            {
                coupler.collisionSwitcher.CheckRotation(); //removes hatch collision based on the final orientation
            }
            
            MountCouplers(ghostCouplers);
            SetTargetTankIfExistent();
            GenerateTopmostHatch();

            List<Coupler> hatchesToDestroy = new List<Coupler>(); // this is done to prevent removing elements from the same list we are iterating over
            foreach (var hatch in hatches)
            {
                if (targetTank != null && hatch == targetTank.topHatch) continue; //we dont add tophatch to hatch placements because the top hatch is generated at the start of the tank's existence, no need to save it in the design
                
                Vector2 chosenDirection = Vector2.zero;
                Vector2 direction = hatch.transform.position - hatch.cellA.transform.position;
                Debug.Log($"Hatch direction: {direction}");
                
                if (direction.x > .5f) chosenDirection = Vector2.right;
                else if (direction.x < -.5f) chosenDirection = Vector2.left;
                
                if (direction.y > .5f) chosenDirection = Vector2.up;
                else if (direction.y < -.5f) chosenDirection = Vector2.down;
                
                
                if (!ValidateHatch(hatch, hatch.transform.eulerAngles.z) || ( targetTank != null && targetTank.topHatch != null && hatch.transform.position == targetTank.topHatch.transform.position))
                {
                    hatchesToDestroy.Add(hatch);
                    Debug.Log("Destroyed hatch due to having no room");
                    continue;
                }
                hatchPlacements.Add(hatch.cellA.name, chosenDirection); //this saves the hatches
                //to the tank design, it's done here because at this point the hatch is at its final rotation
            }

            foreach (var hatch in hatchesToDestroy)
            {
                hatches.Remove(hatch);
                Destroy(hatch.gameObject);
            }
            
            MountCouplers(hatches, mountingHatches:true);
            //if (hatches.Count > 0) MountCouplers(null, true);
            PlaceExtraLadders();
            
            //Cleanup:
            if (targetStructure == null) targetStructure = couplers[0].GetConnectedRoom(this).targetStructure;  //Get target structure from a mounted room if necessary
            if (!targetStructure.GetRooms().Contains(this)) targetStructure.GetRooms().Add(this);               //Add to target structure's index of rooms
            ghostCouplers.Clear();                                                                              //Clear ghost couplers list
            if (enableSounds && !mounted) GameManager.Instance.AudioManager.Play("ConnectRoom");
            mounted = true;                                                                                     //Indicate that room is now mounted

            //Set up cell collision:

            SetUpCollision(); //Set up colliders in treadsystem so that room interacts with other tanks

            //Update tank info:
            transform.parent = couplers[0].roomB.transform.parent; //Child room to parent of the rest of the rooms (home tank)
            targetStructure.UpdateSizeValues(true);                //Check to see if any added cells are higher than the known highest cell
            targetTank?.treadSystem.ReCalculateMass();              //Re-calculate tank mass and center of mass

            return mounted;
        }

        private void SetTargetTankIfExistent()
        {
            if (targetTank == null) targetTank = couplers[0].GetConnectedRoom(this)?.targetTank;
        }

        bool ValidateHatch(Coupler hatch, float orientation)
        {
            Vector2 chosenDirection;
            Vector2 chosenOrientation;
            if (Mathf.Approximately(orientation, 0) || Mathf.Approximately(orientation, 180))
            {
                chosenOrientation = Vector2.right;
                chosenDirection = Vector2.up;
            }
            else
            {
                chosenOrientation = Vector2.up;
                chosenDirection = Vector2.right;
            }

            Vector2 upPosition = new Vector2(hatch.transform.position.x + chosenOrientation.x * .45f,
                hatch.transform.position.y + chosenOrientation.y * .45f);
            Vector2 downPosition = new Vector2(hatch.transform.position.x - chosenOrientation.x * .45f,
                hatch.transform.position.y - chosenOrientation.y * .45f);
            
            bool hitOne = Physics2D.Raycast(hatch.transform.position, chosenDirection, .4f, 1 << LayerMask.NameToLayer("Cell")) || 
                          Physics2D.Raycast(upPosition, chosenDirection, .4f, 1 << LayerMask.NameToLayer("Cell")) || 
                          Physics2D.Raycast(downPosition, chosenDirection, .4f, 1 << LayerMask.NameToLayer("Cell"));
            
            bool hitTwo = Physics2D.Raycast(hatch.transform.position, -chosenDirection, .4f, 1 << LayerMask.NameToLayer("Cell"))|| 
                          Physics2D.Raycast(upPosition, -chosenDirection, .4f, 1 << LayerMask.NameToLayer("Cell")) || 
                          Physics2D.Raycast(downPosition, -chosenDirection, .4f, 1 << LayerMask.NameToLayer("Cell")); // only doing this for now because for some reason using a 2d boxcast returns nothing. can't figure out why. so we have 6 raycasts instead. enjoy.
            
            if (hitOne && hitTwo)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Dismounts this room if it was just mounted (used for undoing in build scene).
        /// </summary>
        public void Dismount()
        {
            //Initialization:
            if (!mounted) { Debug.LogError("Tried to dismount room which is not mounted!"); return; } // Ensure the room is currently mounted

            //Coupler removal:
            while (couplers.Count > 0) couplers[0].Kill(true); //Destroy all couplers connecting this room to other rooms (their normal kill method should work fine)

            //Ladder removal:
            while (ladders.Count > 0 || leadingLadders.Count > 0) //Iterate until there are no more ladders in room or leading to room
            {
                GameObject ladder = ladders.Count > 0 ? ladders[0] : leadingLadders[0];                   //Get ladder from ladders list until it is empty, then start getting them from leading ladders list
                if (ladders.Contains(ladder)) ladders.Remove(ladder); else leadingLadders.Remove(ladder); //Remove ladder from its respective list
                Destroy(ladder);                                                                          //Destroy ladder
            }

            //Cleanup:
            if (targetStructure != null) //Structure system updates
            {
                //Remove cells from treadsystem:
                foreach (Cell cell in cells) //Each cell has a collider composite component that needs to be removed
                {
                    cell.CleanUpCollision();         //Remove collision elements and events from cell
                    cell.AddInteractablesFromCell(); //Remove the interactables and add them to the stacks
                }

                //Remove room from tank system:
                targetStructure.GetRooms().Remove(this);            // Remove room from the structure's list of rooms
                targetStructure.UpdateSizeValues(true);            //Check to see if any added cells are higher than the known highest cell
                targetTank?.treadSystem.ReCalculateMass(); //Re-calculate mass now that room has been removed
            }

            //If the room is an armored room, add it back to the stack
            if (type == RoomType.Armor)
                StackManager.AddToStack(INTERACTABLE.Armor);

            mounted = false;                   //Indicate that room is now disconnected
            SnapMove(transform.localPosition); //Re-generate ghost couplers and stuff once everything is cleaned up and room is disconnected
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
            ChangeRoomColor(newColor);  //Change the room color
        }
        /// <summary>
        /// Changes the color of the room.
        /// </summary>
        /// <param name="newColor">The new color for the room.</param>
        private void ChangeRoomColor(Color newColor)
        {
            if (backWallSprite != null)
            {
                backWallSprite.color = newColor;    //Set the back wall sprite color to new type
            }
            else
            {
                foreach (Cell cell in cells) //Iterate through each cell in room
                {
                    if (cell.backWall.TryGetComponent(out SpriteRenderer backWallRenderer))
                        backWallRenderer.color = newColor;                                //Set cell color to new type
                    //foreach (Connector connector in cell.connectors) if (connector != null) connector.backWall.color = newColor; //Set color of connector back wall
                }
            }
        }
        /// <summary>
        /// To be run as the room is being built, removes cells which were destroyed in a previous saved version of the tank.
        /// </summary>
        /// <param name="manifest">Array of bools corresponding to whether or not respective cells are included in this build of the room.</param>
        public void ProcessManifest(bool[] manifest)
        {
            //Initialization:
            cellManifest = manifest; //Save manifest to room

            //Cell Removal:
            for (int x = 0; x < cellManifest.Length; x++) //Iterate through items in manifest (not affected when cells remove themselves from room's cell list)
            {
                if (!cellManifest[x]) GetCellFromManifestNumber(x).Pull(); //Manifest in this position indicates a removed cell, reference its manifest number to find it and pull it from the room
            }
        }
        /// <summary>
        /// Applies whichever asset kit is available to this room.
        /// </summary>
        /// <param name="targetKit">Set this if you want the new room asset kit to be different than local or tank default.</param>
        public void ApplyRoomKit(RoomAssetKit targetKit = null)
        {
            //Get kit to use:
            RoomAssetKit kit = targetKit;    //First, try using the kit passed as a parameter in the method (room kit may be getting changed at runtime)
            if (kit == null) kit = assetKit; //Next, if no target kit is given, try using room's preset local asset kit (might be different from tank default)
            if (kit == null) //Room has not been given its own unique asset kit
            {
                if (targetStructure == null || targetStructure.GetRoomAssetKit() == null) { Debug.LogError("Room cannot find an asset kit to use, defaulting to placeholder assets."); return; } //Do not proceed if an asset kit cannot be acquired from the parent tank controller
                kit = targetStructure.GetRoomAssetKit(); //Get default kit from room's structure
            }
            assetKit = kit; //Save kit data to indicate which set of assets are active on room

            //Apply kit to room:
            kit.KitRoom(this); //Apply kit assets to room
        }

        public void MakeDummy(Transform newParent)
        {
            transform.parent = newParent; //transfer room to a new parent object
            this.gameObject.layer = LayerMask.NameToLayer("Dummy");
            List<Vector2> cellPositions = new List<Vector2>();

            //Conversions
            Transform[] children = transform.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                //Convert Physics Layers
                if (child.gameObject.layer == LayerMask.NameToLayer("Ground") || child.gameObject.layer == LayerMask.NameToLayer("Connector"))
                { 
                    child.gameObject.layer = LayerMask.NameToLayer("Dummy");
                }

                if (child.gameObject.layer == LayerMask.NameToLayer("Item") && child.GetComponent<Cargo>() == null)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Dummy");
                }

                //Convert Visual Layers
                SpriteRenderer renderer = child.gameObject.GetComponent<SpriteRenderer>();
                if (renderer != null && child.gameObject.layer != LayerMask.NameToLayer("Item")) 
                { 
                    renderer.sortingLayerName = "Background";
                    SortingGroup group = child.gameObject.GetComponent<SortingGroup>();
                    if (group != null) group.sortingLayerName = "Background";
                }

                SpriteShapeRenderer _renderer = child.gameObject.GetComponent<SpriteShapeRenderer>();
                if (_renderer != null)
                {
                    _renderer.sortingLayerName = "Background";
                    SortingGroup group = child.gameObject.GetComponent<SortingGroup>();
                    if (group != null) group.sortingLayerName = "Background";
                }

                ParticleSystem particle = child.gameObject.GetComponent<ParticleSystem>();
                if (particle != null)
                {
                    if (particle.name == "flames")
                    {
                        ParticleSystemRenderer part_renderer = particle.GetComponent<ParticleSystemRenderer>();
                        if (part_renderer != null) part_renderer.sortingLayerName = "Background";
                    }
                }
            }
            
            //Strip Cells of Logic
            foreach (Cell cell in cells)
            {
                //Add Position to List
                cellPositions.Add(cell.transform.localPosition);

                //Strip Interactables
                cell.AddInteractablesFromCell();

                //Strip Couplers
                foreach(Coupler coupler in cell.couplers)
                {
                    if (coupler != null)
                    {
                        GameManager.Instance.ParticleSpawner.SpawnParticle((int)Random.Range(0, 3), coupler.transform.position, 0.1f, null);
                        coupler.gameObject.SetActive(false);
                    }
                }

                //Convert to a Dummy Cell
                cell.gameObject.layer = LayerMask.NameToLayer("Dummy");
                Transform[] _children = cell.GetComponentsInChildren<Transform>();
                foreach(Transform transform in _children)
                {
                    transform.gameObject.layer = LayerMask.NameToLayer("Dummy");
                }

                //Disable the Cell Script
                cell.enabled = false;
            }

            //Add Fire to Average Cell Position
            Vector2 localAverage;
            float x = 0;
            float y = 0;
            foreach (Vector2 pos in cellPositions)
            {
                x += pos.x;
                y += pos.y;
            }
            x = x / cellPositions.Count;
            y = y / cellPositions.Count;
            localAverage = new Vector2(x, y);

            GameObject fireball = roomData.fireBackLayer;
            GameObject _fireball = Instantiate(fireball, localAverage, Quaternion.identity, this.transform);
            _fireball.transform.localPosition = localAverage;

            //Create Dummy Script
            DummyObject dummyScript = this.gameObject.AddComponent<DummyObject>();
            dummyScript.centerPoint = localAverage;

            //Add Rigidbody to Room
            Rigidbody2D rb = this.gameObject.AddComponent<Rigidbody2D>();
            rb.sharedMaterial = dummyMat;
        }
        /// <summary>
        /// Sets up collision with treadsystem so that cells in this room can collide with cells in other tanks.
        /// </summary>
        private void SetUpCollision()
        {
            if (targetStructure.GetStructureType() != IStructure.StructureType.TANK) return;
            foreach (Cell cell in cells) cell.SetUpCollision(); //Set up collision on each individual cell
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
        /// Returns the cell in this room with given manifest number (if it exists).
        /// </summary>
        private Cell GetCellFromManifestNumber(int manifestNum) { return cells.Where(cell => cell.manifestIndex == manifestNum).FirstOrDefault(); }
        /// <summary>
        /// Returns bounding box which encapsulates this room.
        /// </summary>
        public Bounds GetRoomBounds()
        {
            Bounds bounds = new Bounds();                                   //Create a bounds object to encapsulate room
            bounds.center = cells[0].transform.position;                    //Move bounds to within room (so that encapsulation isn't thrown off)
            bounds.size = Vector2.zero;                                     //Zero out side of bounds in case it still goes outside room
            foreach (Cell cell in cells) bounds.Encapsulate(cell.c.bounds); //Encapsulate bounds of each cell
            return bounds;                                                  //Return calculated bounds
        }

        public void ClearItems()
        {
            //Remove Items
            Cargo[] items = GetComponentsInChildren<Cargo>();
            if (items.Length > 0)
            {
                foreach (Cargo item in items)
                {
                    item.transform.parent = null; //removes the item from the cell before destruction
                }
            }
        }
    }
}