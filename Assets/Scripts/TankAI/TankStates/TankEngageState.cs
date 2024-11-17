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
        private Coroutine _heartbeatCoroutine;

        public TankEngageState(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }

        private IEnumerator Heartbeat()
        {
            bool isPlayerOnLeft = _tankAI.GetTarget().transform.position.x < _tank.transform.position.x;
            int directionToTarget = isPlayerOnLeft ? -1 : 1;
            if (_tankAI.GetTarget().transform.position.x + (_tankAI.aiSettings.defaultFightingDistance * directionToTarget) < _tank.transform.position.x)
            {
                while (_tank.gear != -2)
                {
                    _tank.ShiftLeft();
                    yield return null;
                }
            }
            else
            {
                while (_tank.gear != 2)
                {
                    _tank.ShiftRight();
                    yield return null;
                }
            }
            yield return new WaitForSeconds(heartbeatTimer);
            _heartbeatCoroutine = _tank.StartCoroutine(Heartbeat());
        }

        public void OnEnter()
        {
            _tankAI.DistributeAllWeightedTokens(_tankAI.aiSettings.engageStateInteractableWeights);
            _heartbeatCoroutine = _tank.StartCoroutine(Heartbeat());
        }

        public void FrameUpdate() { }

        public void PhysicsUpdate() { }

        public void OnExit()
        {
            _tankAI.RetrieveAllTokens();
            _tank.StopCoroutine(_heartbeatCoroutine);
        }

    }
}
