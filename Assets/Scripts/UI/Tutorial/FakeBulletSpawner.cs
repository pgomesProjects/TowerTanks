using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeBulletSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("The fake bullet GameObject.")] private GameObject fakeBullet;
    [SerializeField, Tooltip("The direction to fire the bullet at.")] private CANNONDIRECTION currentCannonDirection;
    [SerializeField, Tooltip("The angle of the imaginary cannon."), Range(0, 360)] private float cannonAngle;
    [SerializeField, Tooltip("The range of the imaginary cannon.")] private float range = 50;
    private Vector2 direction;
    private Vector3 spawnPoint;

    public void SpawnFakeBullet()
    {
        //Spawn projectile at cannon spawn point
        spawnPoint = new Vector3(transform.position.x, transform.position.y, 0);
        GameObject currentProjectile = Instantiate(fakeBullet, new Vector3(transform.position.x, transform.position.y, 0), Quaternion.identity);

        //Play sound effect
        GameManager.Instance.AudioManager.Play("CannonFire", gameObject);

        switch (currentCannonDirection)
        {
            case CANNONDIRECTION.LEFT:
                direction = -transform.right;
                break;
            case CANNONDIRECTION.RIGHT:
                direction = transform.right;
                break;
            case CANNONDIRECTION.UP:
                direction = transform.up;
                break;
            case CANNONDIRECTION.DOWN:
                direction = -transform.up;
                break;
        }

        //Add a damage component to the projectile
        currentProjectile.AddComponent<DamageObject>().damage = currentProjectile.GetComponent<ShellItemBehavior>().GetDamage();

        //Shoot the project at the current angle and direction
        currentProjectile.GetComponent<Rigidbody2D>().AddForce(GetInitialVelocity() * direction, ForceMode2D.Impulse);
    }

    private Vector3 GetInitialVelocity()
    {
        Vector3 shootingRange = (spawnPoint) * range;
        return shootingRange - spawnPoint;
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
