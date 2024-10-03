using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankAI : MonoBehaviour
{
    private StateMachine _stateMachine;
    private TankController _tank;
    private TankManager _tankManager;
    private GunController[] _guns;

    private void Awake()
    {
        _tank = GetComponent<TankController>();
        if (_tank.tankType == TankId.TankType.PLAYER)
        {
            Destroy(this);
            return;
        }
        _tankManager = FindObjectOfType<TankManager>();
        _stateMachine = new StateMachine();
        
        var patrolState    = new TankPatrolState(_tank);
        var pursueState    = new TankPursueState(_tank);
        var engageState    = new TankEngageState(_tank);
        var surrenderState = new TankSurrenderState(_tank);
        
        void At(IState to, IState from, Func<bool> condition) => _stateMachine.AddTransition(to, from, condition);
        
        _stateMachine.SetState(patrolState);
    }


    private void Update()
    {
        _stateMachine.FrameUpdate();
    }
    
    private void FixedUpdate()
    {
        _stateMachine.PhysicsUpdate();
    }
}
