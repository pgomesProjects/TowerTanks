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
    private Vector2 moveInput;
    private bool jetpackInputHeld;
    
    [SerializeField] private Transform playerSprite;

    [SerializeField] private PlayerInput playerInputComponent;
    [SerializeField] private float deAcceleration;
    [SerializeField] private float maxSpeed;

    InputActionMap inputMap;
    private float vel;

    #endregion

    #region Unity Methods

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        if (GameSettings.debugMode)
        {
            playerInputComponent = FindObjectOfType<Debug_TankBuilder>()?.GetComponent<PlayerInput>();
            if (playerInputComponent != null) LinkPlayerInput(playerInputComponent); //if it is null, it will be set by multiplayer manager. can be set in inspector as an override for testing
        }
    }

    protected override void Update()
    {
        if (jetpackInputHeld)
        {
            currentFuel -= fuelDepletionRate * Time.deltaTime;
        }
        else if (CheckGround() || currentState == CharacterState.CLIMBING)
        {
            currentFuel += fuelRegenerationRate * Time.deltaTime;
        }
        
        base.Update();
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
        }
        else
        {
            currentLadder = null;
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

    #endregion

    #region Input
    public void LinkPlayerInput(PlayerInput newInput)
    {
        playerInputComponent = newInput;
        characterIndex = playerInputComponent.playerIndex;

        //Gets the player input action map so that events can be subscribed to it
        inputMap = playerInputComponent.actions.FindActionMap("Player");
        inputMap.actionTriggered += OnPlayerInput;

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
            case "Jump": OnJetpack(ctx);
                break;
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
    public void OnJetpack(InputAction.CallbackContext ctx) => jetpackInputHeld = ctx.ReadValue<float>() > 0;

    #endregion

    #region Character Functions

    protected override void OnCharacterDeath()
    {
        //TODO: implement something to happen upon the player dying
    }

    #endregion
}