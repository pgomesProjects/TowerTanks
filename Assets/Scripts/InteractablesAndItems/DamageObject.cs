using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
    public class DamageObject : MonoBehaviour
    {
        internal int damage;    //The number of damage points that the damage object has 

        private Rigidbody2D rb; //The rigidbody component
        private float lifeTimeSeconds = 10; //The amount of time the damage object should exist for (in seconds)
        private float currentTimer; //The current amount of time the damage object has existed for

        private void OnEnable()
        {
            rb = GetComponent<Rigidbody2D>();
            currentTimer = 0;

            //Spawn object with sound effect active
            GameManager.Instance.AudioManager.Play("ProjectileInAirSFX", gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //If the object has collided with a layer or an enemy layer
            if (collision.collider.tag == "Layer" || collision.collider.tag == "EnemyLayer")
            {
                //If there is a LayerHealthManager component
                if (collision.collider.GetComponentInParent<LayerManager>() != null)
                {
                    //Deal damage and destroy self if colliding with a layer
                    collision.collider?.GetComponentInParent<LayerManager>().DealDamage(damage, true);
                    GameManager.Instance.AudioManager.Play("MedExplosionSFX", gameObject);

                    //If there is a ShellItemBehavior component on the damage object, check to see if there should be a fire
                    if (TryGetComponent<ShellItemBehavior>(out ShellItemBehavior shell))
                    {
                        collision.collider?.GetComponentInParent<LayerManager>().CheckForFireSpawn(shell.GetChanceToCatchFire());
                    }
                }
            }
            //Just explode if the object hits anything else
            else
            {
                if (GameManager.Instance.AudioManager != null)
                    GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
            }

            //If the object hits the player, launch them in a specified direction
            if (collision.collider.tag == "Player")
            {
                //Hit from left
                if (transform.position.x < collision.collider.transform.position.x)
                {
                    collision.collider.GetComponent<Rigidbody2D>().AddForce(new Vector2(0.5f, 0.3f) * 5000);
                }

                //Hit from right
                else
                {
                    collision.collider.GetComponent<Rigidbody2D>().AddForce(new Vector2(-0.5f, 0.3f) * 5000);
                }
            }

            Destroy(gameObject);    //Destroy object after it collides with something
        }


        private void Update()
        {
            //If the item is passed the world's bounds, delete it
            if (transform.position.y < -14.4f)
            {
                if (GameManager.Instance.AudioManager != null)
                    GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
                Destroy(gameObject);
            }

            CheckLifetime();
        }

        /// <summary>
        /// Checks how long the object should exist for.
        /// </summary>
        private void CheckLifetime()
        {
            currentTimer += Time.deltaTime;
            if (currentTimer > lifeTimeSeconds)
            {
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            transform.right = rb.velocity.normalized;   //Rotate the transform in the direction of the velocity that it has
        }

        private void OnDestroy()
        {
            if (GameManager.Instance.AudioManager != null)
                GameManager.Instance.AudioManager.Stop("ProjectileInAirSFX", gameObject);
        }
    }
}
