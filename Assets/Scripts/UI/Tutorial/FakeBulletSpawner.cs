using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeBulletSpawner : MonoBehaviour
{
    [SerializeField] private GameObject fakeBullet;
    [SerializeField] private CANNONDIRECTION currentCannonDirection;
    [SerializeField, Range(0, 360)] private float cannonAngle;
    [SerializeField] private float cannonForce = 8000;
    private Vector2 direction;

    public void SpawnFakeBullet()
    {
        //Spawn projectile at cannon spawn point
        GameObject currentProjectile = Instantiate(fakeBullet, new Vector3(transform.position.x, transform.position.y, 0), Quaternion.identity);

        //Play sound effect
        FindObjectOfType<AudioManager>().PlayOneShot("CannonFire", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        //Determine the direction of the cannon
        float startingShellRot = 0;

        switch (currentCannonDirection)
        {
            case CANNONDIRECTION.LEFT:
                direction = -GetCannonVectorHorizontal(cannonAngle * Mathf.Deg2Rad);
                startingShellRot = -transform.eulerAngles.z + 90;
                break;
            case CANNONDIRECTION.RIGHT:
                direction = GetCannonVectorHorizontal(cannonAngle * Mathf.Deg2Rad);
                startingShellRot = -transform.eulerAngles.z - 90;
                break;
            case CANNONDIRECTION.UP:
                direction = GetCannonVectorVertical(cannonAngle * Mathf.Deg2Rad);
                break;
            case CANNONDIRECTION.DOWN:
                direction = -GetCannonVectorVertical(cannonAngle * Mathf.Deg2Rad);
                break;
        }

        //Add a damage component to the projectile
        currentProjectile.AddComponent<DamageObject>().damage = currentProjectile.GetComponent<ShellItemBehavior>().GetDamage();
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
}
