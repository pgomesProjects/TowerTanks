using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Tooltip("Controller for elements which players (and enemies) interact with to control and use the tank.")]
public class TankInteractable : MonoBehaviour
{
    //Objects & Components:
    private SpriteRenderer[] renderers;    //Array of all renderers in interactable
    private protected TankController tank; //Controller script for tank this interactable is attached to
    private InteractableZone interactZone; //Hitbox for player detection
    public Transform seat; //Transform operator snaps to while using this interactable

    //Interactable Scripts
    private GunController gunScript;
    private EngineController engineScript;
    private ThrottleController throttleScript;

    //Settings:
    [Header("Placement Constraints:")]
    [Tooltip("The room type this interactable is designed to be placed in.")] public Room.RoomType type;
    //ADD SPATIAL CONSTRAINT SYSTEM

    //Runtime Variables:
    [Tooltip("The cell this interactable is currently installed within.")]   internal Cell parentCell;
    [Tooltip("True if interactable is a ghost and is currently unuseable.")] internal bool ghosted;
    [Tooltip("True if a user is currently operating this system")]           public bool hasOperator;
    [Tooltip("User currently interacting with this system.")]                internal PlayerMovement operatorID;
    [Tooltip("Direction this interactable is facing. (1 = right; -1 = left)")] public float direction = 1;

    //Debug
    public bool flip = false;
    private float introBuffer = 0.2f; //small window when a new operator enters the interactable where they can't use it
    private float cooldown;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        renderers = GetComponentsInChildren<SpriteRenderer>(); //Get all spriterenderers for interactable visual
        interactZone = GetComponentInChildren<InteractableZone>();
        seat = transform.Find("Seat");
        gunScript = GetComponent<GunController>();
        engineScript = GetComponent<EngineController>();
        throttleScript = GetComponent<ThrottleController>();

    }
    private void OnDestroy()
    {
        //Destruction Cleanup:
        if (parentCell != null) //Interactable is mounted in a cell
        {
            if (parentCell.ghostInteractable == this) parentCell.ghostInteractable = null;                                           //Remove ghost reference from parent cell
            if (parentCell.installedInteractable == this) parentCell.installedInteractable = null;                                   //Remove reference from parent cell
            if (parentCell.room != null && parentCell.room.interactables.Contains(this)) parentCell.room.interactables.Remove(this); //Remove reference from parent room
        }

        if (hasOperator)
        {
            Exit(false);
        }
    }

    public void FixedUpdate()
    {
        if (operatorID != null)
        {
            operatorID.gameObject.transform.position = seat.position;
            //operatorID.gameObject.transform.rotation = seat.rotation;
        }

        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
        }

        if (flip) { flip = false; Flip(); }
    }

    public void LockIn(GameObject playerID) //Called from InteractableZone.cs when a user locks in to the interactable
    {
        hasOperator = true;
        operatorID = playerID.GetComponent<PlayerMovement>();

        if (operatorID != null)
        {
            operatorID.currentInteractable = this;
            operatorID.isOperator = true;
            operatorID.gameObject.GetComponent<Rigidbody2D>().isKinematic = true;

            Debug.Log(operatorID + " is in!");
            GameManager.Instance.AudioManager.Play("UseSFX");

            if (cooldown <= 0) cooldown = introBuffer;
        }
    }

    public void Exit(bool sameZone) //Called from operator (PlayerMovement.cs) when they press Cancel
    {
        if (operatorID != null)
        {
            operatorID.currentInteractable = null;
            operatorID.isOperator = false;
            operatorID.gameObject.GetComponent<Rigidbody2D>().isKinematic = false;
            operatorID.CancelInteraction();

            if (!interactZone.players.Contains(operatorID.gameObject) && sameZone) 
            {
                interactZone.players.Add(operatorID.gameObject); //reassign operator to possible interactable players
                operatorID.currentZone = interactZone;
            }

            hasOperator = false;
            Debug.Log(operatorID + " is out!");

            operatorID = null;
            
            GameManager.Instance.AudioManager.Play("ButtonCancel");
        }
    }

    public void Use() //Called from operator when they press Interact
    {
        if (type == Room.RoomType.Weapons)
        {
            if (gunScript != null && cooldown <= 0) gunScript.Fire();
        }

        if (type == Room.RoomType.Engineering)
        {
            if (engineScript != null && cooldown <= 0) engineScript.LoadCoal(1);
        }
    }

    public void Shift(int direction) //Called from operator when they flick L-Stick L/R
    {
        if (type == Room.RoomType.Command)
        {
            if (throttleScript != null && cooldown <= 0)
            {
                throttleScript.UseThrottle(direction);
                cooldown = 0.1f;
            }
        }
    }

    public void Rotate(float force) //Called from operator when they rotate the joystick
    {
        if (type == Room.RoomType.Weapons)
        {
            if (gunScript != null && cooldown <= 0) gunScript.RotateBarrel(force);
        }
    }

    public void SecondaryUse(bool held)
    {
        if (type == Room.RoomType.Engineering)
        {
            if (engineScript != null) engineScript.repairInputHeld = held;
        }
    }

    //UTILITY METHODS:
    /// <summary>
    /// Installs interactable into target cell.
    /// </summary>
    /// <param name="target">The cell interactable will be installed in.</param>
    /// <param name="installAsGhost">Pass true to install interactable as a ghost.</param>
    public bool InstallInCell(Cell target, bool installAsGhost = false)
    {
        //Universal installation:
        parentCell = target;                       //Get reference to target cell
        transform.parent = parentCell.transform;   //Child to target cell
        transform.localPosition = Vector3.zero;    //Match position with target cell
        transform.localEulerAngles = Vector3.zero; //Match rotation with target cell

        //Optional installation modes:
        if (installAsGhost) //Interactable is being installed as a ghost
        {
            ChangeGhostStatus(true);             //Put interactable into ghost mode
            parentCell.ghostInteractable = this; //Designate this as its parent's ghost interactable
        }
        else //Interactable is being fully installed
        {
            parentCell.installedInteractable = this; //Designate this as its parent's installed interactable
            parentCell.room.interactables.Add(this); //Add reference to parent room
        }

        //Cleanup:
        tank = GetComponentInParent<TankController>(); //Get tank controller interactable is being attached to
        return true;                                   //Indicate that interactable was successfully installed in target cell
    }
    /// <summary>
    /// Changes interactactable to or from a ghost of itself.
    /// </summary>
    /// <param name="makeGhost">Pass true to make interactable a ghost, false to make interactable real (you should generally destroy interactables instead of turning them back into ghosts).</param>
    public void ChangeGhostStatus(bool makeGhost = false)
    {
        //Validity checks:
        if (makeGhost == ghosted) return; //Ignore if command is redundant

        //Change visuals:
        foreach (SpriteRenderer r in renderers)
        {
            Color newColor = r.color;          //Get color from sprite
            newColor.a = makeGhost ? 0.5f : 1; //Make color transparent/opaque
            r.color = newColor;                //Apply new color to sprite
        }

        //Update status:
        ghosted = makeGhost; //Update status indicator
        if (parentCell != null && !makeGhost) //Interactable is a ghost being committed to its parent cell
        {
            parentCell.ghostInteractable = null;     //Clear ghost interactable slot
            parentCell.installedInteractable = this; //Make this parent's installed interactable
            parentCell.room.interactables.Add(this); //Add reference to parent room
        }
    }

    public void Flip()
    {
        if (direction == 1)
        {
            transform.Rotate(new Vector3(0, 180, 0));
            direction = -1;
        }
        else
        {
            transform.Rotate(new Vector3(0, -180, 0));
            direction = 1;
        }
    }
}
