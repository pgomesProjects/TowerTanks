using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    //Note: Input implementation is placeholder for testing, and will eventually be integrated with the
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
    [Range(0, 2)]
    [SerializeField] private float groundedBoxX, groundedBoxY;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _input = new PlayerControlSystem();
        extraGravity = GetComponent<ConstantForce2D>();
    }
    
    private void Update()
    {
        moveInput = _input.Player.Move.ReadValue<Vector2>();
        MoveLeftOrRight();
        if (!CheckGround())
        {
            extraGravity.force = new Vector2(0, -extraGravityForce);
        }
        else
        {
            extraGravity.force = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        MoveLeftOrRight();
    }

    private bool CheckGround()
    {
        LayerMask layerToIgnore = (1 << LayerMask.NameToLayer("Player"));
        layerToIgnore = ~layerToIgnore; //inverts the layermask to ignore the player layer
        return Physics2D.OverlapBox(new Vector2(transform.position.x, 
                                                     transform.position.y - transform.localScale.y / 2), 
                                                   new Vector2(1, 1), 
                                                   0f,
                                                   layerToIgnore);
    }
    
    
    private void MoveLeftOrRight()
    {
        rb.velocity = new Vector3(moveSpeed * moveInput.x, rb.velocity.y);
    }

    private void Jump()
    {
        if (CheckGround())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    private void OnDrawGizmos()
    {
        //visualizes the grounded box for debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector2(transform.position.x, transform.position.y - transform.localScale.y / 2), new Vector3(groundedBoxX, groundedBoxY, 0));
    }
    
    private void OnEnable()
    {
        _input.Enable();
        _input.Player.Jump.performed += context => Jump();
    }
    
    private void OnDisable()
    {
        _input.Disable();
        _input.Player.Jump.performed -= context => Jump();
    }
}
