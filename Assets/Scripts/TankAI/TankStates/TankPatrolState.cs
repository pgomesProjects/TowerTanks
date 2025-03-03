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
        private float patrolPoint;

        public TankPatrolState(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }
        private IEnumerator SetTankMovement()
        {
            yield return new WaitForSeconds(Random.Range(_timeBetweenMovesRange.x, _timeBetweenMovesRange.y));
            if (_tankAI.HasActiveThrottle())
            {
                float dist = _tank.treadSystem.transform.position.x - patrolPoint;
                if (Mathf.Abs(dist) > 15)
                {
                    if (dist > 0) //if positive, we want to move left, cause we are too far right from our patrol point
                    {
                        _tank.SetTankGearOverTime(-1, .15f);
                    }
                    else
                    {
                        _tank.SetTankGearOverTime(1, .15f);
                    }
                }
                else
                {
                    _tankAI.MoveRandom(1);
                }
            }
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
            patrolPoint = _tank.treadSystem.transform.position.x;
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
