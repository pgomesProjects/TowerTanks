using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class TankAI : MonoBehaviour
    {
        #region Transition Conditions
        
        public float GetDistanceToTarget() => Vector2.Distance(tank.treadSystem.transform.position, targetTank.treadSystem.transform.position);
        
        public bool TargetInViewRange() =>      targetTank != null &&
                                                GetDistanceToTarget() < aiSettings.viewRange + viewRangeOffset;
        public bool TargetOutOfView() =>        targetTank == null ||
                                                GetDistanceToTarget() > aiSettings.viewRange + viewRangeOffset;
        public bool TargetInEngageRange() =>    targetTank != null &&
                                                GetDistanceToTarget() < aiSettings.maxEngagementRange;
        public bool TargetOutOfEngageRange() => targetTank == null ||
                                                GetDistanceToTarget() > aiSettings.maxEngagementRange;
        public bool TargetTooClose() =>         targetTank != null &&
                                                GetDistanceToTarget() < aiSettings.preferredFightDistance;
        public bool TargetAtFightingDistance() => targetTank != null &&
                                                    Mathf.Abs(GetDistanceToTarget() - aiSettings.preferredFightDistance) <= 5; 
        bool NoGuns() => !tank.interactableList.Any(i => i.script is GunController);
        
        //func bool to check if there are any objects of NewPlayerMovement near tank.treadsystem.transform.position
        public bool PlayerNearby() => Physics2D.OverlapCircleAll(tank.treadSystem.transform.position, 40f)
            .Any(collider => collider.GetComponent<PlayerMovement>() != null);
        
        public bool NoPlayerNearby() => !Physics2D.OverlapCircleAll(tank.treadSystem.transform.position, 40f)
            .Any(collider => collider.GetComponent<PlayerMovement>() != null);
        
        #endregion
        
        public StateMachine fsm;
        [HideInInspector] public TankController tank;
        [HideInInspector] public TankController targetTank;
        private GunController[] _guns;
        private int currentTokenCount;
        [HideInInspector] public List<InteractableId> tokenActivatedInteractables = new List<InteractableId>();
        private Vector3 movePoint;

        [Header("AI Configuration")]
        public TankAISettings aiSettings;
        private bool alerted; //AI is alerted when taking damage from a player-source for the first time
        private float viewRangeOffset = 0;
        public PlayerMovement[] players;

        private void Awake()
        {
            tank = GetComponent<TankController>();
        }
        
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.1f); // AI Waits before initialization to give time for any tank room generation
            
            currentTokenCount = aiSettings.tankEconomy;
            fsm = new StateMachine();
            /////////////// State Instance Creation ///////////////
            var patrolState = new TankPatrolState(this);
            var pursueState = new TankPursueState(this);
            var engageState = new TankEngageState(this);
            var surrenderState = new TankSurrenderState(this);
            /////////////// Substate Instance Creation ////////////
            var huntSubState = new HuntPlayerSubstate(this);
            //////////////////////////////////////////////////////
            
            void At(IState from, IState to, Func<bool> condition) => fsm.AddTransition(from, to, condition);
            void AnyAt(IState to, Func<bool> condition) => fsm.AddAnyTransition(to, condition);
            
            
            At(patrolState, pursueState, TargetInViewRange);
            At(pursueState, engageState ,TargetInEngageRange);
            At(engageState, pursueState ,TargetOutOfEngageRange);
            At(pursueState, patrolState ,TargetOutOfView);

            AnyAt(surrenderState, NoGuns); //this being an "any transition" means that it can be triggered from any state
            
            fsm.AddSubstate(patrolState, huntSubState, PlayerNearby, () => !PlayerNearby());
            fsm.AddSubstate(pursueState, huntSubState, PlayerNearby, () => !PlayerNearby());
            fsm.AddSubstate(engageState, huntSubState, PlayerNearby, () => !PlayerNearby());

            fsm.SetState(patrolState);
        }

        public bool HasActiveThrottle()
        {
            //see if token activated interactables has a throttle interactable in it
            return tokenActivatedInteractables.Any(i => i.brain.GetType() == InteractableLookups.enumToBrainMap[INTERACTABLE.Throttle]);
        }
        
        public void SetClosestTarget()
        {
            targetTank = TankManager.instance.tanks
                .Where(tankId => tankId.tankScript != tank && tankId.tankType != TankId.TankType.NEUTRAL)
                .OrderBy(tankId => Vector2.Distance(tank.treadSystem.transform.position, tankId.tankScript.treadSystem.transform.position))
                .FirstOrDefault()?.tankScript;
            
        }

        [Button]
        public TankController GetTarget()
        {
            Debug.Log($"{tank.name}'s target tank is: {targetTank.name}");
            return targetTank;
        }

        private void Update()
        {
            if (fsm == null) return;
            if (targetTank == null) SetClosestTarget();
            fsm.FrameUpdate();
        }

        private void FixedUpdate()
        {
            if (fsm == null) return;
            fsm.PhysicsUpdate();
        }

        #region Tank AI Token System
        
        public void DistributeToken(INTERACTABLE toInteractable)
        {
            if (currentTokenCount <= 0)
            {
                return;
            }

            List<InteractableId> commonInteractables = tank.interactableList
                .Where(i => i.brain != null && i.brain.GetType() == InteractableLookups.enumToBrainMap[toInteractable]
                                            && !tokenActivatedInteractables.Contains(i))
                .ToList();

            if (!commonInteractables.Any())
            {
                return;
            }

            InteractableId interactable = commonInteractables[Random.Range(0, commonInteractables.Count)];

            if (interactable != null)
            {
                tokenActivatedInteractables.Add(interactable);
                interactable.brain.enabled = true;
                interactable.brain.ReceiveToken();
                interactable.brain.mySpecificType = toInteractable;
                interactable.brain.Init();
                currentTokenCount--;
            }

        }

        public void RetrieveToken(InteractableId interactableToTakeFrom)
        {
            if (tokenActivatedInteractables.Contains(interactableToTakeFrom))
            {
                tokenActivatedInteractables.Remove(interactableToTakeFrom);
                interactableToTakeFrom.brain.ReturnToken();
                currentTokenCount++;
            }
            
        }

        public bool CheckIfHasToken(INTERACTABLE interactable)
        {
            if (tokenActivatedInteractables.Any(i => i.brain.mySpecificType == interactable))
            {
                return true;
            }
            return false;
        }

        public bool CheckIfAvailable(INTERACTABLE interactable)
        {
            // if there is one of these in the interactable list and it is not in the token activated interactables list
            return tank.interactableList.Any(i => i.brain.mySpecificType == interactable);
        }

        public bool CheckIfTypeHasToken(TankInteractable.InteractableType interactableType)
        {
            List<INTERACTABLE> ints = InteractableLookups.typesInGroup[interactableType];
            foreach (INTERACTABLE interactable in ints)
            {
                if (tokenActivatedInteractables.Any(i => i.brain.mySpecificType == interactable))
                {
                    return true;
                }
            }
            return false;
        }
        
        public void RetrieveAllTokens()
        {
            // Create a temporary list to store interactables to be removed.
            // This avoids removing elements from the same list that we are iterating over,
            // which would cause an exception.
            List<InteractableId> interactablesToRemove = new List<InteractableId>(tokenActivatedInteractables);

            // Iterate over the temporary list and remove interactables from the original list
            foreach (var interactable in interactablesToRemove)
            {
                RetrieveToken(interactable);
            }
            
        }
        
        public void DistributeAllWeightedTokens(Dictionary<INTERACTABLE, float> weights)
        {
            var tokensToDistribute = aiSettings.GetTokenDistribution(weights);

            foreach (var tokenDistribution in tokensToDistribute)
            {
                // Check if the interactable type is present on the tank
                bool interactablePresent = tank.interactableList.Any(i => i.brain != null &&
                                                                         i.brain.mySpecificType == tokenDistribution.Key &&
                                                                         !tokenActivatedInteractables.Contains(i));

                if (interactablePresent)
                {
                    for (int i = 0; i < tokenDistribution.Value; i++)
                    {
                        DistributeToken(tokenDistribution.Key);
                    }
                }
                else // Distribute the token to a different interactable of the same classification
                {
                    // Get the group of the current interactable type
                    var group = InteractableLookups.typeToGroupMap[tokenDistribution.Key];

                    // Get the list of interactables in the same group
                    var groupInteractables = InteractableLookups.typesInGroup[group];

                    // Find a suitable interactable in the same group that is present on the tank
                    for (int i = 0; i < tokenDistribution.Value; i++)
                    {
                        var suitableInteractable = groupInteractables
                            .FirstOrDefault(interactable => tank.interactableList.Any(i => i.brain != null && i.brain.mySpecificType == interactable && !tokenActivatedInteractables.Contains(i)));
                        if (suitableInteractable != default) DistributeToken(suitableInteractable);
                    }
                }
            }
        }


        #endregion
        
        
        #region Tank AI Functionality Methods
        
        public List<InteractableId> GetUnusedInteractables()
        {
            return tank.interactableList
                .Where(i => !tokenActivatedInteractables.Contains(i))
                .ToList();
        }
        
        public PlayerMovement GetNearestPlayer()
        {
            
            return players.OrderBy(x => Vector3.Distance(x.transform.position, tank.treadSystem.transform.position)).First();
        }
        
        public bool TankIsRightOfTarget()
        {
            if (targetTank == tank || targetTank == null || tank == null)
            {
                return true;
            }
            return tank.treadSystem.transform.position.x > targetTank.treadSystem.transform.position.x;
        }
        
        

        /// <summary>
        /// Will randomly set the tank's throttle to go left or right.
        /// </summary>
        /// <param name="speedSetting">
        /// The speed setting to set the throttle to.
        /// </param>
        public void MoveRandom(int speedSetting)
        {
            int s = Mathf.Abs(speedSetting);
            if (s == 0)
            {
                Debug.LogError("Speed setting cannot be 0 in the MoveRandom Function");
                return;
            }
            
            if (Random.Range(0, 2) == 1)
            {
                tank.SetTankGearOverTime(s, .15f);
            }
            else
            {
                tank.SetTankGearOverTime(-s, .15f);
            }
        }

        public void TriggerAlerted()
        {
            if (alerted) return;
            if (aiSettings != null)
            {
                Debug.Log("HEY! WHAT THE-");
                StartCoroutine(Alert(5f));
            }
        }

        private IEnumerator Alert(float duration)
        {
            alerted = true;
            viewRangeOffset += 50f;
            yield return new WaitForSeconds(duration);
            viewRangeOffset -= 50f;
            alerted = false;
        }

        #endregion

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            //draw circle for view range
            if (fsm == null) return;
            if (!enabled) return;
            Gizmos.color = Color.red;
            if (tank == null) return;
            Vector3 tankPos = tank.treadSystem.transform.position;
            if (fsm._currentState.GetType() != typeof(TankPursueState)) Gizmos.DrawWireSphere(tankPos, aiSettings.viewRange);
            GUIStyle style = new GUIStyle();
            int i = 0;
            foreach (var interactable in tokenActivatedInteractables)
            {
                //draw a box for each of their bounds
                Gizmos.color = Color.blue;
                Bounds bnds = interactable.script.thisCollider.bounds;
                Gizmos.DrawWireCube(interactable.script.transform.position, bnds.size);
                
                style.fontSize = 20; 
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                Handles.Label(interactable.script.transform.position + Vector3.up * 2, $"{i}", style: style);
                i++;
            }
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.cyan;
            Handles.Label(tank.treadSystem.transform.position + Vector3.up * 25, $"AI STATE: {fsm._currentState.GetType().Name}", style: style);
            if (fsm._currentSubState != null)
            {
                style.normal.textColor = Color.green;
                Handles.Label(tank.treadSystem.transform.position + Vector3.up * 29, $"SUBSTATE: {fsm._currentSubState.GetType().Name}",
                    style: style);
            }
            style.normal.textColor = Color.yellow;
            Handles.Label(tank.treadSystem.transform.position + Vector3.up * 22, $"Available Tokens: {currentTokenCount}", style: style);
            style.normal.textColor = Color.red;
            Handles.Label(tank.treadSystem.transform.position + Vector3.up * 19, $"Total Tokens: {aiSettings.tankEconomy}", style: style);

            //draw circle for engagement range
            Gizmos.color = Color.yellow;
            if (fsm != null) { if (fsm._currentState.GetType() != typeof(TankEngageState)) Gizmos.DrawWireSphere(tankPos, aiSettings.maxEngagementRange); }
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tankPos, aiSettings.preferredFightDistance);
        }
#endif
    }
}
