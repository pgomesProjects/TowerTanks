using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollisionEvent : MonoBehaviour
{
    public void OnParticleCollision(GameObject other)
    {
        Cell cell = other.GetComponentInParent<Cell>();

        if (cell != null)
        {
            //Debug.Log("Particle hit!");

            if (cell.isOnFire)
            {
                float random = Random.Range(0, 100f);
                if (random <= 3f)
                {
                    cell.Extinguish();
                }
            }
        }

        Character character = other.GetComponent<Character>();

        if (character != null)
        {
            //Debug.Log("Particle hit!");

            if (character.isOnFire)
            {
                float random = Random.Range(0, 100f);
                if (random <= 3f)
                {
                    character.Extinguish();
                }
            }
        }
    }
}
