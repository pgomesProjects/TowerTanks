using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonController : MonoBehaviour
{
    [SerializeField] private float cannonSpeed;
    [SerializeField] private float lowerAngleBound, upperAngleBound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject spawnPoint;
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
        GameObject currentProjectile = Instantiate(projectile, new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0), Quaternion.identity);
        currentProjectile.GetComponent<Rigidbody2D>().AddForce(Vector2.right * 2000);

        //Add a damage component to the projectile
        currentProjectile.AddComponent<DamageObject>().damage = 25;
    }

    private void Update()
    {
        if (cannonInteractable.CanInteract())
        {
            cannonRotation += new Vector3(0, 0, cannonInteractable.GetCurrentPlayer().GetCannonMovement() * cannonSpeed);

            //Make sure the cannon stays within the lower and upper bound
            if (cannonRotation.z < lowerAngleBound)
                cannonRotation.z = lowerAngleBound;

            if(cannonRotation.z > upperAngleBound)
                cannonRotation.z = upperAngleBound;

            //Rotate cannon
            cannonPivot.eulerAngles = cannonRotation;
        }
    }

}
