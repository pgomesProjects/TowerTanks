using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class DestructibleObject : MonoBehaviour, IDamageable
    {
        public bool isObstacle; //set true if this object is meant to trigger collision events on tanks
        public float maxHealth;
        public float health;
        private SpriteMask[] damageMasks;
        public float collisionResistance; //multiplier to damage from collision-based damage sources
        public Transform[] particleSpot;
        public float particleScale;
        public Transform[] segments;
        public float segmentMass;
        public int dummyMatIndex;
        private Vector2 impactDirection;
        private Vector2 impactPoint;

        public void Awake()
        {
            health = maxHealth;
            damageMasks = GetComponentsInChildren<SpriteMask>();
            impactDirection = Vector2.down;
            impactPoint = transform.position;
            
            if (damageMasks.Length > 0)
            {
                foreach (SpriteMask mask in damageMasks)
                {
                    mask.enabled = false;
                }
            }
        }
        public float Damage(Projectile projectile, Vector2 position)
        {
            //Handle initial impact:
            Vector2 relativeVelocity = projectile.velocity; //Get difference in velocity between projectile and object at point of impact

            Vector2 impactForce = projectile.hitProperties.mass * relativeVelocity;     //Get impact force as result of mass times (relative) velocity

            //Handle extra slam force:
            Vector2 slamImpactForce = relativeVelocity * projectile.hitProperties.slamForce; //Get additional impact force used to push tanks around (for gameplay reasons)
            ApplyImpactDirection(slamImpactForce, position);

            Damage(projectile.remainingDamage);
            return 0;
        }

        public void Damage(float damage, bool triggerHitEffects = false)
        {
            float tempHealth = health;
            float healthLost = 0;

            health -= damage;
            if (health < 0) health = 0;
            healthLost = tempHealth - health;

            bool becomeDummy = false;

            if (health <= 0)
            {
                Projectile projectile = GetComponent<Projectile>();
                if (projectile != null)
                {
                    if (projectile.type == Projectile.ProjectileType.OTHER)
                    {
                        projectile.remainingDamage = 0;
                        projectile.Hit(null);
                    }
                }
                else
                {
                    if (this.gameObject.layer != LayerMask.NameToLayer("Dummy"))
                    {
                        GameManager.Instance.ParticleSpawner.SpawnParticle((int)Random.Range(0, 3), particleSpot[0].position, particleScale);
                        GameManager.Instance.AudioManager.Play("MedExplosionSFX", this.gameObject);
                        Destroy(gameObject);
                        //becomeDummy = true;
                    }
                }
            }

            EvaluateDamage();
            if (becomeDummy)
            {
                //MakeDummy(); (TODO) This isn't really working correctly - needs more work
                //ApplyForces(impactDirection, impactPoint);
            }
        }

        private void EvaluateDamage()
        {
            float damageThresholdSegment = maxHealth / damageMasks.Length; //67
            for (int i = 0; i < damageMasks.Length; i++)
            {
                if ((i + 1) * Mathf.RoundToInt(damageThresholdSegment) > health)
                {
                    damageMasks[i].enabled = true;
                }
            }
        }

        public void MakeDummy()
        {
            string temp = this.gameObject.name;
            this.gameObject.name = "Object Corpse (" + temp + ")";
            this.gameObject.layer = LayerMask.NameToLayer("Dummy");
            PhysicsMaterial2D dummyMat = Resources.Load<RoomData>("RoomData").dummyMaterials[dummyMatIndex];

            //Conversions
            Transform[] children = transform.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Dummy");
                if (child.name == "Visuals") child.gameObject.SetActive(false); //Disable normal visuals
            }

            bool disabledSegment = false;

            //Segments
            foreach (Transform segment in segments)
            {
                segment.gameObject.SetActive(true); //Enable segmented visuals

                segment.gameObject.layer = LayerMask.NameToLayer("Dummy");

                //Add Rigidbody to Segment
                Rigidbody2D rb = segment.gameObject.AddComponent<Rigidbody2D>();
                rb.sharedMaterial = dummyMat;
                rb.mass = segmentMass;

                //Randomly disable 1 segment for variation purposes
                int random = Random.Range(0, 100);
                if (random <= 50 && !disabledSegment)
                {
                    segment.gameObject.SetActive(false);
                    disabledSegment = true;
                }
            }

            //Disable this script
            this.enabled = false;
        }

        public void ApplyImpactDirection(Vector2 direction, Vector2 point)
        {
            impactDirection = direction;

            impactPoint = point;
        }

        public void ApplyForces(Vector2 direction, Vector2 point)
        {
            foreach (Transform segment in segments)
            {
                Rigidbody2D rb = segment.gameObject.GetComponent<Rigidbody2D>();

                //Calculate Direction based on relative position
                //Vector2 _direction = segment.transform.position - transform.position;
                //direction += _direction * 0.3f;
                Vector2 force = (0.4f * collisionResistance) * direction;

                //Calculate Torque
                int sign = 1;
                float randomSign = Random.Range(0, 1f);
                if (randomSign < 0.5f) sign = -1;

                float randomRotation = Random.Range(5f, 10f);
                randomRotation *= sign;

                //Launch the Object
                rb.AddForceAtPosition(force, point, ForceMode2D.Impulse); //Apply force to rigidbody
                //rb.AddTorque(randomRotation, ForceMode2D.Impulse);
            }
        }
    }
}
