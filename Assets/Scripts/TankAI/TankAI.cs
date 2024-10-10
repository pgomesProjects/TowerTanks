using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TowerTanks.Scripts
{
    public class TankAI : MonoBehaviour
    {
        private StateMachine _stateMachine;
        private TankController _tank, targetTank;
        private TankManager _tankManager;
        private GunController[] _guns;
        private float currentTokenCount;

        [Header("AI Configuration")]
        public TankAISettings aiSettings;

        private void Awake()
        {
            _tank = GetComponent<TankController>();
            if (_tank.tankType == TankId.TankType.PLAYER)
            {
                Destroy(this);
                return;
            }
            currentTokenCount = aiSettings.tankEconomy;
            _tankManager = FindObjectOfType<TankManager>();
            _stateMachine = new StateMachine();

            var patrolState = new TankPatrolState(this);
            var pursueState = new TankPursueState(this);
            var engageState = new TankEngageState(this);
            var surrenderState = new TankSurrenderState(this);

            void At(IState to, IState from, Func<bool> condition) => _stateMachine.AddTransition(to, from, condition);

            bool PlayerInViewRange() => Vector2.Distance(_tank.transform.position, _tankManager.playerTank.transform.position) < aiSettings.viewRange;
            bool TargetInEngagementRange() => Vector2.Distance(_tank.transform.position, targetTank.transform.position) < aiSettings.engagementRange;

            At(pursueState, patrolState, PlayerInViewRange);
            At(engageState, pursueState, TargetInEngagementRange);
            _stateMachine.SetState(patrolState);
        }

        public void SetTarget(TankController tank)
        {
            targetTank = tank;
        }

        public TankController GetTarget()
        {
            return targetTank;
        }

        private void Update()
        {
            _stateMachine.FrameUpdate();
            var tanks = Physics2D.OverlapCircleAll(transform.position, aiSettings.viewRange, 1 << LayerMask.NameToLayer("Treads"));
        }

        private void FixedUpdate()
        {
            _stateMachine.PhysicsUpdate();
        }
    }
}
