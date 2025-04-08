using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
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
            [Tooltip("Name of interactable prefab in folder.")] public string interRef;
            [Tooltip("True if interactable is flipped.")] public bool flipped;
            [Tooltip("List of Special Ammo Loaded into Weapon Interactable.")] public string[] specialAmmo;

            public CellInterAssignment(string cell, string interactable, bool flip = false, string[] specialAmmo = null)
            {
                this.cellName = cell;         //Assign cell name
                this.interRef = interactable; //Assign interactable reference
                this.flipped = flip;          //Assign interactable direction
                this.specialAmmo = specialAmmo; //Assign ammo (if applicable)
            }
        }
        [Serializable]
        public struct CellHatchAssignment
        {
            public string cellName;
            public Vector2 hatchDirection;
            public CellHatchAssignment(string cell, Vector2 direction)
            {
                cellName = cell;
                hatchDirection = direction;
            }
        }

        [SerializeField, Tooltip("Name of the room to build during this step")] public string roomID = "";
        [SerializeField, Tooltip("What type of room to build")] public Room.RoomType roomType;
        [SerializeField, Tooltip("Where to spawn this room inside the tank's parent transform")] public Vector3 localSpawnVector;
        [SerializeField, Tooltip("How many times to rotate the room clockwise before placing")] public int rotate = 0;
        [SerializeField, Tooltip("List of cells in the room which have an interactable, along with references to the interactable they have.")] public CellInterAssignment[] cellInteractables = { };
        [SerializeField, Tooltip("List of bools which indicates whether or not each corresponding cell in room is present.")] public bool[] cellManifest = { };
        [SerializeField, Tooltip("List of cells that have hatches in them, as well as the direction the hatch " +
                                 "should be spawned in (Candidates are: Vector2.Right, Vector2.Up," +
                                 " Vector2.Left, Vector2.Down)")] public List<CellHatchAssignment> hatches = new();
    }
}
