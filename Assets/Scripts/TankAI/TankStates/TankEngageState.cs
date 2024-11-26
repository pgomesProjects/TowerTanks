using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankEngageState : IState
    {
        private TankAI _tankAI;
        private TankController _tank;
        private float heartbeatTimer = 1f;

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
                if (_tankAI.TargetTooClose())
                {
                    _tank.SetTankGear(2 * -dir, .15f);
                    Debug.Log("Target is too close!");
                }
                else
                {
                    _tankAI.MoveRandom(2);
                }
            }
            yield return new WaitForSeconds(heartbeatTimer);
            _tank.StartCoroutine(Heartbeat());
        }

        public void OnEnter()
        {
            _tankAI.DistributeAllWeightedTokens(_tankAI.aiSettings.engageStateInteractableWeights);
            _tank.StartCoroutine(Heartbeat());
        }

        public void FrameUpdate() { }

        public void PhysicsUpdate() { }

        public void OnExit()
        {
            _tankAI.RetrieveAllTokens();
            _tank.StopAllCoroutines();
        }

    }
}
