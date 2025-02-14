using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankSurrenderState : IState
    {
        private TankAI _tankAI;
        private TankController _tank;

        public TankSurrenderState(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }

        public void OnEnter()
        {
            _tank.SetTankGearOverTime(0, .15f); //stop moving
            _tank.Surrender();
        }

        public void FrameUpdate() { }

        public void PhysicsUpdate() { }

        public void OnExit()
        {
            _tankAI.RetrieveAllTokens();
        }

    }
}
