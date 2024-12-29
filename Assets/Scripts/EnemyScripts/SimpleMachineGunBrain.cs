using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMachineGunBrain : WeaponBrain
    {
        private float clip;
        private float clipMin = 15;
        private float clipMax = 35;

        void Start()
        {
            clip = Random.Range(clipMin, clipMax);
        }
        
        void Update()
        {
            base.Update();
            
            if (fireTimer >= gunScript.rateOfFire)
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