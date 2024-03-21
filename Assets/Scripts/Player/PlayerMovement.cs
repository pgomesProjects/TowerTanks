using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

public class PlayerMovement : Character
{
    #region Fields and Properties

    //input
    public bool isDebugPlayer;
    private Vector2 moveInput;
    private bool jetpackInputHeld;

    [SerializeField] private Transform towerJoint;
    [SerializeField] private Transform playerSprite;

    [SerializeField] private PlayerInput playerInputComponent;
    [SerializeField] private float deAcceleration;
    [SerializeField] private float maxSpeed;
    public float fuel;

    InputActionMap inputMap;
    private float vel;

    [Header("Joystick Spin Detection Options")]
    [SerializeField] private float spinAngleCheckUpdateTimer = 0.1f;
    [SerializeField] [Range(0.0f, 180.0f)] private float spinValidAngleLimit = 30.0f;
    [SerializeField] private int validSpinCheckRows = 1;
    [SerializeField] private float cannonScrollSensitivity = 3f;
    public bool isSpinningJoystick = false;
    private float spinningDirection = 1; //1 = Clockwise, -1 = CounterClockwise
    private float spinningForce = 0;

    //Joystick spin detection
    private Vector2 lastJoystickInput = Vector2.zero;
    private bool isCheckingSpinInput = false;
    private int validSpinCheckCounter = 0;

    //objects
    [Header("Interactables")]
    public InteractableZone currentZone = null;
    public bool isOperator; //true if player is currently operating an interactable
    public TankInteractable currentInteractable; //what interactable player is currently operating

    #endregion

    #region Unity Methods

    protected override void Awake()
    {
        base.Awake();

        if (isDebugPlayer)
        {
            PlayerInput debugInput = GameObject.Find("Debug_TankBuilder").GetComponent<PlayerInput>();
            AddDebuggerPlayerInput(debugInput);
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    public void AddDebuggerPlayerInput(PlayerInput debugPlayerInput)
    {
        LinkPlayerInput(debugPlayerInput);
    }

    protected override void Update()
    {
        if (jetpackInputHeld && currentState != CharacterState.OPERATING)
        {
            currentFuel -= fuelDepletionRate * Time.deltaTime;
            if (currentFuel < 0) currentFuel = 0;

            if (!GameManager.Instance.AudioManager.IsPlaying("JetpackRocket"))
            {
                GameManager.Instance.AudioManager.Play("JetpackRocket");
            }
        }
        else if (CheckGround() || currentState == CharacterState.CLIMBING)
        {
            currentFuel += fuelRegenerationRate * Time.deltaTime;
            if (currentFuel > 100) currentFuel = 100;

            if (GameManager.Instance.AudioManager.IsPlaying("JetpackRocket"))
            {
                GameManager.Instance.AudioManager.Stop("JetpackRocket");
            }
        }
        
        
        if (currentInteractable != null)
        {
            currentState = CharacterState.OPERATING;

            CheckJoystickSpinning();
            if (isSpinningJoystick)
            {
                currentInteractable.Rotate(spinningForce);
            }
            else currentInteractable.Rotate(0);
        }

        fuel = currentFuel;
    }
                
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
    }

    #endregion

    #region Movement

    
    protected override void MoveCharacter()
    {
        int ladderLayerIndex = LayerMask.NameToLayer("Ladder");
        LayerMask ladderLayer = 1 << ladderLayerIndex;
        var ladder = Physics2D.OverlapCircle(transform.position, .5f, ladderLayer)?.gameObject;

        if (ladder != null)
        {
            currentLadder = ladder;
            Debug.Log("Ladder found");
        }
        else
        {
            currentLadder = null;
            Debug.Log("Ladder not found");
        }
        
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


    protected override void ClimbLadder() //todo: parent the player to the ladder and then when exit, child back to towerjoint
    {
        Bounds currentLadderBounds = currentLadder.GetComponent<Collider2D>().bounds;
        ladderBounds = new Bounds( new Vector3(currentLadderBounds.center.x, ladderBounds.center.y, ladderBounds.center.z), ladderBounds.size);
        

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.y = climbSpeed * moveInput.y;
        localVelocity.x = 0;
        Vector3 worldVelocity = transform.TransformDirection(localVelocity);
        rb.velocity = worldVelocity;
        
        transform.position = new Vector3(transform.position.x,
                                         Mathf.Clamp(transform.position.y, ladderBounds.min.y + .3f, ladderBounds.max.y - .3f), 
            transform.position.z);
        if (moveInput.x != 0 || jetpackInputHeld)
        {
            SwitchOffLadder();
        }
    }

    public void SetFuel(float value)
    {
        currentFuel = value;
        GameManager.Instance.AudioManager.Play("JetpackRefuel");
    }

    #endregion

    #region Input
    public void LinkPlayerInput(PlayerInput newInput)
    {
        playerInputComponent = newInput;
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
            case "Jetpack": OnJetpack(ctx);  break;
            case "Control Steering": OnControlSteering(ctx); break;
            case "Interact": OnInteract(ctx); break;
            case "Cancel": OnCancel(ctx); break;
            case "Repair": OnRepair(ctx); break;
        }
    }

    private void OnDebugInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "Move": OnMove(ctx); break;
            case "1": OnJetpack(ctx); break;
            case "Control Steering": OnControlSteering(ctx); break;
            case "2": OnInteract(ctx); break;
            case "3": OnRepair(ctx); break;
            case "4": OnCancel(ctx); break;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        
        if (ctx.started && moveInput.y > 0)
        {
            SetLadder();
        }
    }

    public void OnJetpack(InputAction.CallbackContext ctx)
    {
        jetpackInputHeld = ctx.ReadValue<float>() > 0;
        if (ctx.ReadValue<float>() > 0 && currentState != CharacterState.OPERATING) GameManager.Instance.AudioManager.Play("JetpackStartup");
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (currentZone != null)
            {
                currentZone.Interact(this.gameObject);
            }

            if (currentInteractable != null && isOperator)
            {
                currentInteractable.Use();
            }
        }
    }

    public void OnCancel(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Exit(true);
            }
        }
    }

    public void OnControlSteering(InputAction.CallbackContext ctx)
    {
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
        if (ctx.started)
        {
            if (currentInteractable != null) currentInteractable.SecondaryUse(true);
        }

        if (ctx.canceled)
        {
            if (currentInteractable != null) currentInteractable.SecondaryUse(false);
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

    #endregion

    #region Character Functions

    protected override void OnCharacterDeath()
    {
        //TODO: implement something to happen upon the player dying
    }

    #endregion
}