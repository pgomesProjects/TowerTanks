using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageObject : MonoBehaviour
{

    internal int damage;


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Layer" || collision.collider.tag == "EnemyLayer")
        {
            //Deal damage and destroy self if colliding with a layer
            collision.collider.GetComponentInParent<LayerHealthManager>().DealDamage(damage);
            Destroy(gameObject);
        }
    }


    private void Update()
    {
        //If the item is passed the world's bounds, delete it
        if(transform.position.y < -10)
            Destroy(gameObject);
    }
}
