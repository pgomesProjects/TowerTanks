using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character))]
// This floats the character collider over floor by a very small amount, which means the
// character won't slip and also won't trip over small changes in terrain collision
public class CharacterLegFloater : MonoBehaviour
{ 
    [SerializeField] Rigidbody2D rb;
    [SerializeField] private float rayLength;
    public float standingRideHeight;
    [SerializeField] private float rideSpringStrength;
    [SerializeField] private float rideSpringDamper;
    [SerializeField] private Transform raycastOrigin;
    
    private LayerMask currentDetectLayers;
    private RaycastHit2D rideHit;

    private PlayerMovement thisPlayer;
    private bool disabled = false;

    private Vector2 prevPosition;
    
    


    private void Start()
    {
        currentDetectLayers = (1 << LayerMask.NameToLayer("Ground")) 
                              | (1 << LayerMask.NameToLayer("Coupler"));
        thisPlayer = GetComponent<PlayerMovement>();
        prevPosition = rb.position;
    }
    

    private void FixedUpdate()
    {
        //Debug.Log("vel: " +rb.velocity);
        
        
        Vector2 currentPosition = rb.position;
        
        if (disabled) return;
        if (thisPlayer.jetpackInputHeld || rb.velocity.y > 2) return; //conditions to not float player collider
        Vector2 localVel = transform.InverseTransformDirection(rb.velocity);                  //jetpackinputheld is probably deprecated sometime soon
        
        
        Vector2 downDir = Vector2.down;

        rideHit = Physics2D.Raycast(raycastOrigin.position, downDir, rayLength, currentDetectLayers);

        
        if (rideHit.collider != null)
        {
            Vector2 vel = rb.velocity;
            Vector2 rayDir = Vector2.down;

            float rayDirVel = Vector2.Dot(downDir, vel);
            //dot product is a measure of how much of vel is in the direction of rayDir. If the player is moving
            //locally downward (gravity), rayDirVel will be a large positive number. If it's moving locally upwards,
            //it will be a large negative number. this pushes and pulls the player to the proper ride height.

            float x = (rideHit.distance - standingRideHeight); 

            float springForce = (x * rideSpringStrength) - (rayDirVel * rideSpringDamper);

            rb.AddForce(rayDir * springForce);  
        }
        

        prevPosition = rb.position;
    }

    public void SetRideHeight(float newRideHeight)
    {
        standingRideHeight = newRideHeight;
    }
    
    public void DisableFloater(bool disable)
    {
        disabled = disable;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(raycastOrigin.position, Vector3.down * rayLength);
        
    }
}
