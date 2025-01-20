using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class DummyObject : MonoBehaviour
    {
        public Transform corpse; //corpse transform associated with this object

        private bool hasTouchedGround = false;

        public Vector2 centerPoint;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground")) //if it's labeled as Ground on the dummy layer
            {
                if (!hasTouchedGround)
                {
                    hasTouchedGround = true;

                    //Spawn particle relative to impact point
                    Vector2 current = centerPoint;
                    Vector2 spawnPoint = collision.contacts[0].point;
                    Vector2 direction = (spawnPoint - current).normalized;

                    GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(19, spawnPoint, 0.15f, null);
                    particle.transform.rotation = Quaternion.FromToRotation(Vector2.right, direction) * particle.transform.rotation;
                }
            }
        }
    }
}
