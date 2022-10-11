using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellItemBehavior : MonoBehaviour
{
    [SerializeField] private float chanceToExplode = 80;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.collider.tag == "Layer")
        {
            float generateChance = Random.Range(0, 100);
            if(generateChance < chanceToExplode)
            {
                Debug.Log("BOOM! Shell has exploded in tank.");
                Destroy(gameObject);
            }
        }
    }
}
