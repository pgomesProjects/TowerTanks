using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class SimpleMachineGunBrain : WeaponBrain
    {
        void Update()
        {
            base.Update();
            
            stopFiring = aggroCooldownTimer < aggroCooldown;
            
        }

        private void FixedUpdate()
        {
            base.FixedUpdate();
            if (gunScript.isOverheating)
            {
                aggroCooldownTimer = 0 + GetAggroOffset();
            }
            else
            {
                aggroCooldownTimer += Time.fixedDeltaTime;
            }

        }
    }
}