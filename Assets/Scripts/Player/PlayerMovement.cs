using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerMovement : Character
{
    #region Fields and Properties

    //input
    [Header("Player Specific Options:")]
    public bool isDebugPlayer;
    public bool jetpackInputHeld;
    
    public bool interactInputHeld;
    
    [SerializeField] private TextMeshProUGUI playerNameText;

    [SerializeField] private PlayerInput playerInputComponent;

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

    public static Action OnPlayerDeath;
    
    private CharacterLegFloater legFloater;

    [SerializeField]
    [Tooltip("For coupler drop downs, how far the stick must be pushed to drop the coupler.")]
    [Range(0, 1)]
    private float couplerStickDeadzone;

    [SerializeField] 
    [Tooltip("For ladders, how far the stick must be pushed left or right to dismount the ladder.")]
    [Range(0, 1)]
    private float ladderDismountDeadzone;
    
    [SerializeField] 
    [Tooltip("For ladders, how far the stick must be pushed down or up to enter the ladder.")]
    [Range(0, 1)]
    private float ladderEnterDeadzone;

    [SerializeField] private float ladderDetectionRadius;

    [SerializeField]
    [Tooltip("Player speed would reach zero at this slope value. The higher this value is set, the faster the player will walk on slopes up until this value. ")]
    private float maxSlope = 100;
    
    [SerializeField]
    private float maxYVelocity, maxXVelocity;

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
        
        if (TryGetComponent<CharacterLegFloater>(out CharacterLegFloater _legFloater))
        {
            legFloater = _legFloater;
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    private void OnEnable()
    {
        GetComponentInChildren<CanvasGroup>().alpha = GameManager.Instance.UIManager.isVisible ? 1 : 0;
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

        //Debug.Log(rb.velocity);
        if (rb.velocity.y > maxYVelocity)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxYVelocity);
        }
        
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
        
        currentLadder = Physics2D.OverlapCircle(transform.position, ladderDetectionRadius, ladderLayer)?.gameObject;

        //Interactable building:
        if (buildCell != null) //Player is currently in build mode
        {
            currentState = CharacterState.REPAIRING;
            transform.position = buildCell.repairSpot.position;

            timeBuilding += Time.deltaTime; //Increment build time tracker
            if (timeBuilding >= characterSettings.buildTime) //Player has finished build
            {
                StackManager.BuildTopStackItem().InstallInCell(buildCell); //Install interactable from top of stack into designated build cell
                StopBuilding();                                            //Indicate that build has stopped
                CancelInteraction();
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

            if (currentObject.type == Cargo.CargoType.TOOL)
            {
                CargoSprayer sprayer = currentObject.GetComponent<CargoSprayer>();
                if (sprayer != null)
                {
                    sprayer.UpdateNozzle(moveInput);
                }
            }
        }

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
        var slope = transform.eulerAngles.z < 180 ? transform.eulerAngles.z : transform.eulerAngles.z - 360;
        //^ im gonna change this to use a raycast and a normal, but for now this is fine and works for checking in-tank slopes
        
        float deAccel = Mathf.InverseLerp(maxSlope, 0, Mathf.Abs(slope));
        
        if (Mathf.Sign(slope) == Mathf.Sign(moveInput.x))
        {
            currentGroundMoveSpeed = groundMoveSpeed * deAccel;
        } else currentGroundMoveSpeed = groundMoveSpeed;
        
        Vector2 force = transform.right * (moveInput.x * ((CheckGround()) ? currentGroundMoveSpeed : defaultAirForce));
        
        if (CheckGround())
        {
            rb.AddForce(force, ForceMode2D.Impulse);
            //draw gizmo for force
            if (!jetpackInputHeld)
            {
                Vector2 localVel = transform.InverseTransformDirection(rb.velocity);
                if (localVel.y is >= 0 and < 1.5f) localVel.y = 0; // if we are grounded, and not trying to move up,
                                                     // we always want our local up axis, which is aligned with the tank, to be 0
                                                     // if we are over 2 velocity, this code will not run, this fixes
                                                     // issues where you would stop moving in the middle of flying up a coupler
                rb.velocity = transform.TransformDirection(localVel);
            }
        }
        else
        {
            rb.AddForce(force * 5, ForceMode2D.Force); //* 5 just makes the air force value more intuitive when compared to ground speed, air force requires more force because
                                                       // its using force.force instead of impulse
        }
        
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        
        if (CheckGround()) localVelocity.x = Mathf.Lerp(localVelocity.x, 0, groundDeAcceleration * Time.deltaTime);
        
        rb.velocity = transform.TransformDirection(localVelocity);
        
        if (Mathf.Abs(rb.velocity.x) > maxXVelocity && moveInput.x != 0)
        {
            rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxXVelocity, rb.velocity.y);
        }

        if (moveInput.x == 0 && CheckGround()) //slip down slopes
        {
            rb.AddForce(-transform.right.normalized * ((slope * .33f) * slipSlopeValue), ForceMode2D.Force);
        }

        
        if (jetpackInputHeld && currentFuel > 0)
        {
            PropelJetpack();
        }
    }


    protected override void ClimbLadder()
    {
        float raycastDistance = .19f;
        
        var onCoupler = Physics2D.OverlapBox(transform.position, transform.localScale, transform.eulerAngles.z, 1 << LayerMask.NameToLayer("Coupler"));
        var onLadder = Physics2D.OverlapBox(transform.position, transform.localScale, transform.eulerAngles.z, 1 << LayerMask.NameToLayer("Ladder"));
        var hitGround = Physics2D.Raycast(transform.position, -transform.up, raycastDistance, 1 << LayerMask.NameToLayer("Ground"));
        
        Vector3 displacement = transform.up * (climbSpeed * moveInput.y * Time.deltaTime);

        // If you hit ground, and you're trying to move down, stop climbing.
        // If you're not on a coupler or ladder, and you're trying to move up, stop climbing.
        // This makes it so you can climb up through couplers, if the coupler is at the end of the ladder.
        if ((hitGround && moveInput.y < 0) || (!onCoupler && !onLadder && moveInput.y > 0))
        {
            displacement = Vector3.zero;
        }
        
        if (!onCoupler && !onLadder) //final failsafe for if you're somehow in this state and not in a ladder or coupler
        {
             SwitchOffLadder();
        }

        Vector3 newPosition = transform.position + displacement;
        rb.MovePosition(newPosition);

        if (Mathf.Abs(moveInput.x) > ladderDismountDeadzone || jetpackInputHeld)
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

        SetCharacterMovement(ctx.ReadValue<Vector2>());
        
        if (((moveInput.y > ladderEnterDeadzone) || (moveInput.y < -ladderEnterDeadzone && !CheckGround())) && (currentLadder != null && currentState != CharacterState.CLIMBING))
        {   //if up is pressed above the deadzone, if down is pressed under the deadzone and we aren't grounded, and if we are near a ladder and we aren't already climbing
            SetLadder();
        }
        
        if (moveInput.y < -couplerStickDeadzone && CheckSurfaceCollider(18) != null)
        {
            if (CheckSurfaceCollider(18).gameObject.TryGetComponent(out PlatformCollisionSwitcher collSwitcher))
            {
                StartCoroutine(collSwitcher.DisableCollision(GetComponent<Collider2D>())); //Disable collision with platform
            }                                                       // if leg floater is present, 
                                                                    // disable use on platform  (leg floater floats the character collider by a few pixels which means the character won't slip and also won't trip over small changes in terrain collision)
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

        if (ctx.action.WasReleasedThisFrame())
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
                if (cell.room.targetTank.tankType == TankId.TankType.PLAYER && cell.interactable == null && cell.playerBuilding == null && //Cell is friendly and unoccupied
                    !cell.room.isCore &&                      //Interactables cannot be built in the core
                    cell.room.type == Room.RoomType.Standard) //Interactables cannot be built in armor or cargo rooms
                {
                    buildCell = cell;           //Indicate that player is building in this cell
                    cell.playerBuilding = this; //Indicate that this player is building in given cell
                    print("started building");
                    taskProgressBar?.StartTask(characterSettings.buildTime);
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
            if (currentInteractable == null && currentState != CharacterState.OPERATING && currentObject == null)
            {
                LayerMask mask = LayerMask.GetMask("Cell");
                Collider2D cell = Physics2D.OverlapPoint(transform.position, mask);
                if (cell != null)
                {
                    Cell cellscript = cell.GetComponent<Cell>();
                    if (cellscript.health < cellscript.maxHealth && cellscript.repairMan == null && cellscript.room.isCore == false)
                    {
                        GameManager.Instance.AudioManager.Play("UseSFX");
                        currentRepairJob = cell.GetComponent<Cell>();
                        currentRepairJob.repairMan = this.gameObject;
                        repairTimer = 0;
                    }
                }
            }

            //Interactables
            if (currentInteractable != null) currentInteractable.SecondaryUse(true);

            //Items
            if (currentObject != null)
            {
                currentObject.Use(true);
            }
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

            if (currentObject != null)
            {
                currentObject.Use(false);
            }
        }
    }

    public void OnSelfDestruct(InputAction.CallbackContext ctx)
    {
        if (!isAlive || LevelManager.Instance == null) return;

        if (ctx.started)
        {
            isShaking = true;
            currentShakeTime = 0f;
        }

        if (ctx.performed)
        {
            characterVisualParent.localPosition = Vector3.zero;
            KillCharacterImmediate();
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
        timeBuilding = 0;                //Reset build time tracker
        currentState = CharacterState.NONCLIMBING;
        taskProgressBar?.EndTask();
        print("stopped building");
    }

    public void UpdatePlayerNameDisplay()
    {
        playerNameText.text = playerData.GetPlayerName();
    }

    public PlayerData GetPlayerData() => playerData;

    protected override void OnCharacterDeath()
    {
        base.OnCharacterDeath();

        if (permaDeath)
        {
            OnPlayerDeath?.Invoke();
        }
    }

    protected override void ResetPlayer()
    {
        base.ResetPlayer();

        if (currentObject != null)
            currentObject.Drop(this, true, moveInput);

        if (TankManager.instance != null)
            SetAssignedTank(TankManager.instance.playerTank);
    }
    #endregion


}