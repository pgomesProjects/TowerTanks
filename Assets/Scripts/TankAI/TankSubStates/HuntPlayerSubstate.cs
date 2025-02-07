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

        private INTERACTABLE interactableTakenFrom;
        private bool tokenBorrowed;
        private bool couldntOverride;
        private bool returnTokenLater;
        
        public HuntPlayerSubstate(TankAI tank)
        {
            _tankAI = tank;
            _tank = tank.GetComponent<TankController>();
        }
        
        public bool PauseParentState { get; set; }
        public void FrameUpdate()
        {
            if (!couldntOverride && targetBrain == null)
            {
                OnEnter(); //if our original override was successful, but targetbrain ends up becoming null,
                // that means the hunt weapon has been destroyed, so we need to assign a new one. if a new one
                // can't be assigned, couldntOverride will be set to true, and it wont try again.
                // this should only ever run for one frame thanks to the bool
            }
        }

        public void PhysicsUpdate()
        {
            
        }
        
        public void SetTargetWeapon(InteractableId weapon) // our "target weapon" is the one we want to hunt the player with
        {
            targetWeapon = weapon;
            targetBrain = targetWeapon.brain as WeaponBrain;
        }
        
        public void SetTargetWeapon(INTERACTABLE interactable)
        {//overloaded method to allow for different parameter types
            targetWeapon = _tankAI.tokenActivatedInteractables.First(x => x.brain.mySpecificType == interactable);
            targetBrain = targetWeapon.brain as WeaponBrain;
        }

        public void SwitchToken(InteractableId from, INTERACTABLE to)
        {
            _tankAI.RetrieveToken(from);
            var intId = _tankAI.DistributeToken(to); // this sets intID to the interactable that was distributed to
            interactableTakenFrom = from.brain.mySpecificType;
            tokenBorrowed = true;
            SetTargetWeapon(intId);
        }

        public void OnEnter()
        {
            Debug.Log("Hunt Entered");
            var players = GameObject.FindObjectsOfType<PlayerMovement>();
            var nearestPlayer = players.OrderBy(x => Vector3.Distance(x.transform.position, _tank.treadSystem.transform.position)).First();
            
            for (int i = 0; i < weaponPriorityList.Count; i++)
            {
                if (targetWeapon != null) break;
                var interactable = weaponPriorityList[i];
                if (_tankAI.CheckIfAvailable(interactable)) // if we have the interactable available
                {
                    //if we have an open token, and we have the gun, not being used, we should use it.
                    if (_tankAI.currentTokenCount > 0 && !_tankAI.CheckIfHasToken(interactable))
                    {
                        var intId = _tankAI.DistributeToken(interactable);
                        SetTargetWeapon(intId);
                        returnTokenLater = true;
                        break;
                    }
                    
                    if (_tankAI.CheckIfHasToken(interactable))
                    {
                        // Check for higher priority interactables. if any exist on this tank, unused, switch to it
                        for (int j = 0; j < i; j++)
                        {
                            var higherPriorityInteractable = weaponPriorityList[j];
                            if (_tankAI.CheckIfAvailable(higherPriorityInteractable))
                            {
                                var currentInteractable = _tankAI.tokenActivatedInteractables.First(x => x.brain.mySpecificType == interactable);
                                SwitchToken(currentInteractable, higherPriorityInteractable);
                                break;
                            }
                        }

                        if (targetWeapon == null) //nothing higher priority found, so use this one
                        {
                            SetTargetWeapon(interactable);
                            break;
                        
                        }
                    }
                    
                    
                }
            }

            if (targetWeapon == null)
            {
                foreach (var interactable in weaponPriorityList)
                { //if the last checks fail, see if we have a boiler we can redistribute from instead
                    if (_tankAI.CheckIfAvailable(interactable) && _tankAI.CheckIfHasToken(INTERACTABLE.Boiler))
                    {
                        var boiler = _tankAI.tokenActivatedInteractables.First(x =>
                            x.brain.mySpecificType == INTERACTABLE.Boiler);
                        SwitchToken(boiler, interactable);
                        break;
                    }
                }
            }
            
            
            targetBrain?.OverrideTargetPoint(nearestPlayer.transform);
            if (targetBrain != null)
            {
                if (targetBrain.updateAimTarget != null) targetBrain.StopCoroutine(targetBrain.updateAimTarget);
                targetBrain.updateAimTarget = targetBrain.StartCoroutine(targetBrain.UpdateTargetPoint(_tankAI.aiSettings.tankAccuracy));
            }
            
            if (targetBrain != null) targetBrain.updateAimTarget = targetBrain.StartCoroutine(targetBrain.UpdateTargetPoint(_tankAI.aiSettings.tankAccuracy));
            else couldntOverride = true;
        }

        public void OnExit()
        {
            Debug.Log("HuntPlayerSubstate OnExit");
            targetBrain?.ResetTargetPoint();
            if (returnTokenLater || tokenBorrowed)
            {
                _tankAI.RetrieveToken(targetWeapon);
                if (tokenBorrowed) _tankAI.DistributeToken(interactableTakenFrom);
                tokenBorrowed = false;
                returnTokenLater = false;
                couldntOverride = false;
            }
            targetWeapon = null;
        }
    }
}
