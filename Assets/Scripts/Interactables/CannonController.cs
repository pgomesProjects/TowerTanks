using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonController : InteractableController
{
    public enum CANNONDIRECTION { LEFT, RIGHT, UP, DOWN };

    [SerializeField] private int maxShells;
    [SerializeField] private float cannonSpeed;
    [SerializeField] private float lowerAngleBound, upperAngleBound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private float cannonForce = 2000;
    [SerializeField] private CANNONDIRECTION currentCannonDirection;
    private InteractableController cannonInteractable;
    [SerializeField] private Transform cannonPivot;
    private Vector3 cannonRotation;

    private int currentAmmo;

    // Start is called before the first frame update
    void Start()
    {
        cannonInteractable = GetComponentInParent<InteractableController>();
        currentAmmo = 0;
    }

    /// <summary>
    /// Checks to see if the player can load ammo into the cannon
    /// </summary>
    public void CheckForReload()
    {
        //If there is a player
        if (currentPlayer != null)
        {
            //If the cannon can fit ammo
            if(currentAmmo < maxShells)
            {
                //If the player has an item
                if (currentPlayer.GetPlayerItem() != null)
                {
                    //Unlock the player interaction (assumes the interactable controller is the grandparent)
                    LockPlayer(false);

                    //If the player's item is a shell
                    if (currentPlayer.GetPlayerItem().CompareTag("Shell"))
                    {
                        LoadShell();
                    }
                }
            }
        }
    }

    private void LoadShell()
    {
        //Get rid of the player's item and load
        currentPlayer.DestroyItem();

        //Play sound effect
        FindObjectOfType<AudioManager>().PlayOneShot("CannonReload", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        currentAmmo++;
    }

    public void CheckForCannonFire()
    {
        //If there is ammo
        if(currentAmmo > 0 || maxShells == 0)
        {
            //Fire the cannon
            Fire();

            //Shake the camera
            CameraEventController.instance.ShakeCamera(5f, 0.1f);
        }
    }

    public void Fire()
    {
        //Spawn projectile at cannon spawn point
        GameObject currentProjectile = Instantiate(projectile, new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0), Quaternion.identity);
        currentAmmo--;

        //Play sound effect
        FindObjectOfType<AudioManager>().PlayOneShot("CannonFire", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

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
        currentProjectile.AddComponent<DamageObject>().damage = 25;
        DamageObject currentDamager = currentProjectile.GetComponent<DamageObject>();
        currentDamager.StartRotation(startingShellRot);

        //Shoot the project at the current angle and direction
        currentProjectile.GetComponent<Rigidbody2D>().AddForce(direction * cannonForce);
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

    private void Update()
    {
        //If the cannon is linked to an interactable
        if (cannonInteractable != null)
        {
            if (cannonInteractable.CanInteract())
            {
                //Debug.Log("Cannon Movement: " + cannonInteractable.GetCurrentPlayer().GetCannonMovement());

                cannonRotation += new Vector3(0, 0, (cannonInteractable.GetCurrentPlayer().GetCannonMovement() / 100) * cannonSpeed);

                //Make sure the cannon stays within the lower and upper bound
                cannonRotation.z = Mathf.Clamp(cannonRotation.z, lowerAngleBound, upperAngleBound);

                //Rotate cannon
                cannonPivot.eulerAngles = cannonRotation;
            }
        }
    }

    public void SetCannonDirection(CANNONDIRECTION direction)
    {
        currentCannonDirection = direction;
    }

    public void CannonLookAt(Vector3 target)
    {
        // Get angle in Radians
        float cannonAngleRad = Mathf.Atan2(target.y - cannonPivot.transform.position.y, target.x - cannonPivot.transform.position.x);
        // Get angle in Degrees
        float cannonAngleDeg = (180 / Mathf.PI) * cannonAngleRad;

        switch (currentCannonDirection)
        {
            //Flip the cannon if facing left
            case CANNONDIRECTION.LEFT:
                cannonAngleDeg += 180;
                break;
        }

        Debug.Log("Cannon Degree: " + cannonAngleDeg);

        Quaternion lookAngle = Quaternion.Euler(0, 0, cannonAngleDeg);
        Quaternion currentAngle = Quaternion.Slerp(cannonPivot.transform.rotation, lookAngle, Time.deltaTime);

        //Clamp the angle of the cannon
        currentAngle.z = Mathf.Clamp(currentAngle.z, lowerAngleBound, upperAngleBound);

        // Rotate Object
        cannonPivot.transform.rotation = currentAngle;
    }
}
