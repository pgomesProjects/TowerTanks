using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class Projectile : MonoBehaviour
    {
        //Objects & Components:
        public enum ProjectileType { BULLET, SHELL, OTHER };
        public ProjectileType type;

        public LayerMask layerMask;
        private Transform smokeTrail;
        public float particleScale;

        //Settings:
        [SerializeField, Tooltip("Damage dealt on direct hit")] public float damage;  //Damage projectile will deal upon hitting a valid target
        [SerializeField, Tooltip("Directional force to apply to the target when hit")] public float knockbackForce;
        [SerializeField, Tooltip("Amount of time to apply tread system stun effect when applying knockback")] public float stunTime;
        [SerializeField, Tooltip("If true, this projectile utilizes splash damage")] public bool hasSplashDamage; //Whether or not this projectile deals splash damage
        [SerializeField, Tooltip("Contains values related to splash damage zones")] public SplashData[] splashData; //Contains all values related to different splash damage zones
        [SerializeField, Tooltip("If true, this projectile uses the 'Tunneling' mechanic")] public bool isTunneling;
        [SerializeField, Tooltip("Chance this projectile lights things on fire when dealing damage")] public float fireChance;

        [SerializeField, Tooltip("Maximum lifetime of this projectile")] public float maxLife; //Maximum amount of time projectile can spend before it auto-destructs
        [SerializeField, Tooltip("Radius of projectile collider")] public float radius;  //Radius around projectile which is used to check for hits
        [SerializeField, Tooltip("Causes projectile to lose velocity over time")] public float drag; //how fast this projectile loses velocity over time
        [SerializeField, Tooltip("How fast this projectile falls")] public float gravity; //how fast this projectile falls

        [Header("Inheritance")]
        [SerializeField, Tooltip("Which faction this projectile belongs to")] public TankId.TankType factionId;

        //Runtime Variables:
        private Vector2 velocity; //Speed and trajectory of projectile
        private float timeAlive;

        //RUNTIME METHODS:
        private void Awake()
        {
            smokeTrail = transform.Find("smokeTrail");
        }

        private void Update()
        {
            velocity += gravity * Time.deltaTime * Vector2.down;
            //velocity -= drag * Time.deltaTime * new Vector2(transform.right.x, transform.right.y);

            //if (drag > 0 && Mathf.Abs(velocity.x) <= 1) Hit(null, true);

            //transform.rotation = Quaternion.AngleAxis(Vector3.Angle(Vector2.right, velocity), Vector3.back);

            CheckforCollision();

            timeAlive += Time.deltaTime;
            if (timeAlive >= maxLife) Hit(null, true);
        }

        public void CheckforCollision()
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, layerMask);
            if (hit == null)
            {
                hit = Physics2D.CircleCast(transform.position, radius, velocity, (velocity.magnitude * Time.deltaTime), layerMask).collider;
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

            if (hasSplashDamage)
            {
                foreach (SplashData splash in splashData)
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

            Collider2D hit = Physics2D.OverlapCircle(position, radius, layerMask);
            if (hit != null) Hit(hit);
        }

        public void Hit(Collider2D target, bool destroyImmediate = false)
        {
            //Debug.Log("Hit " + target.gameObject.name);
            List<Collider2D> hitThisFrame = new List<Collider2D>(); //Create Temp List for Colliders Hit
            hitThisFrame.Add(target); //Add Direct Hit to Collider
            bool damagedCoreThisFrame = false;
            bool destroyThis = true;
            float damageDealt = 0;

            //Handle Projectile Direct Damage
            if (target != null && target.GetComponentInParent<EnergyShieldController>() != null) //Hit Energy Shield
            {
                EnergyShieldController shield = target.GetComponentInParent<EnergyShieldController>();
                destroyThis = true;
            }

            else if (target != null && target.GetComponentInParent<Cell>() != null) //Hit Cell
            {
                Cell cellHit = target.GetComponentInParent<Cell>();
                damageDealt = cellHit.Damage(damage);
                if (cellHit.room.isCore)
                { //Hit the Core
                    damageDealt = damage;
                    destroyThis = true;
                    damagedCoreThisFrame = true;
                }
                else
                {
                    if (RollFireChance()) { cellHit.Ignite(); } //Check for Fire
                }

                //Apply Knockback Force
                if (knockbackForce > 0)
                {
                    knockbackForce *= Mathf.Sign(velocity.x);
                    cellHit.room.targetTank.treadSystem.ApplyForce(transform.position, knockbackForce, stunTime);
                }

                GameManager.Instance.AudioManager.Play("ShellImpact", gameObject);
            }

            else if (target != null && target.GetComponentInParent<TreadSystem>() != null) //Hit Treads
            {
                TreadSystem treads = target.GetComponentInParent<TankController>().treadSystem;
                if (treads != null)
                {
                    //Damage Treads
                    treads.Damage(damage);

                    //Apply Knockback Force
                    if (knockbackForce > 0)
                    {
                        float knockBackTime = knockbackForce * 0.05f;

                        if (target.transform.position.x < transform.position.x) { knockbackForce *= -1f; }
                        treads.ApplyForce(transform.position, knockbackForce, knockBackTime);
                    }

                    GameManager.Instance.AudioManager.Play("TankImpact", gameObject);
                }
            }

            else if (target != null && target.CompareTag("Destructible")) //Hit Destructible Object
            {
                damageDealt = target.GetComponent<DestructibleObject>().Damage(damage);
                GameManager.Instance.AudioManager.Play("ShellImpact", gameObject);
            }

            else if (target != null && target.GetComponentInParent<Character>() != null) //Hit Character
            {
                Character character = target.GetComponentInParent<Character>();
                damageDealt = character.ModifyHealth(-damage);
            }

            else if (target != null && target.CompareTag("Ground")) //Hit the Ground
            {
                damageDealt = damage;
                destroyThis = true;
            }

            else if (target != null && target.CompareTag("Shell")) //Hit another projectile
            {
                Projectile other = target.GetComponent<Projectile>();
                if (other?.factionId != factionId) //If the other projectile doesn't belong to the same faction as this one
                {
                    damageDealt = damage;
                    destroyThis = true;
                    //other.Hit(gameObject.GetComponent<Collider2D>());
                }
                else destroyThis = false;
            }

            //Check Tunneling
            if (isTunneling)
            {
                damage -= damageDealt;
                if (damage <= 0)
                {
                    damage = 0;
                    destroyThis = true;
                }
                else destroyThis = false;
            }

            if (destroyThis || destroyImmediate)
            {
                //Handle Projectile Splash Damage
                if (hasSplashDamage)
                {
                    foreach (SplashData splash in splashData) //Handle for each individual splash zone
                    {
                        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, splash.splashRadius, layerMask);
                        foreach (Collider2D collider in colliders)
                        {
                            if (!hitThisFrame.Contains(collider)) //If the Collider has not been damaged by any other damage sources in this event this frame
                            {
                                hitThisFrame.Add(collider);

                                Cell cellScript = collider.gameObject.GetComponent<Cell>();
                                if (cellScript != null)
                                {
                                    if (!damagedCoreThisFrame)
                                    {
                                        cellScript.Damage(splash.splashDamage);
                                        if (cellScript.room.isCore) { damagedCoreThisFrame = true; }
                                        else if (RollFireChance()) { cellScript.Ignite(); } //Check for Fire
                                    }
                                }

                                if (collider.CompareTag("Destructible"))
                                {
                                    collider.gameObject.GetComponent<DestructibleObject>().Damage(splash.splashDamage);
                                }

                                Character character = collider.gameObject.GetComponent<Character>();
                                if (character != null)
                                {
                                    character.ModifyHealth(-splash.splashDamage);
                                }
                            }
                        }
                    }
                }

                hitThisFrame.Clear();

                if (destroyImmediate != true) HitEffects();

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

        private void HitEffects()
        {
            if (type == ProjectileType.SHELL)
            {
                GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
                GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, particleScale, null);
            }

            if (type == ProjectileType.BULLET)
            {
                GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);

                GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(13, transform.position, particleScale, null);
                particle.transform.rotation = transform.rotation;

                float randomScale = Random.Range(0.05f, 0.1f);
                particle = GameManager.Instance.ParticleSpawner.SpawnParticle(14, transform.position, randomScale, null);
                particle.transform.rotation = transform.rotation;
            }

            if (type == ProjectileType.OTHER)
            {
                GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
                GameManager.Instance.AudioManager.Play("MedExplosionSFX", gameObject);
                GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, particleScale, null);
            }
        }

        private bool RollFireChance()
        {
            bool canIgnite = false;
            float randomRoll = Random.Range(0.1f, 100f);
            if (randomRoll <= fireChance)
            {
                canIgnite = true;
            }
            return canIgnite;
        }
    }
}
