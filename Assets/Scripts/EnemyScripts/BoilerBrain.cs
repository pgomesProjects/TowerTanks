using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class BoilerBrain : InteractableBrain
    {
        private EngineController engineController;
        
        private void Awake()
        {
            engineController = GetComponent<EngineController>();
        }
        
        private IEnumerator Heartbeat()
        {
            var randTime = Random.Range(engineController.minChargeTime, engineController.maxChargeTime); //we charge for a rand amt of time for now
            yield return new WaitUntil(() => engineController.pressure <= 50); //dont run our next charge until we are below 50 pressure (avoids explosion)
            engineController.StartCharge();
            while (engineController.chargeStarted && engineController.chargeTimer < randTime)
            {
                engineController.chargeTimer += Time.deltaTime;
                yield return null; //skips a frame on every iteration, so this while loop acts like an update task
            }
            engineController.CheckCharge(); //this is what actually applies the charge
            yield return new WaitForSeconds(5);
            var rand = Random.Range(0f, 1.0f);
            yield return new WaitForSeconds(rand);
            
            StartCoroutine(Heartbeat());
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
