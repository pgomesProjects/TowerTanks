using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;
using Sirenix.OdinInspector;

[System.Serializable]
public class TankController : SerializedMonoBehaviour
{
    //Important Variables
    public string TankName;
    [SerializeField] public TankId.TankType tankType;
    
    [InlineButton("KillTank", SdfIconType.EmojiDizzy, "Kill")]
    [InlineButton("DamageTank", SdfIconType.Magic, "-100")]
    [SerializeField] public float coreHealth = 500;

    //Objects & Components:
    [Tooltip("Rooms currently installed on tank.")]                                             internal List<Room> rooms;
    [Tooltip("Core room of tank (there can only be one.")]                                      internal Room coreRoom;
    [Tooltip("This tank's traction system.")]                                                   internal TreadSystem treadSystem;
    [Tooltip("Transform containing all tank rooms, point around which tower tilts.")]           private Transform towerJoint;
    [SerializeField, Tooltip("Target transform in tread system which tower joint locks onto.")] private Transform towerJointTarget;
    public bool isInvincible;

    private TextMeshProUGUI nameText;

    private TankManager tankManager;

    [Header("Cargo")]
    public GameObject[] cargoHold;

    //Settings:
    #region Debug Controls
    [Header("Debug Controls")]
    [InlineButton("ShiftRight", SdfIconType.ArrowRight, "")]
    [InlineButton("ShiftLeft", SdfIconType.ArrowLeft, "")]
    public int gear;

    public void ShiftRight()
    {
        ChangeAllGear(1);
    }
    public void ShiftLeft()
    {
        ChangeAllGear(-1);
    }

    private float damage = 100f;
    private void DamageTank()
    {
        Damage(damage);
    }

    public void KillTank()
    {
        Damage(coreHealth);
    }

    [InlineButton("LoseEngine", SdfIconType.Dash, "")]
    [InlineButton("AddEngine", SdfIconType.Plus, "")]
    public float horsePower;

    private void AddEngine()
    {
        treadSystem.currentEngines += 1;
    }
    private void LoseEngine()
    {
        treadSystem.currentEngines -= 1;
    }

    [PropertySpace]
    [Header("Interactables")]
    [SerializeField] public List<InteractableId> interactableList = new List<InteractableId>();

    [PropertySpace]
    [Button(" Fire All Weapons", ButtonSizes.Small, Icon = SdfIconType.SquareFill), Tooltip("Fires every weapon in the tank, ignoring conditions")]
    private void FireAllCannons()
    {
        GunController[] cannons = GetComponentsInChildren<GunController>();
        foreach (GunController cannon in cannons) { cannon.Fire(true, tankType); }
    }
    [Button(" Double Weapon ROF", ButtonSizes.Small, Icon = SdfIconType.Speedometer2), Tooltip("Doubles the Rate of Fire for every weapon in the tank")]
    public void OverchargeAllWeapons()
    {
        GunController[] weapons = GetComponentsInChildren<GunController>();
        foreach (GunController weapon in weapons)
        {
            weapon.ChangeRateOfFire(0.5f);
        }
    }
    [PropertySpace]
    #endregion

    //Runtime Variables:
    [Tooltip("One of the cells which is in the uppermost position in the tank.")] internal Cell highestCell;
    private bool isDying = false; //true when the tank is in the process of blowing up
    private float deathSequenceTimer = 0;

    //UI
    private SpriteRenderer damageSprite;
    [Tooltip("How long damage visual effect persists for")] private float damageTime;
    private float damageTimer;

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        treadSystem = GetComponentInChildren<TreadSystem>(); //Get tread system from children
        towerJoint = transform.Find("TowerJoint");           //Get tower joint from children
        treadSystem.Initialize();                            //Make sure treads are initialized

        nameText = GetComponentInChildren<TextMeshProUGUI>();
        damageSprite = towerJoint.transform.Find("DiageticUI")?.GetComponent<SpriteRenderer>();

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
        gear = treadSystem.gear;
        horsePower = treadSystem.currentEngines * 100;

        //Update name
        nameText.text = TankName;

        //Death Sequence Events
        if (isDying)
        {
            DeathSequenceEvents();
        }

        //UI
        if (damageTimer > 0)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        Color newColor = damageSprite.color;
        newColor.a = Mathf.Lerp(0, 255f, (damageTimer / damageTime) * Time.deltaTime);
        damageSprite.color = newColor;

        damageTimer -= Time.deltaTime;
        if (damageTimer < 0)
        {
            damageTimer = 0;
            damageTime = 0;
        }
    }

    public void RammingSpeed(float direction) //direction --> 1 = right, -1 = left
    {
        Cell topCell = coreRoom.cells[0];
        Cell bottomCell = coreRoom.cells[0];

        List<Cell> _topCells = new List<Cell>();
        List<Cell> _bottomCells = new List<Cell>();

        foreach (Room room in rooms)
        {
            //Find TopMost Cell
            foreach (Cell cell in room.cells)
            {
                Vector2 cellPos = towerJoint.transform.InverseTransformPoint(topCell.transform.position); //Get current TopMost Cell's position
                if (cell != null) cellPos = towerJoint.transform.InverseTransformPoint(cell.transform.position); //Get selected cell's position

                if (cellPos.y > towerJoint.transform.InverseTransformPoint(topCell.transform.position).y) //If selected cell's y position is greater than the current TopMost Cell's,
                {
                    _topCells.Clear(); //clear list of Top Cells
                    topCell = cell;  //Make it the new TopMost Cell
                    _topCells.Add(cell); //Add it to the list
                }
                else if (cellPos.y == towerJoint.transform.InverseTransformPoint(topCell.transform.position).y) //If selected cell's y position is the same as the TopMost Cell,
                {
                    _topCells.Add(cell); //Add it to the list
                }

                //Turn Off Speedlines
                cell.ShowSpeedTrails(false, 0);
            }

            if (_topCells.Count > 1) //If there's more than 1 Cell tied for highest y position
            {
                foreach (Cell _cell in _topCells) //Find the Left / Right Most Cell 
                {
                    if (direction == -1)
                    {
                        //Find RightMost Cell
                        if (towerJoint.transform.InverseTransformPoint(_cell.transform.position).x > towerJoint.transform.InverseTransformPoint(topCell.transform.position).x)
                        {
                            topCell.ShowSpeedTrails(false, 1);
                            topCell = _cell;
                        }
                    }

                    if (direction == 1)
                    {
                        //Find LeftMost Cell
                        if (towerJoint.transform.InverseTransformPoint(_cell.transform.position).x < towerJoint.transform.InverseTransformPoint(topCell.transform.position).x)
                        {
                            topCell.ShowSpeedTrails(false, 1);
                            topCell = _cell;
                        }
                    }
                }
            }

            //Find BottomMost Cell
            foreach (Cell cell in room.cells)
            {
                Vector2 cellPos = towerJoint.transform.InverseTransformPoint(bottomCell.transform.position); //Get current BottomMost Cell's position
                if (cell != null) cellPos = towerJoint.transform.InverseTransformPoint(cell.transform.position); //Get selected cell's position

                if (cellPos.y < towerJoint.transform.InverseTransformPoint(bottomCell.transform.position).y) //If selected cell's y position is less than the current BottomMost Cell's,
                {
                    _bottomCells.Clear(); //clear list of Bottom Cells
                    bottomCell = cell;  //Make it the new BottomMost Cell
                    _bottomCells.Add(cell); //Add it to the list
                }
                else if (cellPos.y == towerJoint.transform.InverseTransformPoint(bottomCell.transform.position).y) //If selected cell's y position is the same as the BottomMost Cell,
                {
                    _bottomCells.Add(cell); //Add it to the list
                }

                //Turn Off Speedlines
                cell.ShowSpeedTrails(false, -1);
            }

            if (_bottomCells.Count > 1) //If there's more than 1 Cell tied for lowest y position
            {
                foreach (Cell _cell in _bottomCells) //Find the Left / Right Most Cell 
                {
                    if (direction == -1)
                    {
                        //Find RightMost Cell
                        if (towerJoint.transform.InverseTransformPoint(_cell.transform.position).x > towerJoint.transform.InverseTransformPoint(bottomCell.transform.position).x)
                        {
                            bottomCell.ShowSpeedTrails(false, -1);
                            bottomCell = _cell;
                        }
                    }

                    if (direction == 1)
                    {
                        //Find LeftMost Cell
                        if (towerJoint.transform.InverseTransformPoint(_cell.transform.position).x < towerJoint.transform.InverseTransformPoint(bottomCell.transform.position).x)
                        {
                            bottomCell.ShowSpeedTrails(false, -1);
                            bottomCell = _cell;
                        }
                    }
                }
            }
        }

        _topCells.Clear();
        _bottomCells.Clear();

        //Apply Speedlines to both Cells
        topCell.ShowSpeedTrails(true, 1);
        bottomCell.ShowSpeedTrails(true, -1);

        //Apply Ramming Condition to all Cells

    }

    public void DisableSpeedTrails()
    {
        foreach (Room room in rooms)
        {
            foreach (Cell cell in room.cells)
            {
                cell.ShowSpeedTrails(false, 0);
            }
        }
    }

    public void ChangeAllGear(int direction) //changes gear of all active throttles in the tank
    {
        ThrottleController[] throttles = GetComponentsInChildren<ThrottleController>();
        if (throttles.Length > 0)
        {
            treadSystem.gear = throttles[0].gear; //Tell tread system what gear it is in
            for (int i = 0; i < throttles.Length; i++)
            {
                throttles[i].ChangeGear(direction);
            }
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

        //UI
        damageTime += (amount / 50f);
        damageTimer = damageTime;

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

            //Update Objectives & Analytics
            LevelManager levelman = GameObject.Find("LevelManager")?.GetComponent<LevelManager>();

            if (levelman != null)
            {
                //If we're checking for enemies destroyed, add 1 to the Objective
                levelman.AddObjectiveValue(ObjectiveType.DefeatEnemies, 1); 

                //TODO: Add 1 to Global Check for enemies destroyed (Analytics)
            }

            Destroy(gameObject);
        }
    }

    public void Build(TankDesign tankDesign) //Called from TankManager when constructing a specific design
    {
        //Build core interactables:
        foreach (BuildStep.CellInterAssignment cellInter in tankDesign.coreInteractables) //Iterate through each interactable assignment for core cells
        {
            Cell target = coreRoom.transform.GetChild(0).Find(cellInter.cellName).GetComponent<Cell>();                                                                                                  //Get target cell from core
            TankInteractable interactable = Instantiate(Resources.Load<RoomData>("RoomData").interactableList.FirstOrDefault(item => item.name == cellInter.interRef)).GetComponent<TankInteractable>(); //Get reference to and spawn in designated interactable
            interactable.InstallInCell(target);                                                                                                                                                          //Install interactable in target cell
            if (cellInter.flipped) interactable.Flip();                                                                                                                                                  //Flip interactable if ordered
        }

        //Build rooms:
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

            //Execute the step
            roomScript.UpdateRoomType(type);
            roomScript.transform.localPosition = spawnVector;
            for (int r = 0; r < rotate + 4; r++)
            {
                roomScript.Rotate(); 
                roomScript.UpdateRoomType(type);
            }
            roomScript.Mount();

            //Install interactables
            foreach (BuildStep.CellInterAssignment cellInter in tankDesign.buildingSteps[i].cellInteractables) //Iterate through each cell interactable assignment
            {
                Cell target = roomScript.transform.GetChild(0).Find(cellInter.cellName).GetComponent<Cell>();                                                                                                //Get target cell from room
                TankInteractable interactable = Instantiate(Resources.Load<RoomData>("RoomData").interactableList.FirstOrDefault(item => item.name == cellInter.interRef)).GetComponent<TankInteractable>(); //Get reference to and spawn in designated interactable
                interactable.InstallInCell(target);                                                                                                                                                          //Install interactable in target cell
                if (cellInter.flipped) interactable.Flip();                                                                                                                                                  //Flip interactable if ordered
            }
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
            if (roomScript != null)
            {
                //Get interactables:
                List<BuildStep.CellInterAssignment> cellInters = new List<BuildStep.CellInterAssignment>(); //Create list to store cell interactable assignments
                foreach(TankInteractable interactable in roomScript.GetComponentsInChildren<TankInteractable>()) //Iterate through interactables in room
                {
                    string cellName = interactable.parentCell.name;              //Get reference name for cell with interactable
                    string interName = interactable.name.Replace("(Clone)", ""); //Get reference string for installed interactable
                    bool flipStatus = interactable.direction == -1;              //Determine whether or not interactable is flipped
                    cellInters.Add(new BuildStep.CellInterAssignment(cellName, interName, flipStatus)); //Add an interactable designator with reference to cell and interactable name
                }
                if (roomScript.isCore) //Store interactables for core room but do not try to store spawn information
                {
                    design.coreInteractables = cellInters.ToArray(); //Save core list of interactables to special list for spawning stuff in the core
                    continue;                                        //Skip the rest of the building steps (core is not built like the rest of the tank)
                }
                design.buildingSteps[roomCount].cellInteractables = cellInters.ToArray(); //Save interactables to design

                //Get room info:
                string roomID = room.name.Replace("(Clone)", "");
                design.buildingSteps[roomCount].roomID = roomID; //Name of the room's prefab
                design.buildingSteps[roomCount].roomType = roomScript.type; //The room's current type
                design.buildingSteps[roomCount].localSpawnVector = room.transform.localPosition; //The room's local position relative to the tank
                design.buildingSteps[roomCount].rotate = roomScript.debugRotation; //How many times the room has been rotated before being placed
                
                //TODO:
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

    public void AddInteractable(GameObject interactable)
    {
        InteractableId newId = new InteractableId();
        newId.interactable = interactable;
        newId.script = interactable.GetComponent<TankInteractable>();
        newId.type = newId.script.interactableType;
        interactableList.Add(newId);
    }
}
