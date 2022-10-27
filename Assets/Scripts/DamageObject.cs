using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageObject : MonoBehaviour
{

    internal int damage;

    private Rigidbody2D rb;
    private float prevZRot;
    private float currentZRot;

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void StartRotation(float startingRot)
    {
        transform.rotation = Quaternion.Euler(0, 0, startingRot);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Layer" || collision.collider.tag == "EnemyLayer")
        {
            //Deal damage and destroy self if colliding with a layer
            collision.collider.GetComponentInParent<LayerHealthManager>().DealDamage(damage);

            //Check to see if current object is a shell. If so, check to see if the layer will catch fire
            if (TryGetComponent<ShellItemBehavior>(out ShellItemBehavior shell) && collision.collider.GetComponentInParent<LayerHealthManager>() != null)
            {
                collision.collider.GetComponentInParent<LayerHealthManager>().CheckForFireSpawn(shell.GetChanceToCatchFire());
            }

            Destroy(gameObject);
        }
    }


    private void Update()
    {
        //If the item is passed the world's bounds, delete it
        if(transform.position.y < -10)
            Destroy(gameObject);

        currentZRot = (rb.velocity.x - rb.velocity.y) - 90;

        float newRot = -(currentZRot - prevZRot);

        Debug.Log("Prev Z Rot: " + prevZRot);
        Debug.Log("Current Z Rot: " + currentZRot);

        transform.rotation *= Quaternion.Euler(0, 0, newRot);

        prevZRot = currentZRot;
    }
}
