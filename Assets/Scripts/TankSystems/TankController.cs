using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankController : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("Rooms currently installed on tank.")]        internal List<Room> rooms;
    [Tooltip("Core room of tank (there can only be one.")] private Room coreRoom;

    //Settings:


    //Runtime Variables:
    

    //RUNTIME METHODS:
    private void Awake()
    {
        //Room setup:
        rooms = new List<Room>(GetComponentsInChildren<Room>()); //Get list of all rooms which spawn as children of tank (for prefab tanks)
        foreach (Room room in rooms) //Scrub through childed room list
        {
            room.targetTank = this; //Make this the target tank for all childed rooms
            if (room.isCore) //Found a core room
            {
                //Core room setup:
                if (coreRoom != null) Debug.LogError("Found two core rooms in tank " + gameObject.name); //Indicate problem if multiple core rooms are found
                coreRoom = room;                                                                         //Get core room
            }
        } 
    }
}
