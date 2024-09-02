using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;

/// <summary>
/// Square component which rooms are made of.
/// </summary>
public class Cell : MonoBehaviour
{
    //Static Variables:
    public static Vector2[] cardinals = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

    //Objects & Components:
    internal BoxCollider2D c;  //Cell's local collider
    internal Room room; //The room this cell is a part of.
    /// <summary>
    /// Array of up to four cells which directly neighbor this cell.
    /// Includes cells in other rooms.
    /// Order is North (0), West (1), South (2), then East (3).
    /// </summary>
    internal Cell[] neighbors = new Cell[4];
    /// <summary>
    /// Array of up to four optional connector (spacer) pieces which attach this room to its corresponding neighbor.
    /// Connectors indicate splits between room sections.
    /// </summary>
    internal Connector[] connectors = new Connector[4]; //Connectors adjacent to this cell (in NESW order)
    /// <summary>
    /// Couplers adjacent to this cell (connected couplers will modify cell walls).
    /// </summary>
    internal List<Coupler> couplers = new List<Coupler>();

    //UI
    internal SpriteRenderer damageSprite;

    [Header("Cell Components:")]
    [Tooltip("Back wall of cell, will be changed depending on cell purpose.")]                  public GameObject backWall;
    [Tooltip("Pre-assigned cell walls (in NESW order) which confine players inside the tank.")] public GameObject[] walls;
    [SerializeField, Tooltip("The interactable currently installed in this cell (if any).")]    internal TankInteractable interactable;
    [SerializeField, Tooltip("Transform used for repairmen to snap to when repairing this cell")] public Transform repairSpot;
    [SerializeField, Tooltip("The character currently repairing this cell")]                      public GameObject repairMan;
    [SerializeField, Tooltip("Sprites used for showing damage on the cell")]                    public SpriteRenderer[] diageticDamageSprites;
    public enum TankPosition { TOP = 1, BOTTOM = -1 };

    //Settings:
    [Button("Debug Destroy Cell")] public void DebugDestroyCell() { Kill(); }
    [Button("Debug Damage Cell")] public void DebugDamageCell() { Damage(50f); }

    //Runtime Variables:
    //Gameplay:
    [Tooltip("Maximum hitpoints this cell can have when fully repaired.")] public float maxHealth;
    [InlineButton("RepairCell", SdfIconType.Wrench, "")]
    [Tooltip("Current hitpoints this cell has.")]                          public float health;
    [Tooltip("Which section this cell is in inside its parent room.")]     internal int section;
    [Tooltip("How long damage visual effect persists for")]                private float damageTime;
                                                                           private float damageTimer;
        //Meta
    [Tooltip("True if cell destruction has already been scheduled, used to prevent conflicts.")] private bool dying;
    [Tooltip("True once cell has been set up and is ready to go.")]                              private bool initialized = false;
    [Tooltip("Player which is currently building an interactable in this cell.")]                internal PlayerMovement playerBuilding;

    //RUNTIME METHODS:
    private void Awake()
    {
        Initialize(); //Set up cell
    }

    private void Start()
    {
        //Assign Values
        if (room.type == Room.RoomType.Defense) maxHealth *= 2f; //Armor has 2x hitpoints
        health = maxHealth;
    }

    private void Update()
    {
        if (damageTimer > 0)
        {
            UpdateUI();
        }
    }

    //RUNTIME METHODS:
    private void UpdateUI()
    {
        //Flash Effect
        Color newColor = damageSprite.color;
        newColor.a = Mathf.Lerp(0, 255f, (damageTimer / damageTime) * Time.deltaTime);
        damageSprite.color = newColor;

        damageTimer -= Time.deltaTime;
        if (damageTimer < 0)
        {
            damageTimer = 0;
            damageTime = 0;
        }

        //Diagetic Damage Sprites
        if (health < maxHealth)
        {
            diageticDamageSprites[0].enabled = true;

            if (health <= (maxHealth * 0.75f)) { diageticDamageSprites[1].enabled = true; }
            else diageticDamageSprites[1].enabled = false;

            if (health <= (maxHealth * 0.5f)) { diageticDamageSprites[2].enabled = true; }
            else diageticDamageSprites[2].enabled = false;

            if (health <= (maxHealth * 0.25f)) { diageticDamageSprites[3].enabled = true; }
            else diageticDamageSprites[3].enabled = false;
        }
        else
        {
            foreach(SpriteRenderer sprite in diageticDamageSprites)
            {
                sprite.enabled = false;
            }
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Performs all necessary setup so that cell is ready to use.
    /// </summary>
    public void Initialize()
    {
        //Initialization check:
        if (initialized) return; //Do not attempt to re-initialize cell
        initialized = true;      //Indicate that cell has been initialized

        //Get objects & components:
        room = GetComponentInParent<Room>(); //Get room cell is connected to
        c = GetComponent<BoxCollider2D>();   //Get local collider
        health = maxHealth;
        damageSprite = transform.Find("DiageticUI")?.GetComponent<SpriteRenderer>();
    }
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
            List<RaycastHit2D> hits = Physics2D.RaycastAll(transform.position, cardinals[x], 1, (LayerMask.GetMask("Cell") | LayerMask.GetMask("Connector"))).ToList(); //Check for adjacent cell in given direction
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
            }
            foreach (RaycastHit2D hit in hits) //Iterate through remaining hit objects (now that neighbors have been populated)
            {
                //Update connectors:
                if (hit.transform.GetComponentInParent<Connector>() != null) //Adjacent connector found in current direction
                {
                    //Connector updates:
                    Connector connector = hit.transform.GetComponentInParent<Connector>(); //Get found connector object
                    if (connector.room != room) continue;                                  //Skip connectors from other rooms
                    connectors[x] = connector;                                             //Save information about connector to slot in current direction
                    connector.cellA = this;                                                //Indicate to connector that it is attached to this cell
                    
                    //Neighbor updates:
                    neighbors[x].connectors[(x + 2) % 4] = connector; //Give connection information to neighbor
                    connector.cellB = neighbors[x];                   //Indicate to connector that it is attached to neighbor
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
    /// Deals given amount of damage to the cell.
    /// </summary>
    public float Damage(float amount)
    {
        float tempHealth = health;
        float healthLost = 0;

        if (room.isCore)
        {
            if (room.targetTank.isInvincible == false) {
                room.targetTank.Damage(amount);
            }
        }
        else
        {
            if (room.type == Room.RoomType.Defense) amount -= 25f; //Armor reduces incoming damage
            if (room.targetTank.isInvincible) amount = 0;
            if (amount < 0) { amount = 0; }
            else
            {
                damageTime += (amount / 50f);
                damageTimer = damageTime;
            }
            health -= amount;

            if (health < 0) health = 0;
            healthLost = tempHealth - health;
            if (room.type == Room.RoomType.Defense) healthLost += 25f;

            if (health <= 0) Kill();
        }

        return healthLost;
    }
        /// <summary>
        /// Checks to see if this cell has been disconnected from the tank and then kills it if it has been.
        /// </summary>
        public void KillIfDisconnected()
    {
        //Validity checks:
        if (dying) return; //Do not try to kill a cell twice

        //Check connection:
        List<Cell> connectedCells = new List<Cell>(); //Initialize a list to store cells found to be connected to this cell
        connectedCells.Add(this);                     //Seed list with this cell
        for (int y = 0; y < connectedCells.Count; y++) //Iterate through list of cells connected to detached neighbor, populating as we go
        {
            //Check cell identity:
            Cell currentCell = connectedCells[y]; //Get current cell
            if (currentCell.room.isCore) return; //Abort if cell chain is connected to core

            //Populate neighbor list:
            for (int z = 0; z < 4; z++) //Iterate through neighbors of connected cell
            {
                Cell neighbor = currentCell.neighbors[z];                                                 //Get current neighbor
                if (neighbor != null && !connectedCells.Contains(neighbor)) connectedCells.Add(neighbor); //Add neighbor if it isn't already found in list
            }
            foreach (Coupler coupler in currentCell.couplers) //Iterate through couplers connected to current cell
            {
                Cell couplerNeighbor = coupler.GetOtherCell(currentCell);                                                      //Get neighbor through coupler
                if (couplerNeighbor != null && !connectedCells.Contains(couplerNeighbor)) connectedCells.Add(couplerNeighbor); //Add neighbor if it isn't already found in list
            }
        }
        Kill(); //Kill cell if it has not found a connection to the core
    }
    /// <summary>
    /// Obliterates cell and updates adjacency info for other cells.
    /// </summary>
    /// <param name="proxy">Pass true when cell is being destroyed by another cell destruction method.</param>
    /// <param name="spareIfConnected">If true, cell will only be destroyed if it is currently disconnected from the rest of the tank.</param>
    public void Kill(bool proxy = false)
    {
        //Validity checks:
        if (room.isCore) return; //Do not allow cells in core room to be destroyed
        if (dying) return;       //Do not try to kill a cell twice (happens in certain edge cases
        dying = true;            //Indicate that cell is now dying

        //Adjacency cleanup:
        List<Cell> detachedNeighbors = new List<Cell>(); //Create list to store neighbors which have been detached from this cell
        for (int x = 0; x < 4; x++) //Iterate through neighbor and connector array
        {
            if (neighbors[x] != null) //Cell has a neighbor in this direction
            {
                neighbors[x].neighbors[(x + 2) % 4] = null; //Clear neighbor reference to this cell
                detachedNeighbors.Add(neighbors[x]);        //Add neighbor to list of detached cells
            }
        }
        for (int x = 0; x < couplers.Count; x++) //Iterate through couplers adjacent to cell
        {
            detachedNeighbors.Add(couplers[x].GetOtherCell(this)); //Get cell on other side of coupler and add to detached neighbor list
            //NOTE: Delete floating couplers
        }

        //Breakoff detection:
        if (!proxy && detachedNeighbors.Count > 0) //Cells being destroyed by breakoff calculation of another cell do not need to do their own calculation
        {
            for (int x = 0; x < detachedNeighbors.Count;) //Iterate until at the end of detached neighbors list (list may be edited mid-iteration)
            {
                List<Cell> connectedCells = new List<Cell>(); //Initialize a list to store cells found to be connected to detached cell
                connectedCells.Add(detachedNeighbors[x]);     //Seed list with cell from detached neighbors list
                for (int y = 0; y < connectedCells.Count; y++) //Iterate through list of cells connected to detached neighbor, populating as we go
                {
                    //Check cell identity:
                    Cell currentCell = connectedCells[y]; //Get current cell
                    if (currentCell.room.isCore) //Core room has been found, neighborhood will not be detached
                    {
                        foreach (Cell cell in connectedCells) if (detachedNeighbors.Contains(cell)) detachedNeighbors.Remove(cell); //Remove all cells in found neighborhood from potential detachments list (because they are safely connected)
                        connectedCells.Clear();                                                                                     //Clear neighborhood list to indicate that none of these cells should be destroyed
                        break;                                                                                                      //Break neighborhood population loop and continue to other direct neighbors of destroyed cell
                    }

                    //Populate neighbor list:
                    for (int z = 0; z < 4; z++) //Iterate through neighbors of connected cell
                    {
                        Cell neighbor = currentCell.neighbors[z];                                                 //Get current neighbor
                        if (neighbor != null && !connectedCells.Contains(neighbor)) connectedCells.Add(neighbor); //Add neighbor if it isn't already found in list
                    }
                    foreach (Coupler coupler in currentCell.couplers) //Iterate through couplers connected to current cell
                    {
                        Cell couplerNeighbor = coupler.GetOtherCell(currentCell);                                                      //Get neighbor through coupler
                        if (couplerNeighbor != null && !connectedCells.Contains(couplerNeighbor)) connectedCells.Add(couplerNeighbor); //Add neighbor if it isn't already found in list
                    }
                }

                //Neighborhood destruction:
                if (connectedCells.Count > 0) //Neighborhood is no longer connected to tank base
                {
                    detachedNeighbors.RemoveAt(x);        //Indicate that this neighborhood has been dealt with
                    foreach (Cell cell in connectedCells) //Iterate through each cell in disconnected neighborhood
                    {
                        cell.Kill(true); //Destroy cell (proxy setting prevents them from each having to separately check for breakoff)
                    }
                }
            }
        }

        //Non-object cleanup:
        foreach (Connector connector in connectors) { if (connector != null) connector.Damage(this); } //Indicate to each attached connector that cell has been destroyed
        while (couplers.Count > 0) //Destroy connected couplers until none are left
        {
            Coupler coupler = couplers[0];                            //Get reference to target coupler
            coupler.Kill();                                           //Destroy target coupler (it should remove itself from coupler list here
            if (couplers.Contains(coupler)) couplers.Remove(coupler); //Make SURE coupler has been removed from list
        }
        if (playerBuilding) playerBuilding.StopBuilding(); //Force player to stop building if a player is building in this cell

        //Room cleanup:
        room.cells.Remove(this);                                   //Remove this cell from room cell list
        if (!proxy) room.targetTank.treadSystem.ReCalculateMass(); //Re-calculate tank mass based on new cell configuration (only needs to be done once for group cell destructions)

        //Other Effects
        GameManager.Instance.AudioManager.Play("MedExplosionSFX", gameObject);
        GameManager.Instance.ParticleSpawner.SpawnParticle(5, transform.position, 0.15f, null);

        //Cleanup:
        Character player = GetComponentInChildren<Character>();
        if (player != null)
        {
            player.transform.parent = null; // removes the player from the cell before destruction if present
        }
        if (room.targetTank != null && room.targetTank.tankType == TankId.TankType.PLAYER && interactable != null) StackManager.AddToStack(interactable); //Add interactable data to stack upon destruction (if it is in a player tank)
        room.targetTank.UpdateHighestCell(); //Update highest cell tracker
        Destroy(gameObject);                 //Destroy this cell
    }

    private void RepairCell()
    {
        Repair(25);
    }
    public void Repair(float amount)
    {
        if (health < maxHealth)
        {
            health += amount;
            damageTime += (amount / 50f);
            damageTimer = damageTime;
            GameManager.Instance.AudioManager.Play("UseWrench", gameObject);
            GameManager.Instance.ParticleSpawner.SpawnParticle(6, transform.position, 0.25f, null);
            GameManager.Instance.ParticleSpawner.SpawnParticle(7, transform.position, 0.25f, null);
        }
        
        if (health > maxHealth) { health = maxHealth; }
        
    }
}
