using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class BuildStep
{
    [Serializable]
    /// <summary>
    /// Indicates the name of a cell with an interactable and the name of the interactable to install.
    /// </summary>
    public struct CellInterAssignment
    {
        [Tooltip("Name of the cell with the interactable.")] public string cellName;
        [Tooltip("Name of interactable prefab in folder.")]  public string interRef;
        [Tooltip("True if interactable is flipped.")]        public bool flipped;

        public CellInterAssignment(string cell, string interactable, bool flip = false)
        {
            this.cellName = cell;         //Assign cell name
            this.interRef = interactable; //Assign interactable reference
            this.flipped = flip;          //Assign interactable direction
        }
    }

    [SerializeField, Tooltip("Name of the room to build during this step")]                                                                 public string roomID = "";
    [SerializeField, Tooltip("What type of room to build")]                                                                                 public Room.RoomType roomType;
    [SerializeField, Tooltip("Where to spawn this room inside the tank's parent transform")]                                                public Vector3 localSpawnVector;
    [SerializeField, Tooltip("How many times to rotate the room clockwise before placing")]                                                 public int rotate = 0;
    [SerializeField, Tooltip("List of cells in the room which have an interactable, along with references to the interactable they have.")] public CellInterAssignment[] cellInteractables = { };
}
