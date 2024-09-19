using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankEngageState : IState
{
    private readonly TankController _tank;
    
    public TankEngageState(TankController tank)
    {
        _tank = tank;
    }
    
    public void OnEnter() { }

    public void FrameUpdate() { }

    public void PhysicsUpdate() { }

    public void OnExit() { }
    
}
