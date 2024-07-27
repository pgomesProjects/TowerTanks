using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : TankInteractable
{
    //Objects & Components:
    [Tooltip("Default projectile which will be fired by this weapon"), SerializeField]                      private GameObject projectilePrefab;
    [Tooltip("Transform indicating direction and position in which projectiles are fired"), SerializeField] private Transform barrel;
    [Tooltip("Joint around which moving cannon assembly rotates."), SerializeField]                         private Transform pivot;
    [Tooltip("Transforms to spawn particles from when used."), SerializeField]                              private Transform[] particleSpots;
    [Tooltip("Scale particles are multiplied by when used by this weapon"), SerializeField]                 private float particleScale;

    //Settings:
    [Header("Gun Settings:")]
    [Tooltip("Velocity of projectile upon exiting the barrel."), SerializeField, Min(0)]  private float muzzleVelocity;
    [Tooltip("Force exerted on tank each time weapon is fired."), SerializeField, Min(0)] private float recoil;
    [Tooltip("Speed at which the cannon barrel rotates"), SerializeField]                 private float rotateSpeed;
    [Tooltip("Max angle (up or down) weapon joint can be rotated to."), SerializeField]   private float gimbalRange;
    [Tooltip("Cooldown in seconds between when the weapon can fire"), SerializeField, Min(0)] private float rateOfFire;
    private float fireCooldownTimer;
    [Tooltip("Radius of Degrees of the Cone of Fire for this weapon's projectiles"), SerializeField, Min(0)] private float spread;
    
    [Header("Debug Controls:")]
    public bool fire;
    [Range(0, 1)] public float moveGimbal = 0.5f;
    private Vector3 currentRotation = new Vector3(0, 0, 0);

    //Runtime Variables:
 
    //RUNTIME METHODS:
    private void Update()
    {
        //Debug settings:
        if (fire) { fire = false; Fire(); }
        
        pivot.localEulerAngles = currentRotation;

        //Cooldown
        if (fireCooldownTimer > 0)
        {
            fireCooldownTimer -= Time.deltaTime;
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Fires the weapon, once.
    /// </summary>
    public void Fire()
    {
        if (tank == null) tank = GetComponentInParent<TankController>();

        if (fireCooldownTimer <= 0)
        {
            //Adjust for Spread
            float randomSpread = Random.Range(-spread, spread);
            barrel.localEulerAngles += new Vector3(0, 0, randomSpread);

            //Fire projectile:
            Projectile newProjectile = Instantiate(projectilePrefab).GetComponent<Projectile>();
            newProjectile.Fire(barrel.position, barrel.right * muzzleVelocity);

            //Apply recoil:
            Vector2 recoilForce = -barrel.right * recoil;                                  //Get force of recoil from direction of barrel and set magnitude
            tank.treadSystem.r.AddForceAtPosition(recoilForce, barrel.transform.position); //Apply recoil force at position of barrel

            //Revert from Spread
            barrel.localEulerAngles = new Vector3(0, 0, 0);

            //Other effects:
            int random = Random.Range(0, 2);
            GameManager.Instance.ParticleSpawner.SpawnParticle(random, particleSpots[0].position, particleScale, null);
            GameManager.Instance.AudioManager.Play("CannonFire", gameObject);
            GameManager.Instance.AudioManager.Play("CannonThunk", gameObject); //Play firing audioclips

            //Set Cooldown
            fireCooldownTimer = rateOfFire;
        }
    }

    public void RotateBarrel(float force, bool withSound)
    {
        float speed = rotateSpeed * Time.deltaTime;

        currentRotation += new Vector3(0, 0, force * speed * direction);

        if (currentRotation.z > gimbalRange) currentRotation = new Vector3(0, 0, gimbalRange);
        if (currentRotation.z < -gimbalRange) currentRotation = new Vector3(0, 0, -gimbalRange);

        //Play SFX
        if (withSound)
        {
            if (force != 0)
            {
                if (!GameManager.Instance.AudioManager.IsPlaying("CannonRotate", gameObject)) GameManager.Instance.AudioManager.Play("CannonRotate", gameObject);
            }
            else if (GameManager.Instance.AudioManager.IsPlaying("CannonRotate", gameObject)) GameManager.Instance.AudioManager.Stop("CannonRotate", gameObject);
        }
    }
}
