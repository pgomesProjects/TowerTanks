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
            if (gunScript.isOverheating)
            {
                aggroCooldownTimer = 0;
            }
            else
            {
                aggroCooldownTimer += Time.deltaTime;
            }
            
            stopFiring = aggroCooldownTimer < aggroCooldown;
            
        }
    }
}