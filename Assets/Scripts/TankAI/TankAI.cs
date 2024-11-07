using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private Vector3 movePoint;

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
        }
        
        private void Start()
        {
            _stateMachine = new StateMachine();
            var patrolState = new TankPatrolState(this);
            var pursueState = new TankPursueState(this);
            var engageState = new TankEngageState(this);
            var surrenderState = new TankSurrenderState(this);
            
            Debug.Log($"PlayerInViewRange: {Vector2.Distance(_tank.transform.position, _tankManager.playerTank.transform.position)}");
            bool PlayerInViewRange() => Vector2.Distance(_tank.transform.position, _tankManager.playerTank.transform.position) < aiSettings.viewRange;
            bool TargetInEngagementRange() => Vector2.Distance(_tank.transform.position, targetTank.transform.position) < aiSettings.engagementRange;
            
            
            void At(IState to, IState from, Func<bool> condition) => _stateMachine.AddTransition(from, to, condition);
            At(pursueState, patrolState, PlayerInViewRange);
            //At(engageState, pursueState, TargetInEngagementRange);
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

        #region Tank AI Functionality Methods
        
        /// <summary>
        /// Will set the tank's throttle towards it's set movepoint value.
        /// </summary>
        /// <param name="speedSetting">
        /// The speed setting to set the throttle to.
        /// </param>
        public void ChangeThrottleTowardsMovepoint(int speedSetting)
        {
            int dir = _tank.transform.position.x < movePoint.x ? 1 : -1;
            _tank.SetTankGear(speedSetting, .1f);
        }
        
        public void SetMovePoint(Vector3 point)
        {
            movePoint = point;
        }

        #endregion

    }
}
