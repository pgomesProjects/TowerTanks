using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankPursueState : IState
{
    private readonly TankController _tank;
    
    public TankPursueState(TankController tank)
    {
        _tank = tank;
    }
    
    public void OnEnter() { }

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void OnExit() { }

}
