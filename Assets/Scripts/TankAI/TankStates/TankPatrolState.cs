using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;


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
        if (Random.Range(0, 2) == 1)
        {
            while (_tank.gear != 1)
            {
                _tank.ShiftRight();
                yield return null;
            }
        }
        else
        {
            while (_tank.gear != -1)
            {
                _tank.ShiftLeft();
                yield return null;
            }
        }

        yield return new WaitForSeconds(Random.Range(_timeBetweenMovesRange.x, _timeBetweenMovesRange.y));
        _movementCoroutine = _tank.StartCoroutine(SetTankMovement());
        
    }
    
    public void OnEnter()
    {
        Debug.Log($"OnEnter called. _tank: {_tank}");
        if (_movementCoroutine == null) _movementCoroutine = _tank.StartCoroutine(SetTankMovement());
    }

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void OnExit()
    {
        _tank.StopCoroutine(_movementCoroutine);
    }
    

}
