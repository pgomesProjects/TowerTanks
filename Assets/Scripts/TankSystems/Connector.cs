using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class Connector : MonoBehaviour
    {
        //Objects & Components:
        private Transform intactElements;  //Elements of connector which are enabled while connector is undamaged
        private Transform damagedElements; //Elements of connector which are enabled once the connector is damaged
        internal Room room;                //The room this connector is a part of
        internal GameObject backWall;      //Object containing back wall of connector (will be moved and changed into a sprite mask by room kit)
        
        [Tooltip("The two side walls for this connector.")] public GameObject[] walls;
        [Space()]
        [SerializeField] internal Cell cellA; //Cell on first side of the connector
        [SerializeField] internal Cell cellB; //Cell on second side of the connector

        //Runtime variables:
        [Tooltip("True if connector is vertically oriented (hatch). False if connector is horizontally oriented (door).")] internal bool vertical = false;
        private bool damaged;     //True if one cell attached to connector has been destroyed
        private bool initialized; //Indicates whether or not connector has been set up and is ready to go

        //RUNTIME METHODS:
        private void Awake()
        {
            Initialize(); //Set everything up
        }
            private void OnDestroy()
            {
                Destroy(backWall); //Destroy back wall object (will probably be childed to back wall sprite object in room)
            }

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Ensures that coupler has all the information it needs to work properly.
        /// </summary>
        public void Initialize()
        {
            //Initialization check:
            if (initialized) return; //Do not attempt to re-initialize connector
            initialized = true;      //Indicate that connector has been initialized

            //Get objects & components:
            room = GetComponentInParent<Room>();                 //Get parent room
            intactElements = transform.Find("IntactElements");   //Get intact elements container
            damagedElements = transform.Find("DamagedElements"); //Get damaged elements container
            backWall = transform.Find("BackWall").gameObject;    //Get back wall object
        }
        /// <summary>
        /// Converts connector into its damaged form, done when cell on one side is destroyed.
        /// </summary>
        /// <param name="remainingCell">The cell this connector is still attached to.</param>
        public void Damage(Cell destroyedCell)
        {
            //Validity checks:
            if (destroyedCell != cellA && destroyedCell != cellB) { Debug.LogError("Tried to damage connector with reference to unassociated cell!"); return; } //Post error if given parameter does not match known cell
            if (damaged) { Destroy(gameObject); return; }                                                                                                       //Destroy connector once both of its connected cells have been destroyed

            //Visualize damage:
            damagedElements.gameObject.SetActive(true);                                                             //Enable damaged version of connector
            intactElements.gameObject.SetActive(false);                                                             //Disable original version of connector
            Vector2 facingDirection = (destroyedCell.transform.localPosition - transform.localPosition).normalized; //Get direction connector needs to face relative to its remaining intact cell

            //Data cleanup:
            damaged = true;                                //Indicate that connector has been damaged
            if (destroyedCell == cellA) cellA = null;      //Clear reference to destroyed cell
            else if (destroyedCell == cellB) cellB = null; //Clear reference to destroyed cell
        }
        /// <summary>
        /// If thisCell is attached to this connector, returns other attached cell.
        /// </summary>
        /// <param name="thisCell"></param>
        /// <returns></returns>
        public Cell GetOtherCell(Cell thisCell)
        {
            if (thisCell != cellA && thisCell != cellB) { return null; } //Return nothing if given cell is not attached to this connector
            if (thisCell == cellA) return cellB;                         //Return cell B if cell A is given
            return cellA;                                                //Otherwise return cell A
        }
    }
}
