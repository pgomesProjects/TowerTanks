using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CANNONDIRECTION { LEFT, RIGHT, UP, DOWN };

public class CannonController : InteractableController
{
    [SerializeField, Tooltip("The projectile GameObject.")] protected GameObject projectile;
    [SerializeField, Tooltip("The cannon pivot position.")] protected Transform cannonPivot;
    [SerializeField, Tooltip("The projectile spawn point position.")] protected GameObject spawnPoint;

    [SerializeField, Tooltip("The layer(s) that the cannon can detect.")] protected LayerMask layerToHit;

    [SerializeField, Tooltip("The speed of the cannon's rotation.")] protected float cannonSpeed;
    [SerializeField, Tooltip("The lower and upper angle limitations for the cannon.")] protected float lowerAngleBound, upperAngleBound;

    [SerializeField, Tooltip("The direction that the cannon is facing.")] protected CANNONDIRECTION currentCannonDirection;
    [SerializeField, Tooltip("The range that the cannon can shoot at.")] protected float range = 25f;

    protected Vector3 initialVelocity;  //The initial velocity of the project
    protected Vector3 cannonRotation;   //The current rotation for the cannon

    [SerializeField, Tooltip("The particle effect for the smoke that appears when the cannon is fired.")] protected GameObject cSmoke;

    public void Fire()
    {
        //Spawn projectile at cannon spawn point
        if (spawnPoint != null)
        {
            GameObject currentProjectile = Instantiate(projectile, new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0), Quaternion.identity);

            //Play sound effect
            FindObjectOfType<AudioManager>().PlayOneShot("CannonFire", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

            Instantiate(cSmoke, spawnPoint.transform.position, Quaternion.identity);

            //Determine the direction of the cannon
            Vector2 direction = Vector2.zero;
            float startingShellRot = 0;

            switch (currentCannonDirection)
            {
                case CANNONDIRECTION.LEFT:
                    direction = -GetCannonVectorHorizontal(cannonRotation.z * Mathf.Deg2Rad);
                    startingShellRot = -cannonPivot.eulerAngles.z + 90;
                    break;
                case CANNONDIRECTION.RIGHT:
                    direction = GetCannonVectorHorizontal(cannonRotation.z * Mathf.Deg2Rad);
                    startingShellRot = -cannonPivot.eulerAngles.z - 90;
                    break;
                case CANNONDIRECTION.UP:
                    direction = GetCannonVectorVertical(cannonRotation.z * Mathf.Deg2Rad);
                    break;
                case CANNONDIRECTION.DOWN:
                    direction = -GetCannonVectorVertical(cannonRotation.z * Mathf.Deg2Rad);
                    break;
            }

            //Add a damage component to the projectile
            currentProjectile.AddComponent<DamageObject>().damage = currentProjectile.GetComponent<ShellItemBehavior>().GetDamage();
            DamageObject currentDamager = currentProjectile.GetComponent<DamageObject>();
            //currentDamager.StartRotation(startingShellRot);

            Debug.Log("Direction: " + direction);

            UpdateInitialVelocity();

            //Shoot the project at the current angle and direction
            currentProjectile.GetComponent<Rigidbody2D>().AddForce(initialVelocity, ForceMode2D.Impulse);
        }
    }

    private Vector2 GetCannonVectorHorizontal(float radians)
    {
        //Trigonometric function to get the vector (hypotenuse) of the current cannon angle
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private Vector2 GetCannonVectorVertical(float radians)
    {
        //Trigonometric function to get the vector (hypotenuse) of the current cannon angle
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    public void SetCannonDirection(CANNONDIRECTION direction)
    {
        currentCannonDirection = direction;
    }

    /// <summary>
    /// Gets the velocity of the cannon given the range that it has.
    /// </summary>
    protected void UpdateInitialVelocity()
    {
        Vector3 shootingRange = (spawnPoint.transform.rotation * spawnPoint.transform.localPosition) * range;
        initialVelocity = shootingRange - spawnPoint.transform.localPosition;
    }

    /// <summary>
    /// Checks to see if a specific tank layer is within the tank's current trajectory.
    /// </summary>
    /// <param name="tagName">The name of the layer's tag.</param>
    protected void CheckForTargetInTrajectory(string tagName)
    {
        UpdateInitialVelocity();

        float g = Physics2D.gravity.magnitude * projectile.GetComponent<Rigidbody2D>().gravityScale;

        float velocity = initialVelocity.magnitude;
        float angle = Mathf.Atan2(initialVelocity.y, initialVelocity.x);

        Vector3 start = spawnPoint.transform.position;
        Vector3 lastPos = start;
        float timeStep = 0.1f;
        float fTime = 0f;

        bool canHitTarget = false;

        //While the last position is higher than the ground and there is no target
        while (lastPos.y > -16.4f && !canHitTarget)
        {
            float dx = velocity * fTime * Mathf.Cos(angle);
            float dy = velocity * fTime * Mathf.Sin(angle) - (g * fTime * fTime / 2f);
            Vector3 pos = new Vector3(start.x + dx, start.y + dy, 0);

            var result = Physics2D.Linecast(lastPos, pos, layerToHit);

            if (result.collider != null)
            {
                if (result.collider.CompareTag(tagName))
                {
                    Debug.Log("Can Hit Target " + tagName + "!");
                    canHitTarget = true;
                }
            }

            Debug.DrawLine(lastPos, pos, Color.green, Time.deltaTime);

            lastPos = pos;
            fTime += timeStep;
        }

        if (!canHitTarget)
            Debug.Log("No Target Available.");
    }

    /// <summary>
    /// Checks to see if a position is within the cannon's expected trajectory.
    /// </summary>
    /// <param name="target">The target position to check for.</param>
    /// <param name="radius">The radius given to the target.</param>
    protected void CheckForTargetInTrajectory(Vector3 target, float radius)
    {
        UpdateInitialVelocity();

        float g = Physics2D.gravity.magnitude * projectile.GetComponent<Rigidbody2D>().gravityScale;

        float velocity = initialVelocity.magnitude;
        float angle = Mathf.Atan2(initialVelocity.y, initialVelocity.x);

        Vector3 start = spawnPoint.transform.position;
        Vector3 lastPos = start;
        float timeStep = 0.1f;
        float fTime = 0f;

        bool canHitTarget = false;

        //While the last position is higher than the ground and there is no target
        while (lastPos.y > -16.4f && !canHitTarget)
        {
            float dx = velocity * fTime * Mathf.Cos(angle);
            float dy = velocity * fTime * Mathf.Sin(angle) - (g * fTime * fTime / 2f);
            Vector3 pos = new Vector3(start.x + dx, start.y + dy, 0);

            if(Vector3.Distance(pos, target) < radius)
            {
                Debug.Log("Can Hit Target " + target + "!");
                canHitTarget = true;
            }

            Debug.DrawLine(lastPos, pos, Color.green, Time.deltaTime);

            lastPos = pos;
            fTime += timeStep;
        }

        if (!canHitTarget)
            Debug.Log("No Target Available.");
    }
}
