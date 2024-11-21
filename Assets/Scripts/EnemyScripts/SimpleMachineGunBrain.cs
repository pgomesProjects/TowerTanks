using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMachineGunBrain : WeaponBrain
    {
        private float clip;
        private float clipMin = 12;
        private float clipMax = 30;

        void Start()
        {
            base.Start();
            clip = Random.Range(clipMin, clipMax);
        }
        
        void Update()
        {
            base.Update();
            
            if (fireTimer >= fireCooldown)
            {
                clip -= 1;
                if (clip <= 0)
                {
                    fireTimer = Random.Range(-4, -1);
                    clip = Random.Range(clipMin, clipMax);
                }
            }
        }
    }
}