using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TankController : MonoBehaviour
{
    public string TankName;

    //Objects & Components:
    [Tooltip("Rooms currently installed on tank.")]                                             internal List<Room> rooms;
    [Tooltip("Core room of tank (there can only be one.")]                                      internal Room coreRoom;
    [Tooltip("This tank's traction system.")]                                                   internal TreadSystem treadSystem;
    [Tooltip("Transform containing all tank rooms, point around which tower tilts.")]           private Transform towerJoint;
    [SerializeField, Tooltip("Target transform in tread system which tower joint locks onto.")] private Transform towerJointTarget;

    private TextMeshProUGUI nameText;

    //Runtime Variables:


    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        treadSystem = GetComponentInChildren<TreadSystem>(); //Get tread system from children
        towerJoint = transform.Find("TowerJoint");           //Get tower joint from children
        treadSystem.Initialize();                            //Make sure treads are initialized

        nameText = GetComponentInChildren<TextMeshProUGUI>();

        //Identify what tank I am
        GetTankInfo();

        //Room setup:
        rooms = new List<Room>(GetComponentsInChildren<Room>()); //Get list of all rooms which spawn as children of tank (for prefab tanks)
        foreach (Room room in rooms) //Scrub through childed room list (should be in order of appearance under towerjoint)
        {
            room.targetTank = this; //Make this the target tank for all childed rooms
            if (room.isCore) //Found a core room
            {
                //Core room setup:
                if (coreRoom != null) Debug.LogError("Found two core rooms in tank " + gameObject.name); //Indicate problem if multiple core rooms are found
                coreRoom = room;                                                                         //Get core room
            }
            else //Room has been added to tank for pre-mounting
            {
                //Pre-mount room:
                room.UpdateRoomType(room.type);              //Apply preset room type
                room.SnapMove(room.transform.localPosition); //Snap room to position on tank grid
                room.Mount();                                //Mount room to tank immediately
            }
        }
        treadSystem.ReCalculateMass(); //Get center of mass based on room setup
    }
    private void Update()
    {
        //Tread system updates:
        towerJoint.position = towerJointTarget.position; //Move tower joint to target position
        towerJoint.rotation = towerJointTarget.rotation; //Move tower joint to target rotation

        //Update name
        nameText.text = TankName;
    }

    public void ChangeAllGear(int direction) //changes gear of all active throttles in the tank
    {
        ThrottleController[] throttles = GetComponentsInChildren<ThrottleController>();
        if (throttles.Length > 0)
        {
            for (int i = 0; i < throttles.Length; i++)
            {
                throttles[i].ChangeGear(direction);
            }
        }
    }

    public void GetTankInfo()
    {
        var tankMan = GameObject.Find("TankManager").GetComponent<TankManager>();

        if (tankMan != null)
        {
            foreach (TankId tank in tankMan.tanks)
            {
                if (tank.gameObject == gameObject) //if I'm on the list,
                {
                    TankName = tank.TankName; //give me my codename
                }
            }
        }
    }
}
