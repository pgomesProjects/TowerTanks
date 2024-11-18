using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMachineGunBrain : WeaponBrain
    {
        private GunController gunScript;
        
        public float fireCooldown;
        private float fireTimer;
        public float aimCooldown;
        private float aimTimer;
        public float cooldownOffset;

        private float clip;
        private float clipMin = 12;
        private float clipMax = 30;

        public bool isRotating;
        private float currentForce = 0;

        private void Awake()
        {
            gunScript = GetComponent<GunController>();
        }

        void Start()
        {
            fireTimer = 0;
            aimTimer = 0;
            clip = Random.Range(clipMin, clipMax);
        }

        // Update is called once per frame
        void Update()
        {
            if (fireTimer < fireCooldown) fireTimer += Time.deltaTime;
            else
            {
                gunScript.Fire(true, gunScript.tank.tankType);
                float randomOffset = Random.Range(-cooldownOffset, cooldownOffset);
                fireTimer = 0 + randomOffset;

                if (gunScript.gunType == GunController.GunType.MACHINEGUN)
                {
                    clip -= 1;
                    if (clip <= 0)
                    {
                        fireTimer = Random.Range(-4, -1);
                        clip = Random.Range(clipMin, clipMax);
                    }
                }
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