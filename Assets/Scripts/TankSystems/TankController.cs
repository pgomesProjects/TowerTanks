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
    private float currentCoreHealth;
    private TankManager tankManager;

    [Header("Cargo")]
    public GameObject[] cargoHold;

    //Settings:
    #region Debug Controls
    [Header("Debug Controls")]
    [InlineButton("ShiftRight", SdfIconType.ArrowRight, "")]
    [InlineButton("ShiftLeft", SdfIconType.ArrowLeft, "")]
    public int gear;

    public System.Action<float> OnCoreDamaged;

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
    private List<InteractableId> interactablePool = new List<InteractableId>();

    [PropertySpace]
    [Button(" Sort by Type", ButtonSizes.Small, Icon = SdfIconType.SortUpAlt), Tooltip("Sorts Interactable List by Interactable Type")]
    private void SortListByType()
    {
        List<InteractableId> newList = new List<InteractableId>();
        for (int i = 0; i < 4; i++)
        {
            if (i == 0)
            {
                foreach(InteractableId interactable in interactableList) 
                { 
                    if(interactable.type == TankInteractable.InteractableType.WEAPONS)
                    {
                        newList.Add(interactable);
                    }
                }
            }

            if (i == 1)
            {
                foreach (InteractableId interactable in interactableList)
                {
                    if (interactable.type == TankInteractable.InteractableType.ENGINEERING)
                    {
                        newList.Add(interactable);
                    }
                }
            }

            if (i == 2)
            {
                foreach (InteractableId interactable in interactableList)
                {
                    if (interactable.type == TankInteractable.InteractableType.DEFENSE)
                    {
                        newList.Add(interactable);
                    }
                }
            }

            if (i == 3)
            {
                foreach (InteractableId interactable in interactableList)
                {
                    if (interactable.type == TankInteractable.InteractableType.LOGISTICS)
                    {
                        newList.Add(interactable);
                    }
                }
            }
        }

        interactableList = newList;
    }

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
    private bool isDying = false; //true when the tank is in the process of blowing up
    private float deathSequenceTimer = 0;
    [Tooltip("Describes the size of the tank in each cardinal direction (relative to treadbase). X = height, Y = left width, Z = depth, W = right width.")] internal Vector4 tankSizeValues;

    //UI
    private SpriteRenderer damageSprite;
    [Tooltip("How long damage visual effect persists for")] private float damageTime;
    private float damageTimer;

    public static System.Action OnPlayerTankSizeAdjusted;

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
            if (tankType == TankId.TankType.PLAYER) tankManager.playerTank = this;
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

        currentCoreHealth = coreHealth;

        //Camera setup:
        if (CameraManipulator.main != null) CameraManipulator.main.OnTankSpawned(this); //Generate this tank a camera system

        //Get data from wheelbase:
        if (treadSystem != null && treadSystem.wheels.Length > 0) //Tank should really have a treadSystem by now, and that treadSystem should really have at least one wheel
        {
            tankSizeValues.z = Vector3.Project(treadSystem.wheels[0].basePosition, treadSystem.transform.up).magnitude + treadSystem.wheels[0].radius; //Use vector projection to find the y distance between bottom of wheel and center of tread system
            if (treadSystem.wheels.Length > 1) //Check other wheels if present
            {
                for (int x = 1; x < treadSystem.wheels.Length; x++) //Iterate through all wheels in treadsystem, ignoring the first one because it has already been checked
                {
                    tankSizeValues.z = Mathf.Max(tankSizeValues.z , Vector3.Project(treadSystem.wheels[x].basePosition, treadSystem.transform.up).magnitude + treadSystem.wheels[x].radius); //Compare each wheel to current winner and pick farthest-extending one
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && Application.isEditor) //Only do this gizmo stuff in unity editor playmode
        {
            //Wheel extent visualization:
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(treadSystem.transform.position, treadSystem.transform.position - (treadSystem.transform.up * tankSizeValues.z));
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
        #region Speedlines
        /*
        Cell topCell = coreRoom.cells[0];
        Cell bottomCell = coreRoom.cells[0];

        List<Cell> _topCells = new List<Cell>();
        List<Cell> _bottomCells = new List<Cell>();

        foreach (Room room in rooms)
        {
            //Find TopMost Cell
            foreach (Cell cell in room.cells)
            {
                Vector2 cellPos = this.transform.InverseTransformPoint(topCell.transform.position); //Get current TopMost Cell's position
                if (cell != null) cellPos = this.transform.InverseTransformPoint(cell.transform.position); //Get selected cell's position

                if (cellPos.y > this.transform.InverseTransformPoint(topCell.transform.position).y) //If selected cell's y position is greater than the current TopMost Cell's,
                {
                    _topCells.Clear(); //clear list of Top Cells
                    topCell = cell;  //Make it the new TopMost Cell
                    _topCells.Add(cell); //Add it to the list
                }
                else if (cellPos.y == this.transform.InverseTransformPoint(topCell.transform.position).y) //If selected cell's y position is the same as the TopMost Cell,
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
                        if (this.transform.InverseTransformPoint(_cell.transform.position).x > this.transform.InverseTransformPoint(topCell.transform.position).x)
                        {
                            topCell.ShowSpeedTrails(false, 1);
                            topCell = _cell;
                        }
                    }

                    if (direction == 1)
                    {
                        //Find LeftMost Cell
                        if (this.transform.InverseTransformPoint(_cell.transform.position).x < this.transform.InverseTransformPoint(topCell.transform.position).x)
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
        */
        #endregion

        //Apply Ramming Condition to all Cells

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
                    tankType = tank.tankType;
                    tank.TankName = TankName;
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
            cargoHold[i] = GameManager.Instance.CargoManager.GetRandomCargo();
        }
    }

    public void Damage(float amount)
    {
        currentCoreHealth -= amount;

        //UI
        damageTime += (amount / 50f);
        damageTimer = damageTime;

        if (currentCoreHealth <= 0)
        {
            if (!isDying)
            {
                EventSpawnerManager spawner = GameObject.Find("LevelManager")?.GetComponent<EventSpawnerManager>();
                if (tankType == TankId.TankType.ENEMY) spawner.EnemyDestroyed(this);
                StartCoroutine(DeathSequence(2.5f));
            }
        }

        OnCoreDamaged?.Invoke(currentCoreHealth / coreHealth);
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
        CameraManipulator.main?.OnTankDestroyed(this);
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

            if(tankType == TankId.TankType.ENEMY)
            {
                //If we're checking for enemies destroyed, add 1 to the Objective
                LevelManager.Instance?.AddObjectiveValue(ObjectiveType.DefeatEnemies, 1);

                //Random Interactable Drops
                if (interactablePool.Count > 0)
                {
                    int random = Random.Range(1, Mathf.CeilToInt(interactablePool.Count / 3) + 1); //# of drops
                    for (int i = 0; i < random; i++)
                    {
                        int randomDrop = Random.Range(0, interactablePool.Count); //Randomly Select from Pool
                        TankInteractable interactable = interactablePool[randomDrop].script;

                        INTERACTABLE _interactable = GameManager.Instance.TankInteractableToEnum(interactable); //Convert to Enum
                        StackManager.AddToStack(_interactable); //Add to Stack

                        interactablePool.RemoveAt(randomDrop); //Remove from the Pool
                    }
                }
            }

            //Unassign all characters from this tank
            foreach(Character character in GetCharactersAssignedToTank(this))
                character.SetAssignedTank(null);

            //Detach the characters that are still in the tank and kill them
            foreach (Character character in GetCharactersInTank())
            {
                character.transform.SetParent(null);
                character.KillCharacterImmediate();
            }

            //GameManager.Instance.SystemEffects.ActivateSlowMotion(0.05f, 0.4f, 1.5f, 0.4f);
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
            foreach(RoomInfo roomInfo in GameManager.Instance.roomList) //Find the prefab we want to spawn
            {
                if (roomInfo.roomObject.name == tankDesign.buildingSteps[i].roomID)
                {
                    room = roomInfo.roomObject.gameObject;
                }
            }
            Room roomScript = Instantiate(room.GetComponent<Room>(), towerJoint, false);
            Vector3 spawnVector = tankDesign.buildingSteps[i].localSpawnVector;
            int rotate = tankDesign.buildingSteps[i].rotate;

            //Get room type (accounting for deprecated system):
            int typeNum = (int)tankDesign.buildingSteps[i].roomType; //Get integer version of roomType
            if (typeNum == 4) typeNum = 1;                           //Convert armor rooms in previous system to armor rooms in current system
            if (typeNum > 2) typeNum = 0;                            //Convert all other rooms into standard rooms
            Room.RoomType type = (Room.RoomType)typeNum;             //Apply room type

            //Execute the step
            roomScript.UpdateRoomType(type);
            roomScript.transform.localPosition = spawnVector;
            for (int r = 0; r < rotate + 4; r++)
            {
                roomScript.Rotate(); 
                roomScript.UpdateRoomType(type);
            }
            roomScript.Mount();
            roomScript.ProcessManifest(tankDesign.buildingSteps[i].cellManifest);

            //Install interactables
            foreach (BuildStep.CellInterAssignment cellInter in tankDesign.buildingSteps[i].cellInteractables) //Iterate through each cell interactable assignment
            {
                Cell target = roomScript.transform.GetChild(0).Find(cellInter.cellName).GetComponent<Cell>();                                                                                                //Get target cell from room
                TankInteractable interactable = Instantiate(Resources.Load<RoomData>("RoomData").interactableList.FirstOrDefault(item => item.name == cellInter.interRef)).GetComponent<TankInteractable>(); //Get reference to and spawn in designated interactable
                interactable.InstallInCell(target);                                                                                                                                                          //Install interactable in target cell
                if (cellInter.flipped) interactable.Flip();                                                                                                                                                  //Flip interactable if ordered
            }
        }

        SetTankName(tankDesign.TankName);
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
                design.buildingSteps[roomCount].rotate = roomScript.rotTracker; //How many times the room has been rotated before being placed

                //Get missing cells:
                design.buildingSteps[roomCount].cellManifest = roomScript.cellManifest; //Get cell manifest from room

                //TODO:
                //Is this an enemy or player design?
                //Cell damage values?
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
    /// <summary>
    /// Updates envelope describing height and width of tank relative to treadbase.
    /// </summary>
    public void UpdateSizeValues()
    {
        //Find extreme cells:
        Transform upMostCell = null;    //Create container for store highest cell in tank
        Transform leftMostCell = null;  //Create container for most leftward cell in tank
        Transform rightMostCell = null; //Create container for most rightward cell in tank
        foreach (Room room in rooms) //Iterate through all rooms in tank
        {
            foreach (Cell cell in room.cells) //Iterate through all cells in room
            {
                Vector3 cellPos = treadSystem.transform.InverseTransformPoint(cell.transform.position); //Get shorthand variable for position of cell
                if (upMostCell == null || treadSystem.transform.InverseTransformPoint(upMostCell.transform.position).y < cellPos.y) upMostCell = cell.transform;          //Save cell if it is taller than tallest known cell in tank
                if (leftMostCell == null || treadSystem.transform.InverseTransformPoint(leftMostCell.transform.position).x > cellPos.x) leftMostCell = cell.transform;    //Save cell if it is farther left than leftmost known cell in tank
                if (rightMostCell == null || treadSystem.transform.InverseTransformPoint(rightMostCell.transform.position).x < cellPos.x) rightMostCell = cell.transform; //Save cell if it is farther right than rightmost known cell in tank
            }
        }

        //Calculate tank metrics:
        float highestCellHeight = treadSystem.transform.InverseTransformPoint(upMostCell.transform.position).y + 0.5f;                 //Get height from treadbase to top of highest cell
        float tankLeftSideLength = Mathf.Abs(treadSystem.transform.InverseTransformPoint(leftMostCell.transform.position).x) + 0.5f;   //Get length of tank from center of treadbase to outer edge of leftmost cell
        float tankRightSideLength = Mathf.Abs(treadSystem.transform.InverseTransformPoint(rightMostCell.transform.position).x) + 0.5f; //Get length of tank from center of treadbase to outer edge of rightmost cell
        tankSizeValues = new Vector4(highestCellHeight, tankRightSideLength, tankSizeValues.z, tankLeftSideLength);                    //Store found values

        //If this is the player tank, call the Action
        if (tankType == TankId.TankType.PLAYER)
            OnPlayerTankSizeAdjusted?.Invoke();
    }

    public void AddInteractable(GameObject interactable)
    {
        InteractableId newId = new InteractableId();
        newId.interactable = interactable;
        newId.script = interactable.GetComponent<TankInteractable>();
        newId.type = newId.script.interactableType;
        newId.stackName = newId.script.stackName;
        interactableList.Add(newId);
        if (tankType == TankId.TankType.ENEMY) interactablePool.Add(newId);
    }

    public static List<Character> GetCharactersAssignedToTank(TankController tank)
    {
        List<Character> characters = new List<Character>();

        foreach (Character character in FindObjectsOfType<Character>())
        {
            if (character.GetAssignedTank() == tank)
                characters.Add(character);
        }

        return characters;
    }

    public void SetTankName(string newTankName)
    {
        TankName = newTankName;
        nameText.text = TankName;
        gameObject.name = "Tank (" + TankName + ")";
    }
    public Character[] GetCharactersInTank() => GetComponentsInChildren<Character>();
    public float GetHighestPoint() => tankSizeValues.x;
}
