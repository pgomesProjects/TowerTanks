using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonController : MonoBehaviour
{
    public enum CANNONDIRECTION{LEFT, RIGHT, UP, DOWN};

    [SerializeField] private float cannonSpeed;
    [SerializeField] private float lowerAngleBound, upperAngleBound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private float cannonForce = 2000;
    [SerializeField] private CANNONDIRECTION currentCannonDirection;
    private InteractableController cannonInteractable;
    private Transform cannonPivot;
    private Vector3 cannonRotation; 

    // Start is called before the first frame update
    void Start()
    {
        cannonPivot = transform.parent;
        cannonInteractable = GetComponentInParent<InteractableController>();
    }

    public void Fire(){
        //Spawn projectile at cannon spawn point
        GameObject currentProjectile = Instantiate(projectile, new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0), Quaternion.identity);

        //Determine the direction of the cannon
        Vector2 direction = Vector2.zero;

        switch (currentCannonDirection)
        {
            case CANNONDIRECTION.LEFT:
                direction = -GetCannonVectorHorizontal(cannonRotation.z * Mathf.Deg2Rad);
                break;
            case CANNONDIRECTION.RIGHT:
                direction = GetCannonVectorHorizontal(cannonRotation.z * Mathf.Deg2Rad);
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
        currentProjectile.GetComponent<DamageObject>().StartRotation(-cannonPivot.eulerAngles.z - 90);

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
                cannonRotation += new Vector3(0, 0, cannonInteractable.GetCurrentPlayer().GetCannonMovement() * cannonSpeed);

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
