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

    //Settings:
    [Header("Gun Settings:")]
    [Tooltip("Velocity of projectile upon exiting the barrel."), SerializeField, Min(0)]  private float muzzleVelocity;
    [Tooltip("Force exerted on tank each time weapon is fired."), SerializeField, Min(0)] private float recoil;
    [Tooltip("Max angle (up or down) weapon joint can be rotated to."), SerializeField]   private float gimbalRange;
    [Header("Debug Controls:")]
    public bool fire;
    [Range(0, 1)] public float moveGimbal = 0.5f;

    //Runtime Variables:
 
    //RUNTIME METHODS:
    private void Update()
    {
        //Debug settings:
        if (fire) { fire = false; Fire(); }
        pivot.localEulerAngles = Vector3.forward * (Mathf.Lerp(-gimbalRange, gimbalRange, moveGimbal));
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Fires the weapon, once.
    /// </summary>
    public void Fire()
    {
        if (tank == null) tank = GameObject.Find("Tank1").GetComponent<TankController>();

        //Apply recoil:
        Vector2 recoilForce = -barrel.right * recoil;                                  //Get force of recoil from direction of barrel and set magnitude
        tank.treadSystem.r.AddForceAtPosition(recoilForce, barrel.transform.position); //Apply recoil force at position of barrel

        //Other effects:
        int random = Random.Range(0, 2);
        GameManager.Instance.ParticleSpawner.SpawnParticle(random, particleSpots[0], 0.1f, null);
        GameManager.Instance.AudioManager.Play("CannonFire"); //Play firing audioclip
    }
}
