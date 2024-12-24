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
            base.Start();
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
            
            if (Vector2.Distance(myTankAI.tank.treadSystem.transform.position, myTankAI.targetTank.treadSystem.transform.position) > 50)
            {
                fireTimer = 0; //machine gun will no longer shoot if the target is out of range of it's bullets
            }
        }
    }
}