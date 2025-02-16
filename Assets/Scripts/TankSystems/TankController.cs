using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using TMPro;
using System.Linq;
using Sirenix.OdinInspector;
using TowerTanks.Scripts.DebugTools;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class TankController : SerializedMonoBehaviour
    {
        //Static Stuff:
        /// <summary>
        /// List of all tanks currently alive in scene.
        /// </summary>
        public static List<TankController> activeTanks = new List<TankController>();

        //Important Variables
        public string TankName;
        [SerializeField] public TankId.TankType tankType;

        [InlineButton("KillTank", SdfIconType.EmojiDizzy, "Kill")]
        [InlineButton("DamageTank", SdfIconType.Magic, "-100")]
        [SerializeField] public float coreHealth = 500;
        public float currentCoreHealth;

        //Objects & Components:
        [Tooltip("Rooms currently installed on tank.")] internal List<Room> rooms;
        [Tooltip("Core room of tank (there can only be one.")] internal Room coreRoom;
        [Tooltip("This tank's traction system.")] internal TreadSystem treadSystem;
        [SerializeField, Tooltip("The spawn point for the players.")] private Transform playerSpawnPoint;
        [Tooltip("Transform containing all tank rooms, point around which tower tilts.")] private Transform towerJoint;
        [SerializeField, Tooltip("Target transform in tread system which tower joint locks onto.")] private Transform towerJointTarget;
        [SerializeField, Tooltip("When true, the tank cannot take damage.")] public bool isInvincible;
        [SerializeField, Tooltip("When true, the tank transfers all damage it takes to its core.")] public bool isFragile;
        [SerializeField, Tooltip("Current corpse object assigned to this tank.")] public GameObject corpseInstance;
        
        public Cell upMostCell = null;    
        public Cell leftMostCell = null;  
        public Cell rightMostCell = null; 
        private TextMeshProUGUI nameText;
        
        private TankManager tankManager;
        [HideInInspector] public TankId myTankID;
        private TankAI _thisTankAI;
        public GameObject tankFlag;
        public GameObject surrenderFlag;
        private Animator tankAnimator;
        public GameObject corpsePrefab;
        public List<Coupler> hatches = new List<Coupler>();
        private Vector2[] cargoNodes;

        //Settings:
        [Header("Visual Settings:")]
        [Tooltip("Defines how cells in the tank look.")] public RoomAssetKit roomKit;
        public Transform[] diageticElements;
        #region Debug Controls
        [Header("Debug Controls")]
        [InlineButton("ShiftRight", SdfIconType.ArrowRight, "")]
        [InlineButton("ShiftLeft", SdfIconType.ArrowLeft, "")]
        public int gear;
        public System.Action<float> OnCoreDamaged;
        private bool alarmTriggered;
        public bool overrideWeaponROF;
        public bool overrideWeaponRecoil;

        public void ShiftRight()
        {
            ChangeAllGear(1);
        }
        public void ShiftLeft()
        {
            ChangeAllGear(-1);
        }
        
        /// <summary>
        /// Sets the tank's gear to the specified value.
        /// </summary>
        /// <param name="newGear">
        /// int to set the tank's gear to. Must be between -throttle.speedSettings and throttle.speedSettings.
        /// </param>
        public async void SetTankGearOverTime(int newGear, float secondsBetweenThrottleShifts = 0)
        {
            if (Mathf.Abs(newGear) > 2)
            {
                Debug.LogError("SETTANKGEAR: New gear input parameter is over max threshold."); 
                return;
            }
            
            while (treadSystem.gear != newGear)
            {
                if (treadSystem.gear < newGear)
                {
                    ShiftRight();
                }
                else 
                {
                    ShiftLeft();
                }
                await Task.Yield();
                int milli = (int) (secondsBetweenThrottleShifts * 1000);
                
                await Task.Delay(milli);
            }
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
            

        }
        private void LoseEngine()
        {

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
            for (int i = 0; i < 6; i++)
            {
                if (i == 0)
                {
                    foreach (InteractableId interactable in interactableList)
                    {
                        if (interactable.groupType == TankInteractable.InteractableType.WEAPONS)
                        {
                            newList.Add(interactable);
                        }
                    }
                }

                if (i == 1)
                {
                    foreach (InteractableId interactable in interactableList)
                    {
                        if (interactable.groupType == TankInteractable.InteractableType.ENGINEERING)
                        {
                            newList.Add(interactable);
                        }
                    }
                }

                if (i == 2)
                {
                    foreach (InteractableId interactable in interactableList)
                    {
                        if (interactable.groupType == TankInteractable.InteractableType.DEFENSE)
                        {
                            newList.Add(interactable);
                        }
                    }
                }

                if (i == 3)
                {
                    foreach (InteractableId interactable in interactableList)
                    {
                        if (interactable.groupType == TankInteractable.InteractableType.LOGISTICS)
                        {
                            newList.Add(interactable);
                        }
                    }
                }

                if (i == 4)
                {
                    foreach (InteractableId interactable in interactableList)
                    {
                        if (interactable.groupType == TankInteractable.InteractableType.CONSUMABLE)
                        {
                            newList.Add(interactable);
                        }
                    }
                }

                if (i == 5)
                {
                    foreach (InteractableId interactable in interactableList)
                    {
                        if (interactable.groupType == TankInteractable.InteractableType.SHOP)
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
        [Button(" This Is Fine", ButtonSizes.Small, Icon = SdfIconType.ThermometerHigh), Tooltip("Don't worry about it")]
        public void IgniteAllRooms()
        {
            Cell[] cells = GetComponentsInChildren<Cell>();
            foreach (Cell cell in cells)
            {
                cell.Ignite();
            }
        }
        [PropertySpace]
        #endregion

        //Runtime Variables:
        private bool isDying = false; //true when the tank is in the process of blowing up
        private float deathSequenceTimer = 0;
        private bool isTransitioning = false; //true when the tank is about to move between scenes
        private float totalDeathSequenceTimer = 0;
        private float transitionSequenceTimer = 0;
        private float transitionSequenceInterval = 0;
        [Tooltip("Describes the size of the tank in each cardinal direction (relative to treadbase). X = height, Y = left width, Z = depth, W = right width.")] internal Vector4 tankSizeValues;
        [Tooltip("Describes whether the tank is currently in the pre-build stage or not.")] internal bool isPrebuilding;

        //UI
        private SpriteRenderer damageSprite;
        [Tooltip("How long damage visual effect persists for")] private float damageTime;
        private float damageTimer;
      
        public static System.Action OnPlayerTankSizeAdjusted;

        //RUNTIME METHODS:
        private void Awake()
        {
            //Get objects & components:
            _thisTankAI = GetComponent<TankAI>();
            
            treadSystem = GetComponentInChildren<TreadSystem>(); //Get tread system from children
            towerJoint = transform.Find("TowerJoint");           //Get tower joint from children
            treadSystem.Initialize();                            //Make sure treads are initialized

            nameText = GetComponentInChildren<TextMeshProUGUI>();
            damageSprite = towerJoint.transform.Find("DiageticUI")?.GetComponent<SpriteRenderer>();
            tankAnimator = GetComponent<Animator>();

            isPrebuilding = true;
            //Room setup:
            rooms = new List<Room>(GetComponentsInChildren<Room>()); //Get list of all rooms which spawn as children of tank (for prefab tanks)

            foreach (Room room in rooms) //Scrub through childed room list (should be in order of appearance under towerjoint)
            {
                room.targetTank = this; //Make this the target tank for all childed rooms
                room.Initialize();      //Prepare room for mounting
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

            //Other setup:
            activeTanks.Add(this); //Add this script to list of active tanks in scene
        }
        private void OnDestroy()
        {
            //Cleanup:
            activeTanks.Remove(this); //Upon destruction, remove a tank from activeTanks list (should make system consistent between scenes)
            OnCoreDamaged = null;     //Remove all events from the core damaged action
        }
        private void Start()
        {
            tankManager = GameObject.Find("TankManager")?.GetComponent<TankManager>();
            
            if (tankManager != null)
            {
                myTankID = TankManager.instance.tanks.FirstOrDefault(tank => tank.tankScript == this);
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
                                Build(_design); //Build the tank
                                cargoNodes = _design.cargoNodes;
                            }
                        }
                    }
                }

                if (tankType == TankId.TankType.PLAYER)
                {
                    tankManager.playerTank = this;
                    TankManager.OnPlayerTankAssigned?.Invoke(this);
                }
            } 

            /*
            //Check Room Typing for Random Drops
            foreach(Room room in rooms)
            {
                if (room.type == Room.RoomType.Armor)
                {
                    GameObject interactable = GameManager.Instance.interactableList[6].gameObject;

                    InteractableId newId = new InteractableId();
                    newId.interactable = interactable;
                    newId.script = interactable.GetComponent<TankInteractable>();
                    //newId.brain = interactable.GetComponent<InteractableBrain>();
                    newId.groupType = newId.script.interactableType;
                    newId.stackName = newId.script.stackName;
                    interactableList.Add(newId);
                    //if (tankType == TankId.TankType.ENEMY) interactablePool.Add(newId);
                }
            }*/

            //Identify what tank I am
            GetTankInfo();

            //Player Cargo Logic
            if (tankType == TankId.TankType.PLAYER)
            {
                CargoManifest manifest = GameManager.Instance.cargoManifest;
                SpawnCargo(manifest);
            }

            //Enemy Logic
            if (tankType == TankId.TankType.ENEMY)
            {
                coreHealth *= 0.5f;
                EnableCannonBrains(false);
                CargoManifest nodeManifest = GetCurrentManifest(true);
                SpawnCargo(nodeManifest);
                int toLoad = LevelManager.Instance.RollSpecialAmmo();
                if (toLoad > 0)
                {
                    LoadRandomWeapons(toLoad);
                }
            }
            currentCoreHealth = coreHealth;
            UpdateUI();

            //Shop Logic
            if (tankType == TankId.TankType.NEUTRAL)
            {
                //isInvincible = true;
                ShopManager shopMan = GetComponent<ShopManager>();
                shopMan.enabled = true;
                shopMan.InitializeShop();
            }

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
                        tankSizeValues.z = Mathf.Max(tankSizeValues.z, Vector3.Project(treadSystem.wheels[x].basePosition, treadSystem.transform.up).magnitude + treadSystem.wheels[x].radius); //Compare each wheel to current winner and pick farthest-extending one
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
        }

        private void FixedUpdate()
        {
            //Death Sequence Events
            if (isDying)
            {
                DeathSequenceEvents();
            }

            //Transition Sequence Events
            if (isTransitioning)
            {
                TransitionSequenceEvents();
            }

            /*//UI
            if (damageTimer > 0)
            {
                UpdateUI();
            }
            */
        }

        private void UpdateUI()
        {
            /*
            Color newColor = damageSprite.color;
            newColor.a = Mathf.Lerp(0, 255f, (damageTimer / damageTime) * Time.fixedDeltaTime);
            damageSprite.color = newColor;*/

            //Diagetic Damage Elements
            if (currentCoreHealth >= coreHealth)
            {
                foreach (Transform element in diageticElements)
                {
                    element.gameObject.SetActive(false);
                }
            }
            else 
            { 
                diageticElements[2].gameObject.SetActive(true);

                if (currentCoreHealth <= (coreHealth * 0.90f)) { diageticElements[9].gameObject.SetActive(true); }
                if (currentCoreHealth <= (coreHealth * 0.80f)) { diageticElements[0].gameObject.SetActive(true); }
                if (currentCoreHealth <= (coreHealth * 0.70f)) { diageticElements[1].gameObject.SetActive(true); }
                if (currentCoreHealth <= (coreHealth * 0.60f)) { diageticElements[4].gameObject.SetActive(true); }
                if (currentCoreHealth <= (coreHealth * 0.50f)) 
                { 
                    diageticElements[6].gameObject.SetActive(true);
                    diageticElements[10].gameObject.SetActive(true); //Smoke
                }
                if (currentCoreHealth <= (coreHealth * 0.40f)) { diageticElements[7].gameObject.SetActive(true); }
                if (currentCoreHealth <= (coreHealth * 0.30f)) { diageticElements[3].gameObject.SetActive(true); }
                if (currentCoreHealth <= (coreHealth * 0.20f)) 
                {
                    if (!alarmTriggered) TriggerTankAlarm();
                    diageticElements[8].gameObject.SetActive(true);
                    diageticElements[11].gameObject.SetActive(true); //Smoke
                }
                if (currentCoreHealth <= (coreHealth * 0.10f)) { diageticElements[5].gameObject.SetActive(true); }
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

        public void MakeFragile()
        {
            isFragile = true;
            currentCoreHealth = 25;
        }

        public void Damage(float amount)
        {
            currentCoreHealth -= amount;

            //UI
            /*damageTime += (amount / 50f);
            damageTimer = damageTime;
            */
            float speedScale = 1;
            if (amount > 50) speedScale = 0.5f;
            if (amount <= 10) speedScale = 4f;
            if (amount > 0) HitEffects(speedScale);

            if (currentCoreHealth <= 0)
            {
                if (!isDying)
                {
                    EventSpawnerManager spawner = GameObject.Find("LevelManager")?.GetComponent<EventSpawnerManager>();
                    if (tankType == TankId.TankType.ENEMY) spawner.EnemyDestroyed(this);
                    if (tankType == TankId.TankType.NEUTRAL) spawner.EncounterEnded(EventSpawnerManager.EventType.FRIENDLY);
                    totalDeathSequenceTimer = Random.Range(2.0f, 3.0f);
                    isDying = true;
                }
            }
            OnCoreDamaged?.Invoke(currentCoreHealth / coreHealth);
        }

        public float Repair(float amount)
        {
            float difference = 0;
            if (currentCoreHealth < coreHealth)
            {
                currentCoreHealth += amount;

                if (currentCoreHealth >= coreHealth)
                {
                    difference = amount + (coreHealth - currentCoreHealth);
                    currentCoreHealth = coreHealth;
                    GameManager.Instance.AudioManager.Play("ItemPickup", treadSystem.gameObject);
                }
                else difference = amount;
                HitEffects(1.5f);
                GameManager.Instance.AudioManager.Play("UseWrench", treadSystem.gameObject);

                OnCoreDamaged?.Invoke(currentCoreHealth / coreHealth);
            }
            return difference;
        }

        public void HitEffects(float speedScale)
        {
            UpdateUI();
            tankAnimator.SetFloat("SpeedScale", speedScale);
            tankAnimator.Play("DamageFlashCore", 0, 0);
        }

        public void DeathSequenceEvents()
        {
            deathSequenceTimer -= Time.fixedDeltaTime;
            totalDeathSequenceTimer -= Time.fixedDeltaTime;
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
                CameraManipulator.main?.ShakeTankCamera(this, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Jolt"));

                //Apply Haptics to Players inside this tank
                foreach (Character character in this.GetCharactersInTank())
                {
                    PlayerMovement player = character.GetComponent<PlayerMovement>();
                    if (player != null)
                    {
                        HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("QuickRumble");
                        GameManager.Instance.SystemEffects.ApplyControllerHaptics(player.GetPlayerData().playerInput, setting); //Apply haptics
                    }
                }

                deathSequenceTimer = Random.Range(0.1f, 0.2f);
            }

            if (totalDeathSequenceTimer <= 0)
            {
                BlowUp(false);
            }
        }

        public void Transition()
        {
            StartCoroutine(TransitionSequence(4f));
        }

        public void TransitionSequenceEvents()
        {
            transitionSequenceTimer -= Time.fixedDeltaTime;
            if (transitionSequenceTimer <= 0)
            {
                Cargo[] cargo = GetComponentsInChildren<Cargo>();
                if (cargo.Length > 0)
                {
                    foreach (Cargo item in cargo)
                    {
                        if (item.type == Cargo.CargoType.SCRAP)
                        {
                            item.Sell(1f);
                            break;
                        }
                    }
                }
                transitionSequenceTimer = transitionSequenceInterval;
            }
        }

        public IEnumerator TransitionSequence(float duration)
        {
            isTransitioning = true;
            isInvincible = true;

            //Determine Interval Based on Duration
            float sequenceDuration = duration + 1;
            Cargo[] cargo = GetComponentsInChildren<Cargo>();
            if (cargo.Length > 0)
            {
                float crateCount = 0;
                foreach(Cargo item in cargo)
                {
                    if (item.type == Cargo.CargoType.SCRAP)
                    {
                        crateCount++;
                    }
                }

                if (crateCount > 0) transitionSequenceInterval = duration / crateCount;
                else transitionSequenceInterval = 0;
            }
            else sequenceDuration = 1;

            yield return new WaitForSeconds(sequenceDuration);
            LevelManager.Instance.CompleteMission();
            isInvincible = false;
        }

        public void BlowUp(bool immediate)
        {
            if (immediate) DestroyImmediate(gameObject);
            else
            {
                //Trigger Screenshake Effects
                CameraManipulator.main?.ShakeTankCamera(this, GameManager.Instance.SystemEffects.GetScreenShakeSetting("TankExplosionShake"));
                CameraManipulator.main?.ShakeTankCamera(this, GameManager.Instance.SystemEffects.GetScreenShakeSetting("Explosion"));

                CameraManipulator.main?.OnTankDestroyed(this);
                TankManager.instance.tanks.Remove(myTankID);
                Cell[] cells = GetComponentsInChildren<Cell>();
                foreach (Cell cell in cells)
                {
                    //Destroy all cells
                    //cell.Kill();

                    //Blow up the core
                    if (cell.room.isCore)
                    {
                        GameManager.Instance.ParticleSpawner.SpawnParticle(5, cell.transform.position, 0.15f, null);
                    }
                }

                //Spawn Cargo
                
                /*foreach (GameObject _cargo in cargoHold)
                {
                    GameObject flyingCargo = Instantiate(_cargo, treadSystem.transform.position, treadSystem.transform.rotation, null);
                    float randomX = Random.Range(-10f, 10f);
                    float randomY = Random.Range(5f, 20f);
                    float randomT = Random.Range(-16f, 16f);
                    Vector2 _random = new Vector2(randomX, randomY);

                    Rigidbody2D rb = flyingCargo.GetComponent<Rigidbody2D>();
                    rb.AddForce(_random * 40);
                    rb.AddTorque(randomT * 10);
                }*/

                if (tankType == TankId.TankType.ENEMY)
                {
                    //If we're checking for enemies destroyed, add 1 to the Objective
                    LevelManager.Instance?.AddObjectiveValue(ObjectiveType.DefeatEnemies, 1);

                    //Random Interactable Drops
                    if (interactablePool.Count > 0)
                    {
                        int random = Random.Range(1, 3); //# of drops
                        for (int i = 0; i < random; i++)
                        {
                            int randomDrop = Random.Range(0, interactablePool.Count); //Randomly Select from Pool
                            TankInteractable interactable = interactablePool[randomDrop].script;

                            INTERACTABLE _interactable = GameManager.Instance.TankInteractableToEnum(interactable); //Convert to Enum
                            StackManager.AddToStack(_interactable); //Add to Stack

                            interactablePool.RemoveAt(randomDrop); //Remove from the Pool
                        }
                    }

                    //Tutorialize Dropped Cargo
                    GameManager.Instance.DisplayTutorial(4, false, 5);
                }

                RemovePlayersFromTank();
                RemoveItemsFromTank();

                if (tankType == TankId.TankType.PLAYER)
                {
                    //If there are no players in the scene, immediately game over
                    if (GameManager.Instance.MultiplayerManager.GetAllPlayers().Length == 0)
                        LevelManager.Instance?.GameOver();
                }

                //Handle Destruction Logic
                if (corpseInstance == null)
                {
                    GenerateCorpse();
                }

                foreach (Room room in rooms) //Make all current rooms in the tank into DummyRooms, then child them to the Corpse
                {
                    if (!room.isCore) //Except the core
                    {
                        room.MakeDummy(corpseInstance.transform);
                        CorpseController.DummyObject _object = new CorpseController.DummyObject();
                        _object.dummyObject = room.gameObject;
                        corpseInstance.GetComponent<CorpseController>().objects.Add(_object);
                        room.enabled = false;
                    }
                }

                DestructionEffects();
                //GameManager.Instance.SystemEffects.ActivateSlowMotion(0.05f, 0.4f, 1.5f, 0.4f);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Removes all of the players in the tank by killing them or unassigning them.
        /// </summary>
        private void RemovePlayersFromTank()
        {
            //Unassign all characters from this tank
            foreach (Character character in GetCharactersAssignedToTank(this))
            {
                character.SetAssignedTank(null);
                character.InterruptRespawn();
            }

            //Detach the characters that are still in the tank and kill them
            foreach (Character character in GetCharactersInTank())
            {
                character.transform.SetParent(null);
                character.KillCharacterImmediate();
            }
        }

        private void RemoveItemsFromTank()
        {
            //Remove Items
            Cargo[] items = GetComponentsInChildren<Cargo>();
            if (items.Length > 0)
            {
                foreach (Cargo item in items)
                {
                    item.transform.SetParent(null); //removes the item from the cell before destruction
                }
            }
        }

        public void DestructionEffects()
        {
            //Big Explosion
            GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(18, treadSystem.transform.position, 1.25f);
            particle.transform.rotation = towerJoint.transform.rotation;
            Vector2 particlePos = new Vector2(treadSystem.transform.position.x, treadSystem.transform.position.y - 0.25f);
            GameObject _particle = GameManager.Instance.ParticleSpawner.SpawnParticle(24, particlePos, 1f);
            _particle.transform.rotation = towerJoint.transform.rotation;

            //Big Sounds
            GameManager.Instance.AudioManager.PlayRandomPitch("MedExplosionSFX", 0.7f, 0.9f, treadSystem.gameObject);
            GameManager.Instance.AudioManager.PlayRandomPitch("CannonFire", 0.8f, 1f, treadSystem.gameObject);
            GameManager.Instance.AudioManager.Play("LargeExplosionSFX", treadSystem.gameObject);

            //Apply haptics to all players
            HapticsSettings haptics = GameManager.Instance.SystemEffects.GetHapticsSetting("BigRumble");
            GameManager.Instance.SystemEffects.ApplyControllerHaptics(haptics);
        }

        public void GenerateCorpse()
        {
            if (corpseInstance != null) return;

            GameObject corpse = Instantiate(corpsePrefab, null, true); //Generate a new Corpse Parent Object
            corpse.transform.position = towerJoint.transform.position;
            corpse.name = TankName + " (Corpse)"; //Name it accordingly
            corpseInstance = corpse;

            if (tankType == TankId.TankType.PLAYER) corpseInstance.GetComponent<CorpseController>().akListener.SetActive(true); //transfer new listener to corpse
        }

        public void DespawnTank()
        {
            //Unassign Camera
            CameraManipulator.main?.OnTankDestroyed(this);

            //Ensure EventManager ends current Event related to this Tank
            EventSpawnerManager spawner = GameObject.Find("LevelManager")?.GetComponent<EventSpawnerManager>();
            
            if (tankType == TankId.TankType.ENEMY) spawner.EnemyDestroyed(this);
            if (tankType == TankId.TankType.NEUTRAL) spawner.EncounterEnded(EventSpawnerManager.EventType.FRIENDLY);

            RemovePlayersFromTank();

            //GameManager.Instance.SystemEffects.ActivateSlowMotion(0.05f, 0.4f, 1.5f, 0.4f);
            Destroy(gameObject);
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

                        //Change my flag if I'm an enemy
                        if (tankType == TankId.TankType.ENEMY)
                        {
                            FlagSettings flag = tankFlag.GetComponent<FlagSettings>();
                            flag.flagSprite = tankManager.tankFlagSprites[1];

                            tankFlag.transform.Rotate(0, 180, 0);
                        }

                        //Change my flag if I'm a player
                        if (tankType == TankId.TankType.PLAYER)
                        {
                            surrenderFlag?.SetActive(false);
                        }
                    }
                }
            }
        }

        public void AddCargo()
        {
            int random = Random.Range(2, 8);
            /*cargoHold = new GameObject[random];
            for (int i = 0; i < cargoHold.Length; i++)
            {
                cargoHold[i] = GameManager.Instance.CargoManager.GetRandomCargo();
            }*/
        }

        public void TriggerTankAlarm()
        {
            if (tankType != TankId.TankType.PLAYER) return;
            foreach(Room room in rooms)
            {
                if (room.roomAnimator != null)
                {
                    room.roomAnimator.Play("Alarm", 0, 0);
                }
            }
            GameManager.Instance.AudioManager.Play("TankAlarm", transform.gameObject);
            alarmTriggered = true;
        }

        
        //FUNCTIONALITY METHODS:
        public void Build(TankDesign tankDesign) //Called from TankManager when constructing a specific design
        {
            //Build core interactables:
            foreach (BuildStep.CellInterAssignment cellInter in tankDesign.coreInteractables) //Iterate through each interactable assignment for core cells
            {
                Cell target = coreRoom.transform.GetChild(0).Find(cellInter.cellName).GetComponent<Cell>();                                                                                                  //Get target cell from core
                TankInteractable interactable = Instantiate(Resources.Load<RoomData>("RoomData").interactableList.FirstOrDefault(item => item.name == cellInter.interRef)).GetComponent<TankInteractable>(); //Get reference to and spawn in designated interactable
                interactable.InstallInCell(target);                                                                                                                                                          //Install interactable in target cell
                if (cellInter.flipped) interactable.Flip();                                                                                                                                                  //Flip interactable if ordered
                
                if (!CampaignManager.Instance.HasCampaignStarted) //Add the interactable to the stats if joining the combat scene first
                    AddInteractableToStats(interactable);
            }

            //Build rooms:
            for (int i = 0; i < tankDesign.buildingSteps.Length; i++) //Loop through all the steps in the design
            {
                //Get variables of the step
                GameObject room = null;

                //Build Normal Room
                foreach(RoomInfo roomInfo in GameManager.Instance.roomList) //Find the prefab we want to spawn
                {
                    if (roomInfo.roomObject.name == tankDesign.buildingSteps[i].roomID)
                    {
                        room = roomInfo.roomObject.gameObject;
                    }
                }

                //Build Special Room
                foreach (RoomInfo roomInfo in GameManager.Instance.specialRoomList) //Find the special prefab we want to spawn
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
                roomScript.targetTank = this;
                roomScript.Mount();
                
                roomScript.ProcessManifest(tankDesign.buildingSteps[i].cellManifest);

                if (!CampaignManager.Instance.HasCampaignStarted && tankType == TankId.TankType.PLAYER) //Add the room to the stats if joining the combat scene first
                    AddRoomToStats(roomScript);

                //Install interactables
                foreach (BuildStep.CellInterAssignment cellInter in tankDesign.buildingSteps[i].cellInteractables) //Iterate through each cell interactable assignment
                {
                    Cell target = roomScript.transform.GetChild(0).Find(cellInter.cellName).GetComponent<Cell>();                                           //Get target cell from room
                    GameObject interPrefab = Resources.Load<RoomData>("RoomData").interactableList.FirstOrDefault(item => item.name == cellInter.interRef); //Try to get prefab for interactable spawned in this step
                    if (interPrefab == null) interPrefab = GameManager.Instance.defaultInteractable.gameObject;
                    TankInteractable interactable = Instantiate(interPrefab).GetComponent<TankInteractable>();                                              //Get reference to and spawn in designated interactable
                    
                    interactable.InstallInCell(target);                                                                                                                                                          //Install interactable in target cell
                    if (cellInter.flipped) interactable.Flip();                                                                                                                                                  //Flip interactable if ordered

                    if (interactable.interactableType == TankInteractable.InteractableType.WEAPONS)
                    {
                        GunController gun = interactable.gameObject.GetComponent<GunController>();

                        if (cellInter.specialAmmo?.Length > 0)
                        {
                            for(int a = 0; a < cellInter.specialAmmo.Length; a++)
                            {
                                GameObject ammoToLoad = GameManager.Instance.CargoManager.GetProjectileByNameHash(cellInter.specialAmmo[a]);
                                gun.AddSpecialAmmo(ammoToLoad, 1, false);
                            }
                        }

                        if (overrideWeaponROF) gun.rateOfFire = 0;
                    }

                    if (!CampaignManager.Instance.HasCampaignStarted) //Add the interactable to the stats if joining the combat scene first
                        AddInteractableToStats(interactable);
                }
            }

            //Assign Ai Settings
            if (tankDesign.aiSettings != "None")
            {
                SetTankAI(tankDesign.aiSettings);
            }

            SetTankName(tankDesign.TankName); //Assign Tank Name
            isPrebuilding = false;
        }

        public TankDesign GetCurrentDesign()
        {
            TankDesign design = new TankDesign();

            int roomCount = 0;
            int cargoCount = 0;
            //Find out how many steps are needed for this design
            foreach(Transform room in towerJoint)
            {
                Room roomScript = room.GetComponent<Room>();
                if (roomScript != null && roomScript.isCore == false)
                {
                    roomCount++;
                }

                if (room.name.Contains("CargoNode"))
                {
                    cargoCount++;
                }
            }

            design.buildingSteps = new BuildStep[roomCount]; //Set up the instructions
            design.cargoNodes = new Vector2[cargoCount]; //Set up nodes

            for (int i = 0; i < design.buildingSteps.Length; i++)
            {
                design.buildingSteps[i] = new BuildStep(); //Initialize steps
            }

            roomCount = 0;
            cargoCount = 0;

            //Fill out instructions with details
            foreach (Transform node in towerJoint)
            {
                Room roomScript = node.GetComponent<Room>();
                if (roomScript != null)
                {
                    //Get interactables:
                    List<BuildStep.CellInterAssignment> cellInters = new List<BuildStep.CellInterAssignment>(); //Create list to store cell interactable assignments
                    foreach(TankInteractable interactable in roomScript.GetComponentsInChildren<TankInteractable>()) //Iterate through interactables in room
                    {
                        string cellName = interactable.parentCell.name;              //Get reference name for cell with interactable
                        string interName = interactable.name.Replace("(Clone)", ""); //Get reference string for installed interactable
                        bool flipStatus = interactable.direction == -1;              //Determine whether or not interactable is flipped
                        string[] specialAmmo = interactable.GetSpecialAmmoRef();     //Determine what (if any) special ammo is currently loaded into this interactable
                        cellInters.Add(new BuildStep.CellInterAssignment(cellName, interName, flipStatus, specialAmmo)); //Add an interactable designator with reference to cell and interactable name
                    }
                    if (roomScript.isCore) //Store interactables for core room but do not try to store spawn information
                    {
                        design.coreInteractables = cellInters.ToArray(); //Save core list of interactables to special list for spawning stuff in the core
                        continue;                                        //Skip the rest of the building steps (core is not built like the rest of the tank)
                    }
                    design.buildingSteps[roomCount].cellInteractables = cellInters.ToArray(); //Save interactables to design

                    //Get room info:
                    string roomID = node.name.Replace("(Clone)", "");
                    design.buildingSteps[roomCount].roomID = roomID; //Name of the room's prefab
                    design.buildingSteps[roomCount].roomType = roomScript.type; //The room's current type
                    design.buildingSteps[roomCount].localSpawnVector = node.transform.localPosition; //The room's local position relative to the tank
                    design.buildingSteps[roomCount].rotate = roomScript.rotTracker; //How many times the room has been rotated before being placed

                    //Get missing cells:
                    design.buildingSteps[roomCount].cellManifest = roomScript.cellManifest; //Get cell manifest from room

                    //TODO:
                    //Is this an enemy or player design?
                    //Cell damage values?
                    
                    roomCount++;
                }

                //Get Cargo Nodes
                if (node.name.Contains("CargoNode"))
                {
                    Vector2 pos = new Vector2(node.transform.localPosition.x, node.transform.localPosition.y);
                    design.cargoNodes[cargoCount] = pos;
                    node.gameObject.SetActive(false);

                    cargoCount++;
                }
            }

            design.TankName = TankName; //Name the design after the current tank
            if (_thisTankAI != null && tankType != TankId.TankType.PLAYER) design.aiSettings = _thisTankAI.aiSettings.name; //Assign Ai Settings based on current Settings
            else design.aiSettings = "None";
            return design;
        }

        public void SpawnCargo(CargoManifest manifest) //Called when spawning a tank that contains cargo/items
        {
            if (GameManager.Instance.cargoManifest?.items.Count > 0) //if we have any cargo
            {
                foreach (CargoManifest.ManifestItem item in manifest.items) //go through each item in the manifest
                {
                    GameObject prefab = null;
                    foreach (CargoId cargoItem in GameManager.Instance.CargoManager.cargoList)
                    {
                        if (cargoItem.id == item.itemID) //if the item id matches an object in the cargomanager
                        {
                            prefab = cargoItem.cargoPrefab; //get the object we need to spawn from the list
                        }
                    }

                    GameObject _item = Instantiate(prefab, towerJoint, false); //spawn the object
                    Cargo script = _item.GetComponent<Cargo>();
                    script.ignoreInit = true;

                    //Assign Values
                    Vector3 spawnVector = item.localSpawnVector;
                    _item.transform.localPosition = spawnVector;
                    script.AssignValue(item.persistentValue);
                }
            }
        }

        /// <summary>
        /// Gets the current layout of spawned items in the tank.
        /// </summary>
        /// <param name="nodeManifest">Whether this manifest is exclusively looking at existing CargoNodes or not.</param>
        /// <returns>Current manifest of items in the tank.</returns>
        public CargoManifest GetCurrentManifest(bool nodeManifest = false)
        {
            CargoManifest manifest = new CargoManifest();

            if (!nodeManifest)
            {
                //Find All Cargo Items currently inside the Tank
                Cargo[] cargoItems = GetComponentsInChildren<Cargo>();

                if (cargoItems.Length > 0)
                {
                    foreach (Cargo item in cargoItems) //go through each item in the tank
                    {
                        string itemID = "";
                        foreach (CargoId _cargo in GameManager.Instance.CargoManager.cargoList)
                        {
                            if (_cargo.id == item.cargoID) //if it's on the list of valid cargo items
                            {
                                itemID = item.cargoID;
                            }
                        }

                        if (itemID != "")
                        {
                            CargoManifest.ManifestItem _item = new CargoManifest.ManifestItem();
                            _item.itemID = itemID; //update id

                            Transform temp = item.transform.parent;
                            item.transform.parent = towerJoint.transform; //set new temp parent

                            //Get Item Information
                            _item.localSpawnVector = item.transform.localPosition; //Get it's current localPosition
                            _item.persistentValue = item.GetPersistentValue();

                            item.transform.parent = temp;
                            manifest.items.Add(_item); //add it to the manifest
                        }
                    }
                }
            }
            else
            {
                if (cargoNodes?.Length > 0) 
                { 
                    foreach (Vector2 node in cargoNodes)
                    {
                        CargoManifest.ManifestItem _item = new CargoManifest.ManifestItem();
                        CargoId random = GameManager.Instance.CargoManager.GetRandomCargo(true);
                        _item.itemID = random.id;
                        _item.localSpawnVector.x = node.x;
                        _item.localSpawnVector.y = node.y;

                        manifest.items.Add(_item);
                    }
                }
            }

            return manifest;
        }

        public void EnableCannonBrains(bool enabled)
        {
            WeaponBrain[] brains = GetComponentsInChildren<WeaponBrain>();
            foreach(WeaponBrain brain in brains)
            {
                brain.enabled = enabled;
            }

            //SimpleTankBrain _brain = GetComponent<SimpleTankBrain>();
            //_brain.enabled = true;
        }
        /// <summary>
        /// Updates envelope describing height and width of tank relative to treadbase.
        /// </summary>
        public void UpdateSizeValues(bool flagUpdate = false)
        {
            //Find extreme cells:
            upMostCell = null;
            foreach (Room room in rooms) //Iterate through all rooms in tank
            {
                foreach (Cell cell in room.cells) //Iterate through all cells in room
                {
                    Vector3 cellPos = treadSystem.transform.InverseTransformPoint(cell.transform.position); //Get shorthand variable for position of cell
                    if (upMostCell == null || treadSystem.transform.InverseTransformPoint(upMostCell.transform.position).y < cellPos.y) upMostCell = cell;          //Save cell if it is taller than tallest known cell in tank
                    if (leftMostCell == null || treadSystem.transform.InverseTransformPoint(leftMostCell.transform.position).x > cellPos.x) leftMostCell = cell;    //Save cell if it is farther left than leftmost known cell in tank
                    if (rightMostCell == null || treadSystem.transform.InverseTransformPoint(rightMostCell.transform.position).x < cellPos.x) rightMostCell = cell; //Save cell if it is farther right than rightmost known cell in tank
                }
            }

            //Update Flags
            if (flagUpdate) UpdateFlagPosition(upMostCell.transform);
            UpdateSurrenderFlagPosition(upMostCell.transform);

            //Calculate tank metrics:
            float highestCellHeight = treadSystem.transform.InverseTransformPoint(upMostCell.transform.position).y + 0.5f;                 //Get height from treadbase to top of highest cell
            float tankLeftSideLength = Mathf.Abs(treadSystem.transform.InverseTransformPoint(leftMostCell.transform.position).x) + 0.5f;   //Get length of tank from center of treadbase to outer edge of leftmost cell
            float tankRightSideLength = Mathf.Abs(treadSystem.transform.InverseTransformPoint(rightMostCell.transform.position).x) + 0.5f; //Get length of tank from center of treadbase to outer edge of rightmost cell
            tankSizeValues = new Vector4(highestCellHeight, tankRightSideLength, tankSizeValues.z, tankLeftSideLength);                    //Store found values

            //If this is the player tank, call the Action
            if (tankType == TankId.TankType.PLAYER)
            {
                OnPlayerTankSizeAdjusted?.Invoke();

                //Add the highest point value to the tank stats
                if (GetHighestPoint() > GameManager.Instance.currentSessionStats.maxHeight)
                    GameManager.Instance.currentSessionStats.maxHeight = GetHighestPoint();
            }

        }

        public void AddInteractable(GameObject interactable)
        {
            InteractableId newId = new InteractableId();
            newId.interactable = interactable;
            newId.script = interactable.GetComponent<TankInteractable>();

            if (interactable.TryGetComponent(out InteractableBrain brain))
            {
                newId.brain = brain;
                newId.brain.myTankAI = _thisTankAI;
                newId.brain.myInteractableID = newId;
                newId.brain.enabled = false;
            }
            newId.groupType = newId.script.interactableType;
            
            newId.stackName = newId.script.stackName;
            interactableList.Add(newId);
            if (tankType == TankId.TankType.ENEMY)
            {
                if (newId.groupType != TankInteractable.InteractableType.CONSUMABLE)
                {
                    interactablePool.Add(newId);
                }
            }
        }

        public void AddRoomToStats(Room currentRoom)
        {
            GameManager.Instance.currentSessionStats.roomsBuilt++;
            GameManager.Instance.currentSessionStats.totalCells += currentRoom.cells.Count;
        }

        public void RemoveRoomFromStats(Room currentRoom)
        {
            GameManager.Instance.currentSessionStats.roomsBuilt--;
            GameManager.Instance.currentSessionStats.totalCells -= currentRoom.cells.Count;
        }

        public void AddInteractableToStats(TankInteractable tankInteractable)
        {
            //If the tank is the player tank, add to the stats
            if (tankType == TankId.TankType.PLAYER)
            {
                //Check what interactable is being installed and update the stats accordingly
                switch (tankInteractable)
                {
                    //Guns
                    case GunController gun:
                        switch (gun.gunType)
                        {
                            case GunController.GunType.CANNON:
                                GameManager.Instance.currentSessionStats.cannonsBuilt++;
                                break;
                            case GunController.GunType.MACHINEGUN:
                                GameManager.Instance.currentSessionStats.machineGunsBuilt++;
                                break;
                            case GunController.GunType.MORTAR:
                                GameManager.Instance.currentSessionStats.mortarsBuilt++;
                                break;
                        }
                        break;
                    //Boiler
                    case EngineController engine:
                        GameManager.Instance.currentSessionStats.boilersBuilt++;
                        break;
                    //Throttle
                    case ThrottleController throttle:
                        GameManager.Instance.currentSessionStats.throttlesBuilt++;
                        break;
                }
            }
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

        public void Surrender()
        {
            surrenderFlag.SetActive(true);
            if (tankFlag != null) tankFlag.SetActive(false);

            //Enable/Disable interact zone for tank claiming - only triggers for specifically Enemy Tanks
            Transform interactZone = surrenderFlag.transform.Find("InteractZone");
            if (tankType != TankId.TankType.ENEMY || !GameManager.Instance.tankClaiming)
            {
                interactZone.gameObject.SetActive(false);
            }

            MakeFragile();
        }

        //UTILITY METHODS:
        public void SetTankName(string newTankName)
        {
            TankName = newTankName;
            if (nameText != null) nameText.text = TankName;
            gameObject.name = "Tank (" + TankName + ")";
        }

        public void SetTankAI(string newTankAi)
        {
            Object[] settings = Resources.LoadAll("TankAISettings", typeof(TankAISettings));
            foreach (TankAISettings setting in settings)
            {
                if (setting.name == newTankAi)
                {
                    _thisTankAI.aiSettings = setting;
                }
            }
        }

        public void LoadRandomWeapons(int weaponCount)
        {
            List<InteractableId> weaponPool = new List<InteractableId>();

            //Get # of Weapons
            foreach (InteractableId id in interactableList)
            {
                if (id.groupType == TankInteractable.InteractableType.WEAPONS) { weaponPool.Add(id); }
            }

            for (int w = 0; w < weaponCount; w++)
            {
                int random = Random.Range(0, weaponPool.Count); //Pick a Random Weapon from the Pool
                GunController gun = weaponPool[random].interactable.GetComponent<GunController>();

                GameObject ammo = null;
                int amount = 3;

                switch (gun.gunType) //Determine Ammo Type & Quantity
                {
                    case GunController.GunType.MACHINEGUN:
                        ammo = GameManager.Instance.CargoManager.projectileList[0].ammoTypes[1];
                        amount *= 20;
                        break;

                    case GunController.GunType.CANNON:
                        ammo = GameManager.Instance.CargoManager.projectileList[1].ammoTypes[1];
                        break;

                    case GunController.GunType.MORTAR:
                        ammo = GameManager.Instance.CargoManager.projectileList[1].ammoTypes[3];
                        break;
                }

                if (ammo != null) gun.AddSpecialAmmo(ammo, amount); //Load the Gun
            }
        }

        public Vector3 GetPlayerSpawnPointPosition() => playerSpawnPoint.position;
        public Character[] GetCharactersInTank() => GetComponentsInChildren<Character>();
        public float GetHighestPoint() => tankSizeValues.x;

        public float GetMaxHealth() => coreHealth;

        public void UpdateFlagPosition(Transform target)
        {
            Vector2 newPos = target.position;
            tankFlag.transform.position = newPos;
            tankFlag.transform.parent = target;
        }

        public void UpdateSurrenderFlagPosition(Transform target)
        {
            Vector2 newPos = target.position;
            surrenderFlag.transform.position = newPos;
        }
    }
}
