using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public class TankController : MonoBehaviour
{
    //Important Variables
    public string TankName;
    [SerializeField] public TankId.TankType tankType;
    [SerializeField] public float coreHealth = 500;

    //Objects & Components:
    [Tooltip("Rooms currently installed on tank.")]                                             internal List<Room> rooms;
    [Tooltip("Core room of tank (there can only be one.")]                                      internal Room coreRoom;
    [Tooltip("This tank's traction system.")]                                                   internal TreadSystem treadSystem;
    [Tooltip("Transform containing all tank rooms, point around which tower tilts.")]           private Transform towerJoint;
    [SerializeField, Tooltip("Target transform in tread system which tower joint locks onto.")] private Transform towerJointTarget;

    private TextMeshProUGUI nameText;

    private TankManager tankManager;

    [Header("Cargo")]
    public GameObject[] cargoHold;

    //Runtime Variables:
    [Header("Debug")] 
    public bool shiftRight;
    public bool shiftLeft;
    public bool damage;
    public bool addEngine;
    public bool fireAllCannons;

    public bool isDying = false; //true when the tank is in the process of blowing up
    private float deathSequenceTimer = 0;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        treadSystem = GetComponentInChildren<TreadSystem>(); //Get tread system from children
        towerJoint = transform.Find("TowerJoint");           //Get tower joint from children
        treadSystem.Initialize();                            //Make sure treads are initialized

        nameText = GetComponentInChildren<TextMeshProUGUI>();

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
        tankManager = GameObject.Find("TankManager")?.GetComponent<TankManager>();

        if (tankManager != null)
        {
            foreach (TankId tank in tankManager.tanks)
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

        //Identify what tank I am
        GetTankInfo();

        //Enemy Logic
        if (tankType == TankId.TankType.ENEMY)
        {
            coreHealth *= 0.5f;
            EnableCannonBrains(false);
            AddCargo();
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
        if (damage) { damage = false; Damage(100); }
        if (addEngine) { addEngine = false; treadSystem.currentEngines += 1; }
        if (fireAllCannons) { fireAllCannons = false; FireAllCannons(); }

        //Update name
        nameText.text = TankName;

        //Death Sequence Events
        if (isDying)
        {
            DeathSequenceEvents();
        }
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

    public void FireAllCannons()
    {
        GunController[] cannons = GetComponentsInChildren<GunController>();
        foreach(GunController cannon in cannons)
        {
            cannon.Fire();
        }
    }

    public void GetTankInfo()
    {
        tankManager = GameObject.Find("TankManager")?.GetComponent<TankManager>();

        if (tankManager != null)
        {
            foreach (TankId tank in tankManager.tanks)
            {
                if (tank.gameObject == gameObject) //if I'm on the list,
                {
                    TankName = tank.TankName; //give me my codename
                    gameObject.name = "Tank (" + TankName + ")";
                    tankType = tank.tankType;
                }
            }
        }
    }

    public void AddCargo()
    {
        int random = Random.Range(2, 8);
        cargoHold = new GameObject[random];
        for (int i = 0; i < cargoHold.Length; i++)
        {
            int _random = Random.Range(0, GameManager.Instance.cargoList.Length);
            cargoHold[i] = GameManager.Instance.cargoList[_random];
        }
    }

    public void Damage(float amount)
    {
        coreHealth -= amount;
        if (coreHealth <= 0)
        {
            if (!isDying)
            {
                EventSpawnerManager spawner = GameObject.Find("LevelManager")?.GetComponent<EventSpawnerManager>();
                if (tankType == TankId.TankType.ENEMY) spawner.EnemyDestroyed(this);
                StartCoroutine(DeathSequence(2.5f));
            }
        }
    }

    public void DeathSequenceEvents()
    {
        deathSequenceTimer -= Time.deltaTime;
        if (deathSequenceTimer <= 0)
        {
            int randomParticle = Random.Range(0, 3);
            float randomX = Random.Range(-4f, 4f);
            float randomY = Random.Range(0, 2f);
            float randomS = Random.Range(0.1f, 0.2f);

            Vector2 randomPos = new Vector2(treadSystem.transform.position.x + randomX, treadSystem.transform.position.y + randomY);

            GameManager.Instance.ParticleSpawner.SpawnParticle(randomParticle, randomPos, randomS, treadSystem.transform);

            GameManager.Instance.AudioManager.Play("ExplosionSFX", treadSystem.gameObject);
            GameManager.Instance.AudioManager.Play("LargeExplosionSFX", treadSystem.gameObject);

            deathSequenceTimer = Random.Range(0.1f, 0.2f);
        }
    }

    public IEnumerator DeathSequence(float duration)
    {
        isDying = true;
        yield return new WaitForSeconds(duration);
        BlowUp(false);
    }

    public void BlowUp(bool immediate)
    {
        if (immediate) DestroyImmediate(gameObject);
        else
        {
            Cell[] cells = GetComponentsInChildren<Cell>();
            foreach(Cell cell in cells)
            {
                //Destroy all cells
                cell.Kill();

                //Blow up the core
                if (cell.room.isCore)
                {
                    GameManager.Instance.ParticleSpawner.SpawnParticle(5, cell.transform.position, 0.15f, null);
                }
            }

            //Spawn Cargo
            foreach(GameObject _cargo in cargoHold)
            {
                GameObject flyingCargo = Instantiate(_cargo, treadSystem.transform.position, treadSystem.transform.rotation, null);
                float randomX = Random.Range(-10f, 10f);
                float randomY = Random.Range(5f, 20f);
                float randomT = Random.Range(-16f, 16f);
                Vector2 _random = new Vector2(randomX, randomY);

                Rigidbody2D rb = flyingCargo.GetComponent<Rigidbody2D>();
                rb.AddForce(_random * 40);
                rb.AddTorque(randomT * 10);
            }
            Destroy(gameObject);
        }
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
            roomScript.randomizeType = false;
            Room.RoomType type = tankDesign.buildingSteps[i].roomType;
            Vector3 spawnVector = tankDesign.buildingSteps[i].localSpawnVector;
            int rotate = tankDesign.buildingSteps[i].rotate;

            //Add any needed interactables
            Cell cell;
            Transform cells = roomScript.transform.Find("Cells");
            foreach (Transform child in cells)
            {
                cell = child.GetComponent<Cell>();
                if (cell != null)
                {
                    if (cell.gameObject.name == tankDesign.buildingSteps[i].cellWithSlot) //Find matching cell
                    {
                        foreach(GameObject interactable in GameManager.Instance.interactableList)
                        {
                            if (interactable.name == tankDesign.buildingSteps[i].interactable) //Find matching interactable
                            {
                                //NEW INTERACTABLE INSTALLATION LOGIC NEEDS TO BE ADDED HERE
                                //cell.startingInteractable = interactable;
                            }
                        }
                    }
                }
            }

            //Execute the step
            roomScript.UpdateRoomType(type);
            roomScript.transform.localPosition = spawnVector;
            if (tankDesign.buildingSteps[i].direction != 1) roomScript.flipOnStart = true;
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

                //design.buildingSteps[roomCount].cellWithSlot = roomScript.GetCellWithInteractable().gameObject.name; //Name of the cell in this room with an interactable slot
                //string interID = roomScript.GetCellWithInteractable().installedInteractable.gameObject.name.Replace("(Clone)", "");
                //design.buildingSteps[roomCount].interactable = interID; //Name of the interactable in the cell
                //design.buildingSteps[roomCount].direction = roomScript.GetCellWithInteractable().installedInteractable.direction; //direction the interactable is facing
                
                //TODO:
                //Add in cell-by-cell interactable locating ad saving
                //Is this an enemy or player design?
                //Cell damage values?
                //Which cells are intact?
                //Where cargo is located in the tank?
                roomCount++;
            }
        }

        design.TankName = TankName; //Name the design after the current tank
        return design;
    }

    public void EnableCannonBrains(bool enabled)
    {
        SimpleCannonBrain[] brains = GetComponentsInChildren<SimpleCannonBrain>();
        foreach(SimpleCannonBrain brain in brains)
        {
            brain.enabled = enabled;
        }

        SimpleTankBrain _brain = GetComponent<SimpleTankBrain>();
        _brain.enabled = true;
    }
}
