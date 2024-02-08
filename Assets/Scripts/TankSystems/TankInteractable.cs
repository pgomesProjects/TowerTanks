using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Tooltip("Controller for elements which players (and enemies) interact with to control and use the tank.")]
public class TankInteractable : MonoBehaviour
{
    //Objects & Components:
    private SpriteRenderer[] renderers; //Array of all renderers in interactable
    [Tooltip("The cell this interactable is currently installed within.")] internal Cell parentCell;

    //Settings:
    [Header("Placement Constraints:")]
    [SerializeField, Tooltip("The room type this interactable is designed for (set when designing interactable).")] protected Room.RoomType type;

    //Runtime Variables:
    [Tooltip("True if interactable is a ghost and is currently unuseable.")] internal bool ghosted;
    [Tooltip("User currently interacting with this system.")]                internal PlayerMovement user;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        renderers = GetComponentsInChildren<SpriteRenderer>(); //Get all spriterenderers for interactable visual
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
        parentCell = target;                     //Get reference to target cell
        transform.parent = parentCell.transform; //Child to target cell
        transform.localPosition = Vector3.zero;  //Match position with target cell

        //Optional installation modes:
        if (installAsGhost) //Interactable is being installed as a ghost
        {
            ChangeGhostStatus(true);             //Put interactable into ghost mode
            parentCell.ghostInteractable = this; //Designate this as its parent's ghost interactable
        }
        else //Interactable is being fully installed
        {
            parentCell.installedInteractable = this; //Designate this as its parent's installed interactable
        }

        //Cleanup:
        return true; //Indicate that interactable was successfully installed in target cell
    }
    /// <summary>
    /// Changes interactactable to or from a ghost of itself.
    /// </summary>
    /// <param name="makeGhost">Pass true to make interactable a ghost, false to make interactable real.</param>
    public void ChangeGhostStatus(bool makeGhost)
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
        }
    }
}
