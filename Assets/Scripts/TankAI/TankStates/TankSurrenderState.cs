using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankSurrenderState : IState
{
    private TankAI _tankAI;
    private TankController _tank;
    
    public TankSurrenderState(TankAI tank)
    {
        _tankAI = tank;
        _tank = tank.GetComponent<TankController>();
    }
    public void OnEnter() { }

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void OnExit() { }

}
