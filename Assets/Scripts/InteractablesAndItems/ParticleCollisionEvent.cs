using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class ParticleCollisionEvent : MonoBehaviour
    {
        public GameObject foamStick;

        public void OnParticleCollision(GameObject other)
        {
            Cell cell = other.gameObject.GetComponentInParent<Cell>();

            if (cell != null)
            {
                //Debug.Log("Particle hit!");
                //GameObject foam = Instantiate(foamStick, other.point, Quaternion.identity, cell.transform);

                if (cell.isOnFire)
                {
                    float random = Random.Range(0, 100f);
                    if (random <= 3f)
                    {
                        cell.Extinguish();
                    }
                }
            }

            Character character = other.gameObject.GetComponent<Character>();

            if (character != null)
            {
                //Debug.Log("Particle hit!");

                //GameObject foam = Instantiate(foamStick, other.GetContact(0).point, Quaternion.identity, character.transform);

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
}
