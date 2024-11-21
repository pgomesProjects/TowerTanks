using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class WeaponBrain : InteractableBrain
    {
        internal GunController gunScript;
        
        public float fireCooldown;
        internal float fireTimer;
        public float aimCooldown;
        internal float aimTimer;
        public float cooldownOffset;

        public bool isRotating;
        internal float currentForce = 0;

        private void Awake()
        {
            gunScript = GetComponent<GunController>();
        }

        protected void Start()
        {
            fireTimer = 0;
            aimTimer = 0;
        }

        // Update is called once per frame
        protected void Update()
        {
            if (fireTimer < fireCooldown) fireTimer += Time.deltaTime;
            else
            {
                gunScript.Fire(true, gunScript.tank.tankType);
                float randomOffset = Random.Range(-cooldownOffset, cooldownOffset);
                fireTimer = 0 + randomOffset;
            }

            if (aimTimer < aimCooldown) aimTimer += Time.deltaTime;
            else
            {
                float randomForce = Random.Range(-1.2f, 1.2f);
                //StartCoroutine(AimCannon(randomForce));

                float randomOffset = Random.Range(-2f, 2f);
                aimTimer = 0 + randomOffset;
            }

            if (isRotating)
            {
                gunScript.RotateBarrel(currentForce, false);
            }
        }

        public IEnumerator AimCannon(float force)
        {
            currentForce = 1.2f * Mathf.Sign(force);
            isRotating = true;
            yield return new WaitForSeconds(1.0f);
            isRotating = false;
        }
    }
}