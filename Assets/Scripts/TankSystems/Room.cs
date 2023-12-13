using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private Room parentRoom;                              //The room this room was mounted to
    private List<Coupler> couplers = new List<Coupler>(); //Couplers connecting this room to other rooms
    private Cell[] cells;                                 //Individual square sections which make up the room

    [Header("Prefabs:")]
    [SerializeField, Tooltip("Reference to coupler object spawned when mounting this room.")] private GameObject couplerPrefab;

    //Settings:
    [Header("Template Settings:")]
    [SerializeField, Tooltip("Default integrity of this room template.")] private float baseIntegrity = 100;

    //Runtime Variables:
    public RoomType type;         //Which broad purpose this room serves
    private float integrity;      //Health of the room. When reduced to zero, room becomes inoperable
    private bool mounted = false; //Whether or not this room has been attached to another room yet

    //RUNTIME METHODS:
    private void Awake()
    {
        //Setup runtime variables:
        CalculateIntegrity();                    //Set base integrity (will be modified by other scripts)
        cells = GetComponentsInChildren<Cell>(); //Get references to cells in room

        //Setup cells:
        foreach (Cell cell in cells) //Iterate through cells
        {
            Vector2 cellAPos = cell.transform.position; //Get position of current cell

        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Attaches this room to another room or the tank base.
    /// </summary>
    public void Mount()
    {

    }

    //UTILITY METHODS:
    /// <summary>
    /// Sets integrity to base level with room type modifiers applied.
    /// </summary>
    public void CalculateIntegrity()
    {
        integrity = baseIntegrity; //TEMP: Use flat base integrity
    }
}
