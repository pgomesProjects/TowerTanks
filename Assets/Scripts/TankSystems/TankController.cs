using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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

    //Important Variables
    [SerializeField] public float coreHealth = 500;

    private TextMeshProUGUI nameText;

    //Runtime Variables:
    [Header("Debug")] 
    public bool shiftRight;
    public bool shiftLeft;

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
    private void Start()
    {
        var tankMan = GameObject.Find("TankManager").GetComponent<TankManager>();

        if (tankMan != null)
        {
            foreach (TankId tank in tankMan.tanks)
            {
                if (tank.gameObject == gameObject) //if I'm on the list,
                {
                    if (tank.buildOnStart && tank.design != null)
                    {
                        string json = tank.design.text;
                        if (json != null)
                        {
                            TankDesign _design = JsonUtility.FromJson<TankDesign>(json);
                            //Debug.Log("" + layout.chunks[0] + ", " + layout.chunks[1] + "...");
                            Build(_design);
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        //Tread system updates:
        towerJoint.position = towerJointTarget.position; //Move tower joint to target position
        towerJoint.rotation = towerJointTarget.rotation; //Move tower joint to target rotation

        //Debug 
        if (shiftLeft) { shiftLeft = false; ChangeAllGear(-1); }
        if (shiftRight) { shiftRight = false; ChangeAllGear(1); }

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
                    gameObject.name = "Tank (" + TankName + ")";
                }
            }
        }
    }

    public void Damage(float amount)
    {
        coreHealth -= amount;
        if (coreHealth <= 0)
        {
            BlowUp(false);
        }
    }

    public void BlowUp(bool immediate)
    {
        if (immediate) DestroyImmediate(gameObject);
        else Destroy(gameObject);
    }

    public void Build(TankDesign tankDesign) //Called from TankManager when constructing a specific design
    {
        for (int i = 0; i < tankDesign.buildingSteps.Length; i++) //Loop through all the steps in the design
        {
            //Get variables of the step
            GameObject room = null;
            foreach(GameObject prefab in GameManager.Instance.roomList) //Find the prefab we want to spawn
            {
                if (prefab.name == tankDesign.buildingSteps[i].roomID)
                {
                    room = prefab;
                }
            }
            Room roomScript = Instantiate(room.GetComponent<Room>(), towerJoint, false);
            Room.RoomType type = tankDesign.buildingSteps[i].roomType;
            Vector3 spawnVector = tankDesign.buildingSteps[i].localSpawnVector;
            int rotate = tankDesign.buildingSteps[i].rotate;

            //Execute the step
            roomScript.UpdateRoomType(type);
            roomScript.transform.position += spawnVector;
            for (int r = 0; r < rotate + 4; r++)
            {
                roomScript.Rotate(); 
                roomScript.UpdateRoomType(type);
            }
            roomScript.Mount();
        }
    }

    public TankDesign GetCurrentDesign()
    {
        TankDesign design = new TankDesign();

        int roomCount = 0;
        //Find out how many steps are needed for this design
        foreach(Transform room in towerJoint)
        {
            Room roomScript = room.GetComponent<Room>();
            if (roomScript != null && roomScript.isCore == false)
            {
                roomCount++;
            }
        }

        if (roomCount > 0) design.buildingSteps = new BuildStep[roomCount]; //Set up the instructions
        else
        {
            Debug.LogError("You're trying to create a blank design. Place some rooms first.");
            return null;
        }

        for (int i = 0; i < design.buildingSteps.Length; i++)
        {
            design.buildingSteps[i] = new BuildStep(); //Initialize steps
        }

        roomCount = 0;
        //Fill out instructions with details
        foreach(Transform room in towerJoint)
        {
            Room roomScript = room.GetComponent<Room>();
            if (roomScript != null && roomScript.isCore == false)
            {
                string roomID = room.name.Replace("(Clone)", "");
                design.buildingSteps[roomCount].roomID = roomID; //Name of the room's prefab
                design.buildingSteps[roomCount].roomType = roomScript.type; //The room's current type
                design.buildingSteps[roomCount].localSpawnVector = room.transform.localPosition; //The room's local position relative to the tank
                design.buildingSteps[roomCount].rotate = roomScript.debugRotation; //How many times the room has been rotated before being placed
                //TODO:
                //Where the interactable slot is located?
                //Cell damage values?
                //Which cells are in tact?
                roomCount++;
            }
        }

        design.TankName = TankName; //Name the design after the current tank
        return design;
    }
}
