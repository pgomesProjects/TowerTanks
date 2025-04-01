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

        [SerializeField, Tooltip("Name of the room to build during this step")] public string roomID = "";
        [SerializeField, Tooltip("What type of room to build")] public Room.RoomType roomType;
        [SerializeField, Tooltip("Where to spawn this room inside the tank's parent transform")] public Vector3 localSpawnVector;
        [SerializeField, Tooltip("How many times to rotate the room clockwise before placing")] public int rotate = 0;
        [SerializeField, Tooltip("List of cells in the room which have an interactable, along with references to the interactable they have.")] public CellInterAssignment[] cellInteractables = { };
        [SerializeField, Tooltip("List of bools which indicates whether or not each corresponding cell in room is present.")] public bool[] cellManifest = { };
        [SerializeField, Tooltip("A list of cells with directions to spawn hatches at. Used for randomized" +
                                 " room hatches. Key should be cell name, value should be hatch direction (Vector2.up, Vector2.down, etc.)")]
        public Dictionary<string, Vector2> hatchPlacements = new(); //Key should be cell name, value should be hatch direction (up, down, left, right)
    }
}
