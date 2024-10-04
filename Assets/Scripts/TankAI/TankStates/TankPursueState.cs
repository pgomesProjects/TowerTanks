using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
    
    private IEnumerator Heartbeat()
    {
        if (_tankAI.GetTarget().transform.position.x < _tank.transform.position.x)
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

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void OnExit()
    {
        _tank.StopCoroutine(_heartbeatCoroutine);
    }

}
