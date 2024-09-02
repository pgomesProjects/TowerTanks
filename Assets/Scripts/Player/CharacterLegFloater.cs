using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterLegFloater : MonoBehaviour
{ 
    [SerializeField] Rigidbody2D rb;
    [SerializeField] private float rayLength;
    public float standingRideHeight;
    [SerializeField] private float rideSpringStrength;
    [SerializeField] private float rideSpringDamper;
    [SerializeField] private Transform raycastOrigin;

    [SerializeField]
    private float distanceBetweenLeftStep, distanceBetweenRightStep;
    private LayerMask currentDetectLayers;
    private RaycastHit2D rideHit;

    private Vector2 rightFootHit, leftFootHit;
    private RaycastHit2D currentRightCastPoint, currentLeftCastPoint;

    private void Start()
    {
        SetStandOnPlatform(true);
        UpdateLegCasts();
    }

    private void Update()
    {
        UpdateLegCasts();
    }

    private void FixedUpdate()
    {
        Vector2 downDir = -transform.up;

        rideHit = Physics2D.Raycast(raycastOrigin.position, downDir, rayLength, currentDetectLayers);

        if (rideHit.collider != null)
        {
            Vector2 vel = rb.velocity;
            Vector2 rayDir = transform.TransformDirection(Vector2.down); 

            float rayDirVel = Vector2.Dot(rayDir, vel);
            //dot product is a measure of how much of vel is in the direction of rayDir. If the player is moving
            //locally downward (gravity), rayDirVel will be a large positive number. If it's moving locally upwards,
            //it will be a large negative number. this pushes and pulls the player to the proper ride height.

            float x = (rideHit.distance - standingRideHeight); 

            float springForce = (x * rideSpringStrength) - (rayDirVel * rideSpringDamper);

            rb.AddForce(rayDir * springForce);
        }
    }
    
    private void UpdateLegCasts()
    {
        currentRightCastPoint = Physics2D.Raycast(raycastOrigin.position + new Vector3(.14f, 0, 0), transform.TransformDirection(Vector3.down), rayLength, currentDetectLayers);
        currentLeftCastPoint = Physics2D.Raycast(raycastOrigin.position - new Vector3(.14f, 0, 0), transform.TransformDirection(Vector3.down), rayLength, currentDetectLayers);
    
        // Check and update right foot position independently
        if (Vector3.Distance(rightFootHit, currentRightCastPoint.point) > distanceBetweenRightStep)
        {
            rightFootHit = currentRightCastPoint.point;
        }

        // Check and update left foot position independently
        if (Vector3.Distance(leftFootHit, currentLeftCastPoint.point) > distanceBetweenLeftStep)
        {
            leftFootHit = currentLeftCastPoint.point;
        }
    }
    
    private void SetBothLegPoints()
    {
        rightFootHit = currentRightCastPoint.point;
        leftFootHit = currentLeftCastPoint.point;
    }

    public void SetRideHeight(float newRideHeight)
    {
        standingRideHeight = newRideHeight;
    }
    
    public void SetStandOnPlatform(bool stand)
    {
        if (!stand)
        {
            currentDetectLayers = LayerMask.NameToLayer("Ground");
        }
        else
        {
            currentDetectLayers = (1 << LayerMask.NameToLayer("Ground")) 
                                  | (1 << LayerMask.NameToLayer("Coupler"));
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(raycastOrigin.position, transform.TransformDirection(Vector3.down) * rayLength);
        Gizmos.DrawRay(raycastOrigin.position + new Vector3(.14f, 0, 0), transform.TransformDirection(Vector3.down) * rayLength);
        Gizmos.DrawRay(raycastOrigin.position + new Vector3(.14f, 0, 0), transform.TransformDirection(Vector3.down) * rayLength);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(rightFootHit, .1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(leftFootHit, .1f);
    }
}
