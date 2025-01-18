using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class CargoSprayer : Cargo
    {
        [Header("Sprayer Settings:")]
        public Transform nozzle;
        public bool isSpraying;

        private float sprayRate = 0.05f;
        private float sprayTimer = 0;

        private ParticleSystem spray;

        // Start is called before the first frame update
        void Start()
        {
            base.Start();

            spray = nozzle.GetComponentInChildren<ParticleSystem>();
        }

        // Update is called once per frame
        void Update()
        {
            base.Update();

            if (cooldown > 0) cooldown -= Time.deltaTime;

            if (isSpraying && cooldown <= 0)
            {
                sprayTimer -= Time.deltaTime;
                if (sprayTimer <= 0)
                {
                    if (!GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", this.gameObject))
                    {
                        GameManager.Instance.AudioManager.Play("SteamExhaustLoop", this.gameObject);
                    }
                    Spray();
                    spray.Play();
                    sprayTimer = sprayRate;
                }
            }
            else
            {
                if (GameManager.Instance.AudioManager.IsPlaying("SteamExhaustLoop", this.gameObject))
                {
                    GameManager.Instance.AudioManager.Stop("SteamExhaustLoop", this.gameObject);
                    GameManager.Instance.AudioManager.Play("SteamExhaust", this.gameObject);
                }
                spray.Stop();
            }

            if (isOnTank)
            {
                var main = spray.main;
                if (tankTransform != null)
                {
                    if (main.simulationSpace != ParticleSystemSimulationSpace.Custom) main.simulationSpace = ParticleSystemSimulationSpace.Custom;
                    main.customSimulationSpace = tankTransform;
                }
                else
                {
                    if (main.simulationSpace != ParticleSystemSimulationSpace.World) main.simulationSpace = ParticleSystemSimulationSpace.World;
                }
            }
        }


        public void Spray()
        {
            //GameObject particle = GameManager.Instance.ParticleSpawner.SpawnParticle(15, nozzle.position, 1f);
            //particle.transform.rotation = nozzle.rotation;
        }

        public void UpdateNozzle(Vector2 direction)
        {

            Vector3 moveVector = (Vector3.up * direction.y - Vector3.left * direction.x);
            if (direction.x != 0 || direction.y != 0)
            {
                nozzle.rotation = Quaternion.LookRotation(Vector3.forward, moveVector);
            }
        }

    }
}
