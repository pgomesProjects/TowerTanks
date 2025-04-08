using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankEngageState : IState
    {
        private TankAI _tankAI;
        private TankController _tank;
        private float heartbeatTimer = 1;

        public TankEngageState(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }

        private IEnumerator Heartbeat()
        {
            var dir = _tankAI.TankIsRightOfTarget() ? -1 : 1;
            if (_tankAI.HasActiveThrottle())
            {
                if (!_tankAI.TargetAtFightingDistance()) dir *= 2;
                if (_tankAI.TargetAtFightingDistance() && Random.Range(0, 2) == 0) dir = 0; //if we are at our fighting distance, stop moving.
                                                                                               //i have a random 50/50 for if the tank stops entirely
                if (_tankAI.TargetTooClose())                                               //at the fight distance, or keeps moving a little bit.         
                {                                                                           //so, at fight distance, it should move a few inches
                    _tank.SetTankGearOverTime(-dir, .15f);        //every now and then. just humanizes the movement a bit
                }                                                                           
                else
                {
                    _tank.SetTankGearOverTime(dir, .15f);
                }
            }
            yield return new WaitForSeconds(heartbeatTimer);
            _tank.StartCoroutine(Heartbeat());
        }
        
        private IEnumerator RedistributeTokens()
        {
            yield return new WaitForSeconds(_tankAI.aiSettings.redistributeTokensCooldown);
            _tankAI.RetrieveAllTokens(true);
            _tankAI.DistributeAllWeightedTokens(_tankAI.aiSettings.engageStateInteractableWeights);
            _tank.StartCoroutine(RedistributeTokens());
        }

        public void OnEnter()
        {
            _tankAI.DistributeAllWeightedTokens(_tankAI.aiSettings.engageStateInteractableWeights);
            _tank.StartCoroutine(Heartbeat());
            _tank.StartCoroutine(RedistributeTokens());
        }

        public void FrameUpdate() { }

        public void PhysicsUpdate() { }

        public void OnExit()
        {
            _tankAI.RetrieveAllTokens(true);
            _tank.StopAllCoroutines();
        }

    }
}
