using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonController : InteractableController
{
    public enum CANNONDIRECTION { LEFT, RIGHT, UP, DOWN };

    [SerializeField] private float cannonSpeed;
    [SerializeField] private float lowerAngleBound, upperAngleBound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private float cannonForce = 2000;
    [SerializeField] private CANNONDIRECTION currentCannonDirection;
    private InteractableController cannonInteractable;
    [SerializeField] private Transform cannonPivot;
    private Vector3 cannonRotation;

    // Start is called before the first frame update
    void Start()
    {
        cannonInteractable = GetComponentInParent<InteractableController>();
    }

    /// <summary>
    /// Fires a cannon in the direction of the enemies
    /// </summary>
    public void StartCannonFire()
    {
        //If there is a player
        if (currentPlayer != null)
        {
            //If the player has an item
            if (currentPlayer.GetPlayerItem() != null)
            {
                //Unlock the player interaction (assumes the interactable controller is the grandparent)
                LockPlayer(false);

                //If the player's item is a shell
                if (currentPlayer.GetPlayerItem().CompareTag("Shell"))
                {
                    //Get rid of the player's item and fire
                    Debug.Log("BAM! Weapon has been fired!");
                    currentPlayer.DestroyItem();
                    Fire();

                    //Shake the camera
                    CameraEventController.instance.ShakeCamera(5f, 0.1f);
                }
            }
        }
    }

    public void Fire()
    {
        //Spawn projectile at cannon spawn point
        GameObject currentProjectile = Instantiate(projectile, new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0), Quaternion.identity);

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
                if (cannonRotation.z < lowerAngleBound)
                    cannonRotation.z = lowerAngleBound;

                if (cannonRotation.z > upperAngleBound)
                    cannonRotation.z = upperAngleBound;

                //Rotate cannon
                cannonPivot.eulerAngles = cannonRotation;
            }
        }
    }

}
