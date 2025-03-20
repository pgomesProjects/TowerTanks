using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    /// <summary>
    /// Structural element which connects two rooms of a tank (not necessarily associated with any given cell).
    /// </summary>
    public class Coupler : MonoBehaviour
    {
        //Objects & Components:
        private SpriteRenderer r; //Local renderer component

        [Tooltip("First room linked to this coupler.")] public Room roomA;
        [Tooltip("Second room linked to this coupler.")] public Room roomB;
        [Tooltip("Cell closest to this coupler on the first room.")] internal Cell cellA;
        [Tooltip("Cell closest to this coupler on the second room.")] internal Cell cellB;
        [Tooltip("Walls which are touching and affected by this coupler."), SerializeField] private Collider2D[] adjacentWalls;
        [HideInInspector] public List<GameObject> ladders;

        //Runtime Variables:
        [Tooltip("True if coupler is vertically oriented (hatch). False if coupler is horizontally oriented (door).")] internal bool vertical = true;

        //Settings:
        [Header("Settings:")]
        [SerializeField, Range(0, 1), Tooltip("")] private float ghostOpacity = 0.5f;

        [SerializeField] private Collider2D lockedCollider;
        [SerializeField] private GameObject lockedVisual;
        public bool locked { get; private set; }

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

            List<Collider2D> combinedWalls = new List<Collider2D>();
            if (cellA != null) combinedWalls.AddRange(cellA.walls.Select(wall => wall.GetComponent<Collider2D>()));
            if (cellB != null) combinedWalls.AddRange(cellB.walls.Select(wall => wall.GetComponent<Collider2D>()));

            // Sort the combined walls by their distance to the coupler's position. 
            combinedWalls = combinedWalls.OrderBy(wall => Vector2.Distance(wall.transform.position, transform.position)).ToList();

            // Get the 2 closest walls. this should return the walls that the coupler is connected to
            adjacentWalls = combinedWalls.Take(2).ToArray();                                                                                                                                   //Store found walls in local array

            //Make holes in wall colliders:
            for (int x = 0; x < adjacentWalls.Length; x++) //Iterate through each wall adjacent to coupler
            {
                //Gather initial data:
                BoxCollider2D wall = adjacentWalls[x].GetComponent<BoxCollider2D>(); //Get box collider component corresponding to each wall (needs to be more specific than generic collider type)
                Vector2 wallOffset = transform.position - wall.transform.position;   //Get position of wall relative to position of coupler

                //Single wall bisection:
                if (vertical && Mathf.Abs(wallOffset.x) < 0.125f || !vertical && Mathf.Abs(wallOffset.y) < 0.125f) //Wall is directly aligned with coupler (in either orientation) (with rounding to account for positional error)
                {
                    BoxCollider2D newWall = wall.gameObject.AddComponent<BoxCollider2D>();            //Generate a new wall (because current wall will be bisected)
                    Vector2 splitWallOffset = (vertical ? Vector2.right : Vector2.up) * 0.45f;        //Get value for modifying individual collider offsets (moves them to corners of cell)
                    Vector2 splitWallSize = (vertical ? new Vector2(0.1f, 1) : new Vector2(1, 0.1f)); //Get value for modifying individual collider sizes (changes them into cubes, accounts for scaled dimension)
                    wall.offset = splitWallOffset; newWall.offset = -splitWallOffset;                 //Move walls to opposite corners of cell
                    wall.size = splitWallSize; newWall.size = splitWallSize;                          //Scale walls into 0.11x0.11 cubes (one dimension is already scaled by object transform)
                    continue;                                                                         //New wall colliders have been computed, move to next wall
                }

                //Offset wall modification:
                wall.size = vertical ? new Vector2(Mathf.Abs(wallOffset.x) + 0.1f, 1) : new Vector2(1, Mathf.Abs(wallOffset.y) + 0.1f);                                                                                               //Set wall size based on how much of the coupler intersects it
                wall.offset = vertical ? new Vector2((0.45f - (0.125f * 4 * Mathf.Abs(wallOffset.x))) * -Mathf.Sign(wallOffset.x), 0) : new Vector2(0, (0.45f - (0.125f * 4 * Mathf.Abs(wallOffset.y))) * -Mathf.Sign(wallOffset.y)); //Set wall offset based on direction wall is offset by and how much area it will cover with its new size

                //Cell data update:
                Cell cell = wall.GetComponentInParent<Cell>();              //Get cell containing wall
                if (!cell.couplers.Contains(this)) cell.couplers.Add(this); //Indicate that this coupler is adjacent to this cell
            }

            //Cleanup:
            if (r != null) { Color newColor = r.color; newColor.a = 1; r.color = newColor; }    //Remove ghost transparency
            mounted = true;                                                                     //Indicate that coupler is mounted
        }
        
        [Button]
        public void LockCoupler()
        {
            lockedCollider.enabled = true;
            lockedVisual.SetActive(true);
            gameObject.layer = LayerMask.NameToLayer("StopPlayer");
            locked = true;
        }
        [Button]
        public void UnlockCoupler()
        {
            lockedCollider.enabled = false;
            lockedVisual.SetActive(false);
            gameObject.layer = LayerMask.NameToLayer("Coupler");
            locked = false;
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
        /// <summary>
        /// If thisCell is adjacent to this coupler, returns closest cell on the other end of the coupler.
        /// </summary>
        public Cell GetOtherCell(Cell thisCell)
        {
            //Validity checks:
            if (!thisCell.couplers.Contains(this)) //Given cell is not adjacent to coupler
            {
                Debug.LogError("GetOtherCell was given a cell which is not adjacent to it as a parameter."); //Indicate error
                return null;                                                                                 //Return nothing
            }

            //Determine closest cell on other side:
            if (thisCell == cellA) return cellB;      //Simply return alternate cell if given cell is recognized
            else if (thisCell == cellB) return cellA; //Simply return alternate cell if given cell is recognized
            else //Divide neighboring cells into sides A and B and figure out which one given cell is on
            {
                Cell[] adjacentCells = adjacentWalls.Select(wall => wall.GetComponentInParent<Cell>()).ToArray(); //Get array of cells adjacent to coupler
                Cell[] adjacentCellsA = adjacentCells.Where(cell => SameSide(cell, cellA)).ToArray();             //Get array of cells on side A of coupler
                if (adjacentCellsA.Contains(thisCell)) return cellB; //Return alternate cell if given cell is on side A
                Cell[] adjacentCellsB = adjacentCells.Where(cell => SameSide(cell, cellB)).ToArray();             //Get array of cells on side B of coupler
                if (adjacentCellsB.Contains(thisCell)) return cellB; //Return alternate cell if given cell is on side B
            }

            //Could not find cell:
            Debug.LogError("GetOtherCell failed to find which side of coupler given cell was on."); //Indicate error
            return null;                                                                            //Return nothing
        }
        /// <summary>
        /// Destroys this coupler and cleans up all references to it.
        /// </summary>
        /// <param name="nonDestructive">Pass true to prevent this action from potentially killing any cells.</param>
        public void Kill(bool nonDestructive = false)
        {
            //Reference cleanup:
            foreach (Collider2D wall in adjacentWalls) //Iterate through each wall affected by this coupler
            {
                if (wall == null) continue; //Skip cell walls which are already being destroyed
                                            //NOTE: CHECK FOR PLAYER INSIDE
                Cell cell = wall.GetComponentInParent<Cell>();                          //Get cell associated with this wall

                //Fix cell walls:
                RaycastHit2D hit = Physics2D.Raycast(transform.position, wall.transform.position - cell.transform.position, .5f, 1 << LayerMask.NameToLayer("Cell")); // if there is a cell in our destroyed coupler direction, we don't want to change the collider size back
                                                                                                                                                                 // this is required for hatches to work in the build scene correctly for now
                if (!Mathf.Approximately(transform.eulerAngles.z, 0)) hit = new RaycastHit2D(); // if the coupler is vertical, we want to change the collider size back
                BoxCollider2D[] actualWalls = wall.GetComponents<BoxCollider2D>(); //Get box collider component(s) from wall
                if (!hit) actualWalls[0].size = Vector2.one;                                 //Change wall size back to default
                if (!hit) actualWalls[0].offset = Vector2.zero;                              //Move wall back to default position
                if (actualWalls.Length > 1) Destroy(actualWalls[1]);               //If there is a second (split) wall, destroy it
                roomA.ladderCells.Remove(cellA);

                //Remove from lists:
                
                if (cell.room.couplers.Contains(this)) cell.room.couplers.Remove(this); //Remove this coupler from memory of parent room
                if (cell.couplers.Contains(this)) cell.couplers.Remove(this);           //Remove this coupler from memory of all adjacent cells
                if (!nonDestructive) cell.KillIfDisconnected();                         //Check to see if cell has been disconnected by this and destroy it if this is the case
            }

            //Final cleanup:
            if (gameObject != null) Destroy(gameObject); //Destroy this coupler
        }
        /// <summary>
        /// Returns true if both cells are on the same side of the coupler
        /// </summary>
        private bool SameSide(Cell cell1, Cell cell2)
        {
            if (vertical && Mathf.Sign(cell1.transform.localPosition.y - transform.localPosition.y) == Mathf.Sign(cell2.transform.localPosition.y - transform.localPosition.y)) return true;       //Return true if coupler is in hatch orientation and both cells are on the same side
            else if (!vertical && Mathf.Sign(cell1.transform.localPosition.x - transform.localPosition.x) == Mathf.Sign(cell2.transform.localPosition.x - transform.localPosition.x)) return true; //Return true if coupler is in door orientation and both cells are on the same side
            else return false;                                                                                                                                                                     //Otherwise, return false

        }
        
        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.magenta;
        //     Vector2 size = new Vector2(0.79f, 0.25f + 0.1f);
        //     Gizmos.DrawWireCube(transform.position, size);
        // }
    }
}
