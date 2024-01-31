using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region Fields and Properties

    enum PlayerState { CLIMBING, NONCLIMBING }; //Simple state system, in the future this will probably be refactored
    PlayerState currentState;                   //to an FSM.

    //Components
    private Rigidbody2D rb;
    private ConstantForce2D extraGravity;
    private GameObject currentLadder;
    private Bounds ladderBounds;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float extraGravityForce;
    [SerializeField] private float climbSpeed;
    [Range(0, 2)]
    [SerializeField] private float groundedBoxX, groundedBoxY;

    [SerializeField] private float maxYVelocity, minYVelocity;

    [Header("Jetpack values")] 
    [SerializeField] private float maxFuel;
    [SerializeField] private float fuelDepletionRate;
    [SerializeField] private float fuelRegenerationRate;
    private float currentFuel;

    //temp
    private float moveSpeedHalved; // once we have a state machine for the player, we wont need these silly fields.
    private float currentMoveSpeed; // this is fine for the sake of prototyping though.

    //input
    private Vector2 moveInput;
    private bool jetpackInputHeld;
    
    [SerializeField] private PlayerInput playerInputComponent;
    private int playerIndex;
    InputActionMap inputMap;
    private PlayerHUD playerHUD;
    private float vel;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        extraGravity = GetComponent<ConstantForce2D>();
    }

    private void Start()
    {
        currentFuel = maxFuel;
        if (playerInputComponent != null) LinkPlayerInput(playerInputComponent); //if it is null, it will be set by multiplayer manager. can be set in inspector as an override for testing
        currentState = PlayerState.NONCLIMBING;
        moveSpeedHalved = moveSpeed / 2;
        SetExtraGravityAmount(extraGravityForce);
    }

    private void FixedUpdate()
    {
        if (currentState == PlayerState.NONCLIMBING) MoveLeftOrRight();

        else if (currentState == PlayerState.CLIMBING) ClimbLadder();

        if (jetpackInputHeld && currentFuel > 0) PropelJetpack();

        vel = rb.velocity.y;
        vel = Mathf.Clamp(vel, minYVelocity, maxYVelocity);
        rb.velocity = new Vector2(rb.velocity.x, vel);
    }

    private void Update()
    {
        if (jetpackInputHeld)
        {
            currentFuel -= fuelDepletionRate * Time.deltaTime;
        } else if (CheckGround())
        {
            currentFuel += fuelRegenerationRate * Time.deltaTime;
        }
        
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
        //Debug.Log($"Current Fuel: {currentFuel}");
    }

    private void OnDrawGizmos()
    {
        //visualizes the grounded box for debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector2(transform.position.x, transform.position.y - transform.localScale.y / 2), new Vector3(groundedBoxX, groundedBoxY, 0));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            currentLadder = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            currentLadder = null;
        }
    }
    

    #endregion

    #region Movement

    private bool CheckGround()
    {
        LayerMask groundLayer = (1 << LayerMask.NameToLayer("Ground"));
        return Physics2D.OverlapBox(new Vector2(transform.position.x,
                                                     transform.position.y - transform.localScale.y / 2),
                                                   new Vector2(1, 1),
                                                   0f,
                                                   groundLayer);
    }

    private void MoveLeftOrRight()
    {
        rb.velocity = new Vector3(moveSpeed * moveInput.x, rb.velocity.y); // Rigid movement system, we wanna keep this simple.
    }

    private void PropelJetpack()
    {
        rb.AddForce(Vector2.up * (jumpForce * Time.fixedDeltaTime));
    }

    private void SetLadder()
    {
        if (currentLadder != null)
        {
            currentState = PlayerState.CLIMBING;
            SetExtraGravityAmount(0);
            rb.bodyType = RigidbodyType2D.Kinematic;
            transform.position = new Vector2(currentLadder.transform.position.x, transform.position.y);
            ladderBounds = currentLadder.GetComponent<Collider2D>().bounds;
        }
        
    }

    private void ClimbLadder()
    {
        Vector2 targetPosition = rb.position + new Vector2(0, climbSpeed * moveInput.y * Time.fixedDeltaTime);
        targetPosition = new Vector2(targetPosition.x, Mathf.Clamp(targetPosition.y, ladderBounds.min.y + transform.localScale.y / 2, ladderBounds.max.y - transform.localScale.y / 2));

        rb.MovePosition(targetPosition);

        if (moveInput.x != 0 || jetpackInputHeld)
        {
            SwitchOffLadder();
        }
    }

    private void SwitchOffLadder()
    {
        currentState = PlayerState.NONCLIMBING;
        SetExtraGravityAmount(extraGravityForce);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
    }

    private void SetExtraGravityAmount(float extraGravityAmount)
    {
        extraGravity.force = new Vector2(0, -Mathf.Abs(extraGravityAmount));
    }
    #endregion

    #region Input
    public void LinkPlayerInput(PlayerInput newInput)
    {
        playerInputComponent = newInput;
        playerIndex = playerInputComponent.playerIndex;

        //Gets the player input action map so that events can be subscribed to it
        inputMap = playerInputComponent.actions.FindActionMap("Player");
        inputMap.actionTriggered += OnPlayerInput;

        //Subscribes events for control lost / regained
        playerInputComponent.onDeviceLost += OnDeviceLost;
        playerInputComponent.onDeviceRegained += OnDeviceRegained;
    }

    public void LinkPlayerHUD(PlayerHUD newHUD)
    {
        playerHUD = newHUD;
        playerHUD.InitializeHUD(GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerIndex]);
    }

    public void OnDeviceLost(PlayerInput playerInput)
    {
        Debug.Log("Player " + (playerIndex + 1) + " Controller Disconnected!");
        FindObjectOfType<CornerUIController>().OnDeviceLost(playerIndex);
    }

    public void OnDeviceRegained(PlayerInput playerInput)
    {
        Debug.Log("Player " + (playerIndex + 1) + " Controller Reconnected!");
        FindObjectOfType<CornerUIController>().OnDeviceRegained(playerIndex);
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
}