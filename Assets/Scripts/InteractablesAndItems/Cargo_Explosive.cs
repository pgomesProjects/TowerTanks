using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class Cargo_Explosive : Cargo
    {
        [Header("Explosive Settings")]
        public float fuseTimer;
        public bool isLit;

        //Components
        private Transform smokeTrail;
        private Animator animator;
        private float bombFlashTimer;
        private float randomDelay;

        public LayerMask hitboxMask;
        public float explosionRadius;

        //public bool isExploding = false;

        [Header("Debug")]
        public bool detonate;

        protected override void Awake()
        {
            base.Awake();

            smokeTrail = transform.Find("smokeTrail");
            smokeTrail.gameObject.SetActive(false);
            animator = GetComponent<Animator>();
        }

        protected override void Start()
        {
            base.Start();

            float randomOffset = Random.Range(0f, 4f);
            fuseTimer += randomOffset;
            bombFlashTimer = 0;
            randomDelay = Random.Range(4f, 5.5f);
        }

        protected override void Update()
        {
            base.Update();

            if (isLit)
            {
                if (!smokeTrail.gameObject.activeInHierarchy) smokeTrail.gameObject.SetActive(true);
                fuseTimer -= Time.deltaTime;

                if (fuseTimer <= 0) Explode();


                bombFlashTimer -= Time.deltaTime;
                if (bombFlashTimer <= 0)
                {
                    animator.Play("BombFlash", 0, 0);
                    bombFlashTimer = 1f;
                    if (fuseTimer <= randomDelay) bombFlashTimer = 0.4f;
                }
            }

            if (detonate) { Explode(); }

        }

        public void Explode()
        {
            //AOE Damage
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, hitboxMask);
            foreach (Collider2D collider in colliders)
            {
                Cell cellScript = collider.gameObject.GetComponent<Cell>();
                if (cellScript != null)
                {
                    cellScript.Damage(200);
                }

                if (collider.CompareTag("Destructible"))
                {
                    collider.gameObject.GetComponent<DestructibleObject>().Damage(200);
                }

                /*if (collider.CompareTag("Cargo"))
                {
                    if (collider.gameObject.GetComponent<Cargo>().type == CargoType.EXPLOSIVE)
                    {
                        collider.gameObject.GetComponent<Cargo_Explosive>().isExploding = true;
                    }
                    else
                    {
                        Destroy(collider.gameObject);
                    }
                }*/
            }

            //Other Effects
            GameManager.Instance.ParticleSpawner.SpawnParticle(4, transform.position, 0.2f);
            GameManager.Instance.AudioManager.Play("CannonFire", gameObject);
            GameManager.Instance.AudioManager.Play("MedExplosionSFX", gameObject);

            Destroy(gameObject);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
