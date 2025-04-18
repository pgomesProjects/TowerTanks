using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class Projectile : MonoBehaviour, IMagnetizable
    {
        //Classes, Enums & Structs:
        public enum ProjectileType { BULLET, SHELL, OTHER };

        //Objects & Components:
        private Transform smokeTrail;

        //Settings:
        [Tooltip("Defines broad projectile behavior.")]                             public ProjectileType type;
        [Tooltip("Defines the physics layers this projectile can hit.")]            public LayerMask layerMask;
        [Tooltip("Describes damage effect this projectile has on struck targets.")] public ProjectileHitProperties hitProperties;

        [Header("Travel Properties:")]
        [SerializeField, Tooltip("Maximum lifetime of this projectile. Set to -1 for infinite.")] public float maxLife; 
        [SerializeField, Tooltip("Radius of projectile collider.")]                public float radius;
        [SerializeField, Tooltip("Causes projectile to lose velocity over time.")] public float drag;
        [SerializeField, Tooltip("How fast this projectile falls.")]               public float gravity;
        
        [Header("Visual Properties:")]
        [Tooltip("Defines how large projectile trail is.")] public float particleScale;

        [Header("Other Properties:")]
        [SerializeField, Tooltip("Which faction this projectile belongs to")] public TankId.TankType factionId;
        bool reflected = false;

        //Runtime Variables:
        internal Vector2 velocity;                                                                                                                           //Speed and trajectory of projectile
        private float timeAlive;                                                                                                                             //Time this projectile has existed for
        [Tooltip("Damage projectile has left to deal (projectile is destroyed when this is reduced to zero during a hit).")] internal float remainingDamage; //Actual damage value which may be decreased by tunnelling effects
        private List<Collider2D> shieldsIgnored = new List<Collider2D>();
        private float shieldsIgnoreCooldownTimer = 0;
        public bool isIndoors = false;

        //RUNTIME METHODS:
        private void Awake()
        {
            smokeTrail = transform.Find("smokeTrail"); //Get reference to smoke trail object
            remainingDamage = hitProperties.damage;   //Get preset damage value (may be modified later)
        }

        private void Update()
        {
            velocity += gravity * Time.deltaTime * Vector2.down;
            //velocity -= drag * Time.deltaTime * new Vector2(transform.right.x, transform.right.y);

            //if (drag > 0 && Mathf.Abs(velocity.x) <= 1) Hit(null, true);

            //transform.rotation = Quaternion.AngleAxis(Vector3.Angle(Vector2.right, velocity), Vector3.back);

            CheckforCollision();

            if (maxLife > -1)
            {
                timeAlive += Time.deltaTime;
                if (timeAlive >= maxLife) Hit(null, true);
            }
        }

        private void FixedUpdate()
        {
            if (shieldsIgnored.Count > 0)
            {
                shieldsIgnoreCooldownTimer -= Time.fixedDeltaTime;
                if (shieldsIgnoreCooldownTimer <= 0)
                {
                    shieldsIgnored.Clear(); //Clear the List - we're probably outside of the Shield(s) we're ignoring
                }
            }
        }

        public void CheckforCollision()
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, layerMask);
            if (hit == null)
            {
                hit = Physics2D.CircleCast(transform.position, (radius * 1.5f), velocity, (velocity.magnitude * Time.deltaTime), layerMask).collider;
            }
            if (hit != null)
            {
                //Debug.Log("" + this.gameObject.name + " hit the " + hit.gameObject.name);
                Hit(hit);
                //return;
            }

            Vector2 newPos = (Vector2)transform.position + (velocity * Time.deltaTime);
            transform.position = newPos;

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Projectile other = collision.gameObject.GetComponent<Projectile>();
            if (other != null)
            {
                if (other.factionId != this.factionId)
                {
                    Hit(collision);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);

            if (hitProperties.hasSplashDamage)
            {
                foreach (SplashData splash in hitProperties.splashData)
                {
                    Color tempColor = Color.yellow;
                    Gizmos.color = tempColor;
                    Gizmos.DrawWireSphere(transform.position, splash.splashRadius);
                }
            }
        }

        private void LateUpdate()
        {
            if (type != ProjectileType.OTHER) transform.right = velocity.normalized;   //Rotate the transform in the direction of the velocity that it has
        }

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Fires projectile at given velocity from given position.
        /// </summary>
        /// <param name="velocity"></param>
        public void Fire(Vector2 position, Vector2 startVelocity)
        {
            velocity = startVelocity;
            transform.position = position;
            //transform.rotation = Quaternion.AngleAxis(Vector3.Angle(Vector2.right, velocity), Vector3.back);

            //Pre-Fire Collision Checks
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, layerMask);
            if (hits.Length > 0)
            {
                foreach (Collider2D _hit in hits)
                {
                    if (_hit.CompareTag("EnergyShield"))
                    {
                        shieldsIgnored.Add(_hit);
                        shieldsIgnoreCooldownTimer = 2f;
                    }

                    int cellLayerId = LayerMask.NameToLayer("Cell");
                    if (_hit.gameObject.layer == cellLayerId && isIndoors)
                    {
                        layerMask &= ~(1 << cellLayerId);
                    }
                }
            }

            Collider2D hit = Physics2D.OverlapCircle(position, radius, layerMask);
            if (hit != null)
            {
                Hit(hit);
            }
        }

        public void Hit(Collider2D hitCollider, bool destroyImmediate = false)
        {
            //Initialization:
            List<IDamageable> damagedThisHit = new List<IDamageable>(); //Create temporary list of targets that have been damaged by this projectile in this frame (used to prevent double damage due to splash)
            List<IBurnable> burnedThisHit = new List<IBurnable>();      //Create list to track targets burned by this projectile this hit

            //Check for Ignored Shields
            if (shieldsIgnored.Count > 0)
            {
                foreach(Collider2D collider in shieldsIgnored)
                {
                    if (hitCollider == collider)
                    {
                        //Hit a shield we're ignoring
                        return;
                    }
                }
            }

            bool hitGround = false;

            //Handle projectile collision:
            if (hitCollider != null) //Projectile has actually hit something
            {
                //Handle direct damage:
                IDamageable target = hitCollider.GetComponent<IDamageable>();                 //Try to get damage receipt component from collider object
                if (target == null) target = hitCollider.GetComponentInParent<IDamageable>(); //If damage receipt component is not in collider object, look in parent objects
                if (target != null) //Projectile has hit a target
                {
                    damagedThisHit.Add(target);                                //Indicate that target is being damaged now so it is not hit by splash damage later
                    remainingDamage = target.Damage(this, transform.position); //Strike the target and determine whether or not this projectile has any damage remaining
                    if (!hitProperties.tunnels) remainingDamage = 0;           //Make sure non-tunneling projectiles are terminated upon hit
                }
                else if (hitCollider.CompareTag("Ground")) //Hit the Ground
                {
                    SurfaceManager surface = hitCollider.GetComponent<SurfaceManager>();
                    if (surface?.surfaceMaterial == SurfaceManager.SurfaceMaterial.SAND) hitGround = true;
                    remainingDamage = 0; //Always destroy projectiles that hit the ground (by reducing their remaining damage to zero)
                }
                else if (hitCollider.CompareTag("Shell")) //Hit another projectile
                {
                    Projectile other = hitCollider.GetComponent<Projectile>();
                    if (other?.factionId != factionId) //Only process collisions between unfriendly projectiles
                    {
                        if (other.hitProperties.damage != 0) remainingDamage = 0; //Always destroy projectiles that hit the ground (by reducing their remaining damage to zero)
                        if (this.hitProperties.damage == 0) other.Reflect();
                        else other.remainingDamage = 0;
                        //other.Hit(gameObject.GetComponent<Collider2D>());
                    }
                }

                //Handle fire:
                if (hitProperties.fireChance > 0) //Only do fire stuff if projectile can cause fires
                {
                    IBurnable fireTarget = hitCollider.GetComponent<IBurnable>();                       //Try to get fire receipt component from collider object
                    if (fireTarget == null) fireTarget = hitCollider.GetComponentInParent<IBurnable>(); //If fire receipt component is not in collider object, look in parent objects
                    if (hitCollider.gameObject.layer == LayerMask.NameToLayer("EnergyShield")) fireTarget = null; //don't light energy shield parent on fire
                    if (fireTarget != null) //Projectile can light something on fire
                    {
                        if (Random.Range(0f, 1f) <= hitProperties.fireChance) fireTarget.Ignite(); //Roll a random chance depending on projectile-defined likelihood of fire and ignite target if roll is high enough
                        burnedThisHit.Add(fireTarget);                                             //Make sure target can't be targeted again this impact for fire
                    }
                }
            }

            //Handle projectile destruction:
            if (remainingDamage == 0 || destroyImmediate) //Projectiles which run out of damage to deal (or are commanded to) die/explode
            {
                //Deal splash damage:
                if (hitProperties.hasSplashDamage) //Projectile can deal splash damage
                {
                    foreach (SplashData splash in hitProperties.splashData) //Handle for each individual splash zone
                    {
                        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, splash.splashRadius, layerMask); //Get everything within splash damage radius
                        foreach (Collider2D collider in colliders)
                        {
                            //Damage check:
                            IDamageable splashTarget = collider.GetComponent<IDamageable>();                       //Try to get damage receipt component from collider object
                            if (splashTarget == null) splashTarget = collider.GetComponentInParent<IDamageable>(); //If damage receipt component is not in collider object, look in parent objects
                            if (splashTarget != null && !damagedThisHit.Contains(splashTarget)) //Explosion has hit a (new) target
                            {
                                damagedThisHit.Add(splashTarget);               //Add target to list so it cannot be damaged again by same explosion
                                splashTarget.Damage(splash.splashDamage, true); //Deal direct damage to each target
                                if (hitProperties.breakChance > 0 && GameManager.Instance.damageBreaksInteractables)
                                {
                                    //handle override break chance for splash damage here
                                }
                            }

                            //Fire check:
                            IBurnable fireTarget = collider.GetComponent<IBurnable>(); //Try to get fire receipt component from collider object
                            if (fireTarget == null) fireTarget = collider.GetComponentInParent<IBurnable>(); //If fire receipt component is not in collider object, look in parent objects
                            if (collider.gameObject.layer == LayerMask.NameToLayer("EnergyShield")) fireTarget = null; //don't light energy shield parent on fire
                            if (fireTarget != null && !burnedThisHit.Contains(fireTarget))                   //Projectile can light something on fire (and burn has not already been attempted on this target)
                            {
                                if (Random.Range(0f, 1f) <= hitProperties.fireChance) fireTarget.Ignite(); //Roll a random chance depending on projectile-defined likelihood of fire and ignite target if roll is high enough
                                burnedThisHit.Add(fireTarget);                                             //Make sure target can't be targeted again this impact for fire
                            }
                        }
                    }
                }

                if (destroyImmediate != true) HitEffects(hitGround);

                //Seperate smoketrail
                if (smokeTrail != null)
                {
                    smokeTrail.parent = null;
                    Lifetime lt = smokeTrail.gameObject.AddComponent<Lifetime>();
                    ParticleSystem ps = smokeTrail.gameObject.GetComponent<ParticleSystem>();
                    ps.Stop();
                    lt.lifeTime = 0.5f;
                }

                Destroy(gameObject);
            }
        }

        private void HitEffects(bool hitGround = false)
        {
            if (type == ProjectileType.SHELL)
            {
                GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);

                if (hitProperties.fireChance >= 0.9) GameManager.Instance.ParticleSpawner.SpawnParticle(17, transform.position, particleScale, null);
                else if (!hitGround)
                {
                    GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, particleScale, null);
                }

                if (hitGround) GameManager.Instance.ParticleSpawner.SpawnParticle(23, transform.position, particleScale + 1f, null);
            }

            if (type == ProjectileType.BULLET)
            {
                GameManager.Instance.AudioManager.Play("BulletImpactDirt", gameObject);

                if (!hitGround)
                {
                    GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(13, transform.position, particleScale, null);
                    particle.transform.rotation = transform.rotation;
                }
                else
                {
                    GameManager.Instance.ParticleSpawner.SpawnParticle(22, transform.position, 1, null);
                }
                
                float randomScale = Random.Range(0.05f, 0.1f);
                GameObject _particle = GameManager.Instance.ParticleSpawner.SpawnParticle(14, transform.position, randomScale, null);
                _particle.transform.rotation = transform.rotation;
            }

            if (type == ProjectileType.OTHER)
            {
                GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
                GameManager.Instance.AudioManager.Play("MedExplosionSFX", gameObject);
                GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, particleScale, null);
                GameManager.Instance.ParticleSpawner.SpawnParticle(23, transform.position, 1.5f, null);
            }
        }

        public void Reflect()
        {
            if (reflected) return;
            GameManager.Instance.ParticleSpawner.SpawnParticle(25, transform.position, 0.7f);
            velocity = -velocity;
            reflected = true;
        }

        //INTERFACE METHODS:
        public void ApplyMagnetForce(Vector2 force, Vector2 hitPoint)
        {
            velocity += force; //Apply force to velocity
        }
    }
}
