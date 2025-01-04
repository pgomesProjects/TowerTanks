using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class RandomForce : MonoBehaviour
    {
        public Rigidbody2D rb;
        float randomRotation;
        public float lifeTime = 4f;
        float shrinkTime = 1f;
        public float xForceMultiplier = 1f;
        public float yForceMultiplier = 1f;

        public bool bounceExpire;
        public bool bounceParticle; //spawn a particle when bouncing?
        public int bounceParticleId;
        public bool expireParticle; //spawn a particle when expiring?
        public int expireParticleId;
        public int bounces; 

        // Start is called before the first frame update
        void Start()
        {

            lifeTime += Random.Range(-0.5f, 2.5f);

            randomRotation = Random.Range(-3f, 3f);
            float randomS = Random.Range(0.9f, 1.4f);
            float randomX = Random.Range(-7f, 7f);
            if (randomX <= -2f) randomX -= 1f;
            if (randomX > 2f) randomX += 1f;
            float randomY = Random.Range(4f, 10f);
            if (randomX < 2f && randomX > -2f) randomY += 8f;
            transform.localScale *= randomS;
            rb.AddForce(new Vector2(randomX * xForceMultiplier, randomY * yForceMultiplier), ForceMode2D.Impulse);
            rb.AddTorque(randomRotation);
        }

        // Update is called once per frame
        void Update()
        {
            //transform.Rotate(0f, 0f, randomRotation);

            lifeTime -= Time.deltaTime;
            if (lifeTime < shrinkTime)
            {
                float rate = 0.8f * Time.deltaTime;
                transform.localScale -= new Vector3(rate, rate, 0);
                if (transform.localScale.x < 0) transform.localScale = new Vector3(0, 0, 0);
            }
            if (lifeTime <= 0) Destroy(gameObject);
        }

        public void OnCollisionEnter2D(Collision2D collision)
        {
            if (bounceExpire)
            {
                bounces -= 1;
                if (bounces <= 0)
                {
                    if (expireParticle) GameManager.Instance.ParticleSpawner.SpawnParticle(expireParticleId, transform.position, 0.5f, null);
                    Destroy(gameObject);
                }

                if (bounceParticle)
                {
                    Vector2 current = transform.position;
                    Vector2 spawnPoint = collision.contacts[0].point;
                    Vector2 direction = (spawnPoint - current).normalized;
                    
                    GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(bounceParticleId, spawnPoint, 0.2f, null);
                    particle.transform.rotation = Quaternion.FromToRotation(Vector2.up, direction) * particle.transform.rotation;
                }
            }
        }
    }
}
