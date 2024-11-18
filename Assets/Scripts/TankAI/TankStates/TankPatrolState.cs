using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class TankPatrolState : IState
    {
        private TankAI _tankAI;
        private TankController _tank;
        private Vector2 _timeBetweenMovesRange = new Vector2(4.00f, 8.00f);
        private Coroutine _movementCoroutine;

        public TankPatrolState(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }
        private IEnumerator SetTankMovement()
        {
            if (_tankAI.HasActiveThrottle())
            {
                _tankAI.MoveRandom(1);
            }

            yield return new WaitForSeconds(Random.Range(_timeBetweenMovesRange.x, _timeBetweenMovesRange.y));
            _movementCoroutine = _tank.StartCoroutine(SetTankMovement());

        }

        public void OnEnter()
        {
            Debug.Log($"OnEnter called. _tank: {_tank}");
            _tankAI.DistributeAllWeightedTokens(_tankAI.aiSettings.patrolStateInteractableWeights);
            if (_movementCoroutine == null) _movementCoroutine = _tank.StartCoroutine(SetTankMovement());
        }

        public void FrameUpdate() { }

        public void PhysicsUpdate() { }

        public void OnExit()
        {
            _tank.StopCoroutine(_movementCoroutine);
            _tankAI.RetrieveAllTokens();
            Debug.Log("OnExit patrol called.");
        }


    }
}
