using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankPursueState : IState
    {
        private TankAI _tankAI;
        private TankController _tank;
        private float heartbeatTimer = 5f;
        private Coroutine _heartbeatCoroutine;

        public TankPursueState(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }

        public void OnEnter()
        {
            _tankAI.SetTarget(TankManager.instance.playerTank);
            _heartbeatCoroutine = _tank.StartCoroutine(Heartbeat());
            Debug.Log("Pursue state entered.");
        }

        private IEnumerator Heartbeat()
        {
            Debug.Log("AI Beat");
            if (_tankAI.GetTarget().transform.position.x < _tank.transform.position.x)
            {
                _tank.SetTankGear(-2);
            }
            else
            {
                _tank.SetTankGear(2);
            }
            yield return new WaitForSeconds(heartbeatTimer);
            _heartbeatCoroutine = _tank.StartCoroutine(Heartbeat());
        }

        public void FrameUpdate() { }

        public void PhysicsUpdate() { }

        public void OnExit()
        {
            _tank.StopCoroutine(_heartbeatCoroutine);
        }

    }
}
