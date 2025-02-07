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
            _tank.StartCoroutine(SetTankMovement());
            
        }
        
        private IEnumerator RefreshTarget()
        {
            _tankAI.SetClosestTarget();
            yield return new WaitForSeconds(5);
            _tank.StartCoroutine(RefreshTarget());
        }

        public void OnEnter()
        {
            Debug.Log($"OnEnter called. _tank: {_tank}");
            _tankAI.DistributeAllWeightedTokens(_tankAI.aiSettings.patrolStateInteractableWeights);
            _tank.StartCoroutine(SetTankMovement());
            _tank.StartCoroutine(RefreshTarget());
        }

        public void FrameUpdate() { }

        public void PhysicsUpdate() { }

        public void OnExit()
        {
            _tank.StopAllCoroutines();
            _tankAI.RetrieveAllTokens(true);
            Debug.Log("OnExit patrol called.");
        }


    }
}
