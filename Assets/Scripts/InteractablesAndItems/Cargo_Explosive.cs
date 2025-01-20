using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class Cargo_Explosive : Cargo, IDamageable
    {
        [Header("Explosive Settings")]
        public float fuseTimer;
        public bool isLit;
        public float explosionDamage;

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
        private bool isExploding;

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
            isExploding = false;
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
            List<IDamageable> damagedThisHit = new List<IDamageable>(); //Create temporary list of targets that have been damaged by this projectile in this frame (used to prevent double damage due to splash)

            //AOE Damage
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, hitboxMask);
            foreach (Collider2D collider in colliders)
            {
                //Damage check:
                IDamageable splashTarget = collider.GetComponent<IDamageable>();                       //Try to get damage receipt component from collider object
                if (splashTarget == null) splashTarget = collider.GetComponentInParent<IDamageable>(); //If damage receipt component is not in collider object, look in parent objects
                if (splashTarget != null && !damagedThisHit.Contains(splashTarget)) //Explosion has hit a (new) target
                {
                    damagedThisHit.Add(splashTarget);           //Add target to list so it cannot be damaged again by same explosion
                    splashTarget.Damage(explosionDamage, true); //Deal direct damage to each target
                }
            }

            //Other Effects
            GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, 0.6f);
            GameManager.Instance.AudioManager.Play("CannonFire", gameObject);
            GameManager.Instance.AudioManager.Play("MedExplosionSFX", gameObject);

            if (isOnTank)
            {
                if (tankTransform != null)
                {
                    CameraManipulator.main.ShakeTankCamera(tankTransform.GetComponent<TankController>(), 
                        GameManager.Instance.SystemEffects.GetScreenShakeSetting("Explosion"));

                    //Apply Haptics to Players inside this tank
                    foreach (Character character in tankTransform.GetComponent<TankController>().GetCharactersInTank())
                    {
                        PlayerMovement player = character.GetComponent<PlayerMovement>();
                        if (player != null)
                        {
                            HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("BigRumble");
                            GameManager.Instance.SystemEffects.ApplyControllerHaptics(player.GetPlayerData().playerInput, setting); //Apply haptics
                        }
                    }
                }
            }

            Destroy(gameObject);
        }

        public IEnumerator ExplodeAfterDelay(float delay)
        {
            isExploding = true;
            yield return new WaitForSeconds(delay);
            Explode();
        }

        public float Damage(Projectile projectile, Vector2 position)
        {
            //If the current scene is the build scene, return
            if (GameManager.Instance.currentSceneState == SCENESTATE.BuildScene)
                return 0;

            animator.Play("BombFlash", 0, 0);

            float damage = projectile.remainingDamage;
            if (damage > 20) 
            {
                if (!isExploding) StartCoroutine(ExplodeAfterDelay(Random.Range(0.1f, 0.2f)));
            }
            else
            {
                Use();
            }

            return 0;
        }

        public void Damage(float damage, bool triggerHitEffect = false)
        {
            //If the current scene is the build scene, return
            if (GameManager.Instance.currentSceneState == SCENESTATE.BuildScene)
                return;

            animator.Play("BombFlash", 0, 0);

            if (damage > 20)
            {
                if (!isExploding) StartCoroutine(ExplodeAfterDelay(Random.Range(0.1f, 0.2f)));
            }
            else
            {
                Use();
            }
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
