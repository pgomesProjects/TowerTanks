using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class EnergyShieldBrain : InteractableBrain
    {
        private EnergyShieldController shieldController;
        [SerializeField, Tooltip("MaxShieldCooldown is the cooldown used when the tank's aggression is at 0, and min is " +
                                 "used when the tank's aggression is at 1. at 0.5 aggression, it uses the value in the" +
                                 " middle of these 2 floats, etc etc, " +
                                 "you get the idea")] private float maxShieldCooldown, minShieldCooldown;
        [SerializeField, Tooltip("A random offset will be added to the cooldown between randomOffset and half of -randomOffset. " +
                                 "Also lerped with aggression to be less of a random offset for higher aggressions")] private float randomOffset;
        
        protected override void Awake()
        {
            base.Awake();
            shieldController = GetComponent<EnergyShieldController>();
        }
        
        private IEnumerator Heartbeat()
        {
            while (enabled)
            {
                float cooldown = Mathf.Lerp(maxShieldCooldown, minShieldCooldown, shieldController.tank._thisTankAI.aiSettings.aggression); //if aggression is at 1, we have a very small cooldown
                float offset = Mathf.Lerp(randomOffset, -randomOffset * .5f, shieldController.tank._thisTankAI.aiSettings.aggression);
                if (shieldController.tank._thisTankAI.aiSettings.aggression < .75f) cooldown += Random.Range(0f, offset);
                yield return new WaitForSeconds(cooldown);
                shieldController.Use();
            }
        }

        private void OnEnable()
        {
            StartCoroutine(Heartbeat());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }
}
