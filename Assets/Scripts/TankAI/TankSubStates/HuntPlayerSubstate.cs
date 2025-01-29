using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class HuntPlayerSubstate : ISubState
    {
        private TankAI _tankAI;
        private TankController _tank;
        private InteractableId targetWeapon;
        private WeaponBrain targetBrain;
        private List<INTERACTABLE> weaponPriorityList = new List<INTERACTABLE>
        {
            INTERACTABLE.MachineGun, // from top to bottom, which weapon to prioritize killing player with
            INTERACTABLE.Cannon,
            INTERACTABLE.Mortar
        };
        
        public HuntPlayerSubstate(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }
        
        public bool PauseParentState { get; set; }
        public void FrameUpdate()
        {
            
        }

        public void PhysicsUpdate()
        {
            
        }

        public void OnEnter()
        {
            
            Debug.Log("HuntPlayerSubstate OnEnter");
            var players = GameObject.FindObjectsOfType<PlayerMovement>();
            var nearestPlayer = players.OrderBy(x => Vector3.Distance(x.transform.position, _tank.treadSystem.transform.position)).First();
            foreach (var interactable in weaponPriorityList)
            {
                if (_tankAI.CheckIfAvailable(interactable))
                {
                    if (_tankAI.CheckIfHasToken(interactable))
                    {
                        targetWeapon = _tankAI.tokenActivatedInteractables.First(x => x.brain.mySpecificType == interactable);
                        break;
                    }
                    
                    if (_tankAI.CheckIfHasToken(INTERACTABLE.Boiler))
                    {
                        var boiler = _tankAI.tokenActivatedInteractables.First(x =>
                            x.brain.mySpecificType == INTERACTABLE.Boiler);
                        _tankAI.RetrieveToken(boiler);
                        _tankAI.DistributeToken(interactable);
                        targetWeapon = _tankAI.tokenActivatedInteractables.First(x => x.brain.mySpecificType == interactable);
                        targetBrain = targetWeapon.brain as WeaponBrain;
                        break;
                    }
                    
                }
            }
            targetBrain?.OverrideTargetPoint(nearestPlayer.transform);
            if (targetBrain != null)
            {
                if (targetBrain.updateAimTarget != null) targetBrain.StopCoroutine(targetBrain.updateAimTarget);
            }
            targetBrain.updateAimTarget = targetBrain.StartCoroutine(targetBrain.UpdateTargetPoint(_tankAI.aiSettings.tankAccuracy));
            if (targetBrain != null) targetBrain.updateAimTarget = targetBrain.StartCoroutine(targetBrain.UpdateTargetPoint(_tankAI.aiSettings.tankAccuracy));
            
             
        }

        public void OnExit()
        {
            Debug.Log("HuntPlayerSubstate OnExit");
            targetBrain?.ResetTargetPoint();
            _tankAI.RetrieveToken(targetWeapon);
            _tankAI.DistributeToken(INTERACTABLE.Boiler);
        }
    }
}
