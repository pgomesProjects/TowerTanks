using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    enum PlayerState { CLIMBING, NONCLIMBING }; //Simple state system, in the future this will probably be refactored
    PlayerState currentState;                   //to an FSM.
    
    //NOTE: Input implementation is placeholder for testing, and will later be integrated with the
    //MultiplayerManager input routing system.
    
    //Components
    private Rigidbody2D rb;
    private PlayerControlSystem _input;
    private ConstantForce2D extraGravity;
    
    [Header("Movement")]
    private Vector2 moveInput;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float extraGravityForce;
    [SerializeField] private float climbSpeed;
    [Range(0, 2)]
    [SerializeField] private float groundedBoxX, groundedBoxY;
    
    //privs
    private GameObject currentLadder;
    private Bounds ladderBounds;
    
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _input = new PlayerControlSystem();
        extraGravity = GetComponent<ConstantForce2D>();
    }

    private void Start()
    {
        currentState = PlayerState.NONCLIMBING;
        SetExtraGravityAmount(extraGravityForce);
    }

    private void Update()
    {
        moveInput = _input.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (currentState == PlayerState.NONCLIMBING) MoveLeftOrRight();

        else if (currentState == PlayerState.CLIMBING) ClimbLadder();
        
    }

    private bool CheckGround()
    {
        LayerMask groundLayer = (1 << LayerMask.NameToLayer("Ground"));
        return Physics2D.OverlapBox(new Vector2(transform.position.x, 
                                                     transform.position.y - transform.localScale.y / 2), 
                                                   new Vector2(1, 1), 
                                                   0f,
                                                   groundLayer);
    }
    
    /*Vector2 targetPosition = rb.position + new Vector2(moveSpeed * moveInput.x * Time.fixedDeltaTime, 0);
    rb.MovePosition(targetPosition);*/
    private void MoveLeftOrRight()
    {
        rb.velocity = new Vector3(moveSpeed * moveInput.x, rb.velocity.y); // Rigid movement system, we wanna keep this simple.
    }

    private void Jump()
    {
        if (CheckGround() || currentState == PlayerState.CLIMBING)
        {
            SwitchOffLadder();
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    private void OnDrawGizmos()
    {
        //visualizes the grounded box for debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector2(transform.position.x, transform.position.y - transform.localScale.y / 2), new Vector3(groundedBoxX, groundedBoxY, 0));
    }
    
    private void CheckLadder()
    {
        moveInput = _input.Player.Move.ReadValue<Vector2>();
        if (moveInput.y > 0)
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
    }
    private void ClimbLadder()
    {
        Vector2 targetPosition = rb.position + new Vector2(0, moveSpeed * moveInput.y * Time.fixedDeltaTime);
        targetPosition = new Vector2(targetPosition.x, Mathf.Clamp(targetPosition.y, ladderBounds.min.y + transform.localScale.y / 2, ladderBounds.max.y));
        
        rb.MovePosition(targetPosition);
        
        if (moveInput.x != 0)
        {
            SwitchOffLadder();
        }
    }
    
    private void SwitchOffLadder()
    {
        currentState = PlayerState.NONCLIMBING;
        SetExtraGravityAmount(extraGravityForce);
        rb.bodyType = RigidbodyType2D.Dynamic;
    }
    
    private void SetExtraGravityAmount(float extraGravityAmount)
    {
        extraGravity.force = new Vector2(0, -Mathf.Abs(extraGravityAmount));
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
            /*currentState = PlayerState.NONCLIMBING;
            SetExtraGravityAmount(extraGravityForce);
            rb.bodyType = RigidbodyType2D.Dynamic;*/
        }
    }
    private void OnEnable()
    {
        _input.Enable();
        _input.Player.Jump.performed += context => Jump();
        _input.Player.Move.performed += context => CheckLadder();
    }
    private void OnDisable()
    {
        _input.Disable();
        _input.Player.Jump.performed -= context => Jump();
        _input.Player.Move.performed -= context => CheckLadder();
    }
}
