using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerMovement : Character
{
    #region Fields and Properties

    //input
    public bool isDebugPlayer;
    private Vector2 moveInput;
    private bool jetpackInputHeld;
    
    public bool interactInputHeld;

    [SerializeField] private Transform towerJoint;
    [SerializeField] private Transform playerSprite;
    [SerializeField] private TextMeshProUGUI playerNameText;

    [SerializeField] private PlayerInput playerInputComponent;
    [SerializeField] private float deAcceleration;
    [SerializeField] private float maxSpeed;
    public float fuel;

    InputActionMap inputMap;
    private bool isHoldingDown = false;

    [Header("Joystick Spin Detection Options")]
    [SerializeField] private float spinAngleCheckUpdateTimer = 0.1f;
    [SerializeField] [Range(0.0f, 180.0f)] private float spinValidAngleLimit = 30.0f;
    [SerializeField] private int validSpinCheckRows = 1;
    [SerializeField] private float cannonScrollSensitivity = 3f;
    public bool isSpinningJoystick = false;
    private float spinningDirection = 1; //1 = Clockwise, -1 = CounterClockwise
    public float spinningForce = 0;

    //Joystick spin detection
    private Vector2 lastJoystickInput = Vector2.zero;
    private bool isCheckingSpinInput = false;
    private int validSpinCheckCounter = 0;

    //objects
    [Header("Interactables")]
    public InteractableZone currentZone = null;
    public bool isOperator; //true if player is currently operating an interactable
    public TankInteractable currentInteractable; //what interactable player is currently operating

    [SerializeField, Tooltip("Time it takes player to build an interactable.")] private float buildTime;
    private Cell buildCell;         //Cell player is currently building a stack interactable in (null if not building)
    private float timeBuilding = 0; //Time spent in build mode

    [Header("Objects")]
    public LayerMask carryableObjects;
    public bool isCarryingSomething; //true if player is currently carrying something
    public Cargo currentObject; //what object the player is currently carrying

    [Header("Repairing")]
    [SerializeField, Tooltip("What cell the player is currently repairing")] private Cell currentRepairJob; //what cell the player is currently repairing
    [SerializeField, Tooltip("Time it takes the player to complete one repair tick")] private float repairTime;
    private float repairTimer = 0;
    [SerializeField, Tooltip("How much health the player repairs each completed tick")] private float repairAmount;

    private float maxShakeIntensity = 0.5f;
    private float currentShakeTime;
    private bool isShaking = false;

    private PlayerData playerData;

    #endregion

    #region Unity Methods

    protected override void Awake()
    {
        base.Awake();

        if (isDebugPlayer)
        {
            PlayerInput debugInput = GameObject.Find("Debug_TankBuilder").GetComponent<PlayerInput>();
            if (debugInput != null) AddDebuggerPlayerInput(debugInput);
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    private void OnDisable()
    {
        if (playerInputComponent != null)
        {
            if (isDebugPlayer)
                inputMap.actionTriggered -= OnDebugInput;

            else
                inputMap.actionTriggered -= OnPlayerInput;

            playerInputComponent.onDeviceLost -= OnDeviceLost;
            playerInputComponent.onDeviceRegained -= OnDeviceRegained;
        }
    }

    public void AddDebuggerPlayerInput(PlayerInput debugPlayerInput)
    {
        LinkPlayerInput(debugPlayerInput);
    }

    protected override void Update()
    {
        base.Update();
        if (!isAlive)
        {
            if (GameManager.Instance.AudioManager.IsPlaying("JetpackRocket", gameObject))
            {
                GameManager.Instance.AudioManager.Stop("JetpackRocket", gameObject);
            }
            return;
        }

        if (isShaking)
            ShakePlayer();
        
        currentLadder = Physics2D.OverlapCircle(transform.position, .02f, ladderLayer)?.gameObject;

        //Interactable building:
        if (buildCell != null) //Player is currently in build mode
        {
            timeBuilding += Time.deltaTime; //Increment build time tracker
            if (timeBuilding >= buildTime) //Player has finished build
            {
                StackManager.BuildTopStackItem().InstallInCell(buildCell); //Install interactable from top of stack into designated build cell
                StopBuilding();                                            //Indicate that build has stopped
            }
        }

        //Repairing Something
        if (currentRepairJob != null)
        {
            currentState = CharacterState.REPAIRING;
            transform.position = currentRepairJob.repairSpot.position;

            repairTimer += Time.deltaTime;
            if (repairTimer >= repairTime)
            {
                currentRepairJob.Repair(repairAmount);
                repairTimer = 0;
            }

            if (currentRepairJob.health >= currentRepairJob.maxHealth) //Fully fixed!
            {
                GameManager.Instance.AudioManager.Play("ItemPickup", gameObject);
                currentRepairJob.repairMan = null;
                currentRepairJob = null;
                CancelInteraction();
            }
        }

        //Handle Jetpack
        if (jetpackInputHeld && currentState != CharacterState.OPERATING)
        {
            currentFuel -= fuelDepletionRate * Time.deltaTime;
            if (currentFuel < 0) currentFuel = 0;

            if (!GameManager.Instance.AudioManager.IsPlaying("JetpackRocket", gameObject))
            {
                GameManager.Instance.AudioManager.Play("JetpackRocket", gameObject);
            }
        }
        else 
        {
            if (CheckGround() || currentState == CharacterState.OPERATING)
            {
                currentFuel += fuelRegenerationRate * Time.deltaTime;
                if (currentFuel > 100) currentFuel = 100;
            }

            if (GameManager.Instance.AudioManager.IsPlaying("JetpackRocket", gameObject))
            {
                GameManager.Instance.AudioManager.Stop("JetpackRocket", gameObject);
            }
        }
        
        //If we're manning an Interactable
        if (currentInteractable != null)
        {
            currentState = CharacterState.OPERATING;

            if (interactInputHeld && currentInteractable.isContinuous)
            {
                currentInteractable.Use();
            }

            if (currentInteractable.canAim)
            {
                CheckJoystickSpinning();
                if (isSpinningJoystick)
                {
                    currentInteractable.Rotate(spinningForce);
                }
                else currentInteractable.Rotate(0);
            }
        }

        //If we're carrying something
        if (currentObject != null)
        {
            currentObject.transform.position = hands.position;
        }

        fuel = currentFuel;

        if (moveInput.y <= -0.5) isHoldingDown = true;
        else isHoldingDown = false;
    }
                
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(ladderBounds.center, ladderBounds.size);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (transform.up * .2f));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position - (transform.up * .2f));
    }

    /*protected override void OnTriggerEnter2D(Collider2D other)//TODO: Check Character.cs ontrigger for more info
    {
        base.OnTriggerEnter2D(other);
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
    }*/

    #endregion

    #region Movement

    
    protected override void MoveCharacter()
    {
        
        
        float force = transform.right.x * moveInput.x * moveSpeed; 

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.x = force;
        Vector3 worldVelocity = transform.TransformDirection(localVelocity);
        
        if (jetpackInputHeld && currentFuel > 0)
        {
            PropelJetpack();
        }
        if (CheckGround()) rb.velocity = worldVelocity;
        else rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);
    }


    protected override void ClimbLadder()
    { 
        float raycastDistance = .3f;
        
        RaycastHit2D hitUp = Physics2D.Raycast(transform.position, transform.up, raycastDistance, 1 << LayerMask.NameToLayer("LadderEnd"));
        RaycastHit2D hitDown = Physics2D.Raycast(transform.position, -transform.up, raycastDistance, 1 << LayerMask.NameToLayer("LadderEnd"));
        
        Vector3 displacement = transform.up * (climbSpeed * moveInput.y * Time.deltaTime);
        
         if ((moveInput.y > 0 && hitUp) || (moveInput.y < 0 && hitDown))
         {
             displacement = Vector3.zero;
        }

        Vector3 newPosition = transform.position + displacement;
        rb.MovePosition(newPosition);

        if (moveInput.x > 0.2 || moveInput.x < -0.2 || jetpackInputHeld)
        {
            SwitchOffLadder();
        }
    }

    public void SetFuel(float value)
    {
        currentFuel = value;
        GameManager.Instance.AudioManager.Play("JetpackRefuel", gameObject);
    }

    #endregion

    #region Input
    public void LinkPlayerInput(PlayerInput newInput)
    {
        playerInputComponent = newInput;
        playerData = PlayerData.ToPlayerData(newInput);
        characterIndex = playerInputComponent.playerIndex;

        //Gets the player input action map so that events can be subscribed to it
        if (isDebugPlayer)
        {
            inputMap = playerInputComponent.actions.FindActionMap("Debug");
            inputMap.actionTriggered += OnDebugInput;
        }
        else
        {
            inputMap = playerInputComponent.actions.FindActionMap("Player");
            inputMap.actionTriggered += OnPlayerInput;
        }

        //Subscribes events for control lost / regained
        playerInputComponent.onDeviceLost += OnDeviceLost;
        playerInputComponent.onDeviceRegained += OnDeviceRegained;

        characterColor = GameManager.Instance.MultiplayerManager.GetPlayerColors()[newInput.playerIndex];
        UpdatePlayerNameDisplay();
    }

    public void OnDeviceLost(PlayerInput playerInput)
    {
        Debug.Log("Player " + (characterIndex + 1) + " Controller Disconnected!");
        FindObjectOfType<CornerUIController>().OnDeviceLost(characterIndex);
    }

    public void OnDeviceRegained(PlayerInput playerInput)
    {
        Debug.Log("Player " + (characterIndex + 1) + " Controller Reconnected!");
        FindObjectOfType<CornerUIController>().OnDeviceRegained(characterIndex);
    }

    private void OnPlayerInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "Move": OnMove(ctx); break;
            case "Pause": OnPause(ctx); break;
            case "Jetpack": OnJetpack(ctx);  break;
            case "Control Steering": OnControlSteering(ctx); break;
            case "Interact": OnInteract(ctx); break;
            case "Cancel": OnCancel(ctx); break;
            case "Repair": OnRepair(ctx); break;
            case "Build": OnBuild(ctx); break;
            case "Persona3": OnSelfDestruct(ctx); break;
        }
    }

    private void OnDebugInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "Move": OnMove(ctx); break;
            case "Pause": OnPause(ctx); break;
            case "1": OnJetpack(ctx); break;
            case "Control Steering": OnControlSteering(ctx); break;
            case "2": OnInteract(ctx); break;
            case "3": OnRepair(ctx); break;
            case "4": OnCancel(ctx); break;
            case "Build": OnBuild(ctx); break;
            case "Persona3": OnSelfDestruct(ctx); break;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!isAlive) return;

        moveInput = ctx.ReadValue<Vector2>();
        
        if (ctx.started && moveInput.y > 0 && currentLadder != null && currentState != CharacterState.CLIMBING)
        {
            SetLadder();
        }
        
        if (moveInput.y < 0 && CheckSurfaceCollider(18) != null)
        {
            if (CheckSurfaceCollider(18).gameObject.TryGetComponent(out PlatformCollisionSwitcher collSwitcher))
            {
                StartCoroutine(collSwitcher.DisableCollision(GetComponent<Collider2D>()));
            }
        }
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        //If the player presses the pause button
        if (ctx.started)
        {

            if (!LevelManager.Instance.isPaused)
            {
                //Pause the game
                Debug.Log("Player " + characterIndex + " Paused.");
                LevelManager.Instance?.PauseToggle(characterIndex);
            }
        }
    }

    public void OnJetpack(InputAction.CallbackContext ctx)
    {
        if (!isAlive) return;
        if (buildCell != null) return;

        jetpackInputHeld = ctx.ReadValue<float>() > 0;
        if (ctx.ReadValue<float>() > 0 && currentState != CharacterState.OPERATING) GameManager.Instance.AudioManager.Play("JetpackStartup", gameObject);
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!isAlive) return;
        if (buildCell != null) return;

        interactInputHeld = ctx.ReadValue<float>() > 0;

        if (ctx.started && currentState != CharacterState.REPAIRING)
        {
            if (currentZone != null && !isHoldingDown)
            {
                currentZone.Interact(this.gameObject);
            }

            if (currentInteractable != null && isOperator)
            {
                currentInteractable.Use();
            }

            if (currentInteractable == null && !isCarryingSomething && isHoldingDown)
            {
                Pickup();
            }
        }

        if (ctx.performed)
        {
            if (currentInteractable != null)
            {
                currentInteractable.CancelUse();
            }
        }
    }

    public void OnBuild(InputAction.CallbackContext ctx)
    {
        if (!isAlive) return;

        if (StackManager.stack.Count > 0 && ctx.performed && !isOperator)
        {
            //Check if build is valid:
            Collider2D cellColl = Physics2D.OverlapPoint(transform.position, LayerMask.GetMask("Cell")); //Get cell player is currently on top of (if any)
            if (cellColl != null && cellColl.TryGetComponent(out Cell cell)) //Player is on top of a cell
            {
                if (cell.room.targetTank.tankType == TankId.TankType.PLAYER && cell.interactable == null && cell.playerBuilding == null) //Cell is friendly and unoccupied
                {
                    buildCell = cell;           //Indicate that player is building in this cell
                    cell.playerBuilding = this; //Indicate that this player is building in given cell
                    print("started building");
                } else print("tried to start building");
            }
        }
        
        if (buildCell != null && ctx.canceled) //Player is cancelling a build
        {
            StopBuilding(); //Stop building
        }
    }

    public void OnCancel(InputAction.CallbackContext ctx)
    {
        if (!isAlive) return;

        if (ctx.started)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Exit(true);
            }

            if (currentObject != null)
            {
                if (isHoldingDown) { currentObject.Drop(this, false, moveInput); }
                else currentObject.Drop(this, true, moveInput);
            }
        }
    }

    public void OnControlSteering(InputAction.CallbackContext ctx)
    {
        if (!isAlive) return;

        float steeringValue = ctx.ReadValue<float>();
        int _steeringValue = 0;
        if (currentInteractable != null && isOperator && Mathf.Abs(steeringValue) > 0.5f)
        {
            if (steeringValue > 0.5f) _steeringValue = 1;
            if (steeringValue < -0.5f) _steeringValue = -1;

            currentInteractable.Shift(_steeringValue);
        }
    }

    public void OnRepair(InputAction.CallbackContext ctx)
    {
        if (buildCell != null) return;

        if (ctx.started) //Tapped
        {
            //Items
            if (currentObject != null) currentObject.Use();
        }

        if (ctx.performed) //Held for 0.4 sec
        {
            //Repairing
            if (currentInteractable == null && currentState != CharacterState.OPERATING)
            {
                LayerMask mask = LayerMask.GetMask("Cell");
                Collider2D cell = Physics2D.OverlapPoint(transform.position, mask);
                if (cell != null)
                {
                    Cell cellscript = cell.GetComponent<Cell>();
                    if (cellscript.health < cellscript.maxHealth && cellscript.repairMan == null && cellscript.room.isCore == false)
                    {
                        //Debug.Log("I can fix it!");
                        GameManager.Instance.AudioManager.Play("UseSFX");
                        currentRepairJob = cell.GetComponent<Cell>();
                        currentRepairJob.repairMan = this.gameObject;
                        repairTimer = 0;
                    }
                }
            }

            //Interactables
            if (currentInteractable != null) currentInteractable.SecondaryUse(true);
        }

        if (ctx.canceled) //Let go
        {
            if (currentInteractable != null) currentInteractable.SecondaryUse(false);

            if (currentState == CharacterState.REPAIRING)
            {
                currentRepairJob.repairMan = null;
                currentRepairJob = null;
                CancelInteraction();
            }
        }
    }

    public void OnSelfDestruct(InputAction.CallbackContext ctx)
    {
        if (!isAlive) return;

        if (ctx.started)
        {
            isShaking = true;
            currentShakeTime = 0f;
        }

        if (ctx.performed)
        {
            characterVisualParent.localPosition = Vector3.zero;
            SelfDestruct();
            isShaking = false;
        }

        if (ctx.canceled)
        {
            characterVisualParent.localPosition = Vector3.zero;
            isShaking = false;
        }
    }

    private void ShakePlayer()
    {
        currentShakeTime += Time.deltaTime;

        float maxShakeTime = 3f;

        if (currentShakeTime >= 1f)
        {
            // Normalize the shake intensity between 1 second and 3 seconds
            float normalizedTime = Mathf.Clamp01((currentShakeTime - 1f) / (maxShakeTime - 1f));
            float shakeIntensity = normalizedTime * maxShakeIntensity;

            // Use Perlin noise for smooth random movement in each axis
            float offsetX = (Mathf.PerlinNoise(Time.time * 10f, 0f) - 0.5f) * 2f * shakeIntensity;
            float offsetY = (Mathf.PerlinNoise(Time.time * 10f, 100f) - 0.5f) * 2f * shakeIntensity;
            float offsetZ = (Mathf.PerlinNoise(Time.time * 10f, 200f) - 0.5f) * 2f * shakeIntensity;

            // Apply the shake offset to the characterVisualParent's local position
            characterVisualParent.localPosition = new Vector3(offsetX, offsetY, offsetZ);
        }
    }

    private void CheckJoystickSpinning()
    {
        //If the current movement vector is different from the previous movement vector and spinning input is not being checked
        if (moveInput != lastJoystickInput && !isCheckingSpinInput)
        {
            //Check for spin input
            isCheckingSpinInput = true;
            StartCoroutine(JoystickSpinningDetection());
        }

        //If the number of spin checks is equal to number of spins that are needed, the joystick has been properly spun
        if (validSpinCheckCounter == validSpinCheckRows)
        {
            isSpinningJoystick = true;
        }

        //If not, the joystick is not spinning properly
        else
        {
            isSpinningJoystick = false;
        }
    }

    private IEnumerator JoystickSpinningDetection()
    {
        //Store the movement variable for later use
        lastJoystickInput = moveInput;

        //Wait for a bit to check for a spin angle
        yield return new WaitForSeconds(spinAngleCheckUpdateTimer);

        //If the angle between the last known movement vector and the current movement vector reaches a specified amount
        if (Vector2.Angle(lastJoystickInput, moveInput) >= spinValidAngleLimit)
        {
            //Detect rotation direction
            var spinAngle = Vector2.SignedAngle(lastJoystickInput, moveInput);
            if (spinAngle > 0) spinningDirection = -1;
            else if (spinAngle < 0) spinningDirection = 1;

            //Assign rotation force variable
            spinningForce = (spinAngle / 100);

            //Register this as a joystick spin
            validSpinCheckCounter++;
            validSpinCheckCounter = Mathf.Clamp(validSpinCheckCounter, 0, validSpinCheckRows);
        }
        //If not, there is not enough movement to consider the action a spin. Reset
        else
        {
            validSpinCheckCounter = 0;
        }

        //End the check
        isCheckingSpinInput = false;
    }

    public override void LinkPlayerHUD(PlayerHUD newHUD)
    {
        characterHUD = newHUD;
        characterHUD.InitializeHUD(characterIndex, playerData.GetPlayerName());
    }

    #endregion

    #region Character Functions

    public void Pickup()
    {
        Vector2 zone = new Vector2(1.2f, 1.2f);
        List<Cargo> objects = new List<Cargo>();
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, zone, 0f, carryableObjects);

        if (colliders.Length > 0)
        {
            foreach (Collider2D collider in colliders) //Look through each collider we're touching
            {
                Cargo item = collider.transform.GetComponent<Cargo>();
                if (item != null) objects.Add(item); //Add it to the list of cargo 
            }

            float distance = 5f;
            foreach (Cargo item in objects)
            {
                float _distance = Vector2.Distance(item.transform.position, transform.position);
                if (_distance < distance) distance = _distance;
            }

            foreach (Cargo item in objects)
            {
                float _distance = Vector2.Distance(item.transform.position, transform.position);
                if (_distance == distance) item.Pickup(this);
            }
        }
    }
    public void StopBuilding()
    {
        if (buildCell == null) return;   //Do nothing if player is not building
        buildCell.playerBuilding = null; //Indicate to cell that it is no longer being built in
        buildCell = null;                //Clear cell reference
        buildTime = 0;                   //Reset build time tracker
        print("stopped building");
    }

    public void UpdatePlayerNameDisplay()
    {
        playerNameText.text = playerData.GetPlayerName();
    }

    protected override void OnCharacterDeath(bool isRespawnable = true)
    {
        base.OnCharacterDeath(isRespawnable);
    }

    protected override void ResetPlayer()
    {
        base.ResetPlayer();
    }

    #endregion
}