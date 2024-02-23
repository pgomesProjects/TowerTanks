using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : Character
{
    #region Fields and Properties

    //input
    private Vector2 moveInput;
    private bool jetpackInputHeld;
    private PlayerInput playerInputComponent;

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

        if (jetpackInputHeld && currentFuel > 0) PropelJetpack();

        vel = rb.velocity.y;
        vel = Mathf.Clamp(vel, minYVelocity, maxYVelocity);
        rb.velocity = new Vector2(rb.velocity.x, vel);
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
        rb.velocity = new Vector3(moveSpeed * moveInput.x, rb.velocity.y); // Rigid movement system, we wanna keep this simple.
    }

    protected override void ClimbLadder()
    {
        base.ClimbLadder();
        Vector2 targetPosition = rb.position + new Vector2(0, climbSpeed * moveInput.y * Time.fixedDeltaTime);
        targetPosition = new Vector2(targetPosition.x, Mathf.Clamp(targetPosition.y, ladderBounds.min.y + transform.localScale.y / 2, ladderBounds.max.y - transform.localScale.y / 2));

        rb.MovePosition(targetPosition);

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