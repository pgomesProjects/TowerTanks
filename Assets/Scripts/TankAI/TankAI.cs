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
        
        private float GetDistanceToTarget() => Vector2.Distance(tank.treadSystem.transform.position, targetTank.treadSystem.transform.position);
        
        public bool TargetInViewRange() =>      targetTank != null &&
                                                GetDistanceToTarget() < aiSettings.viewRange;
        public bool TargetOutOfView() =>        targetTank == null ||
                                                GetDistanceToTarget() > aiSettings.viewRange;
        public bool TargetInEngageRange() =>    targetTank != null &&
                                                GetDistanceToTarget() < aiSettings.maxEngagementRange;
        public bool TargetOutOfEngageRange() => targetTank == null ||
                                                GetDistanceToTarget() > aiSettings.maxEngagementRange;
        public bool TargetTooClose() =>         targetTank != null &&
                                                GetDistanceToTarget() < aiSettings.preferredFightDistance;
        public bool TargetAtFightingDistance() => targetTank != null &&
                                                    Mathf.Abs(GetDistanceToTarget() - aiSettings.preferredFightDistance) <= 25; //we are within 3 units of our preferred fighting distance (we are at our preferred distance)
        bool NoGuns() => !tank.interactableList.Any(i => i.script is GunController);
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

        private void Awake()
        {
            tank = GetComponent<TankController>();
            
            currentTokenCount = aiSettings.tankEconomy;

        }
        
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.1f); // AI Waits before initialization to give time for any tank room generation
            fsm = new StateMachine();
            var patrolState = new TankPatrolState(this);
            var pursueState = new TankPursueState(this);
            var engageState = new TankEngageState(this);
            var surrenderState = new TankSurrenderState(this);
            
            void At(IState from, IState to, Func<bool> condition) => fsm.AddTransition(from, to, condition);
            void AnyAt(IState to, Func<bool> condition) => fsm.AddAnyTransition(to, condition);
            
            
            At(patrolState, pursueState, TargetInViewRange);
            At(pursueState, engageState ,TargetInEngageRange);
            At(engageState, patrolState ,TargetOutOfView);
            At(pursueState, patrolState ,TargetOutOfView);

            AnyAt(surrenderState, NoGuns); //this being an "any transition" means that it can be triggered from any state
            
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
        
        public void DistributeAllWeightedTokens(Dictionary<INTERACTABLE, float> weights) // this can definitely be optimized. will do later
        {
            float totalWeight = weights.Values.Sum();
            
            // scales weights to add up to 100%
            // (if they are under 100% it just won't distribute 100% of the tank's tokens)
            if (totalWeight > 100)
            {
                float ratio = 100 / totalWeight;
                
                foreach (var key in weights.Keys.ToList())
                {
                    weights[key] *= ratio;
                }
            }
            
            // Calculate tokens for each type based on weight percentage
            Dictionary<INTERACTABLE, int> tokensToDistribute = new Dictionary<INTERACTABLE, int>();
            foreach (var weight in weights)
            {
                int tokens = Mathf.RoundToInt((weight.Value / 100) * currentTokenCount);
                tokensToDistribute[weight.Key] = tokens;
                if (weight.Key == INTERACTABLE.Throttle && tokens > 0) tokensToDistribute[weight.Key] = 1; //doesn't make sense to distribute more than 1 throttle token
                Debug.Log($"Distributing {tokens} tokens to {weight.Key}");
            }
            
            int totalTokensToDistribute = tokensToDistribute.Values.Sum();

            //#1#/ if we have too many tokens to distribute, take away from the type with the most weight
            while (totalTokensToDistribute > currentTokenCount)
            { 
                var maxWeightType = tokensToDistribute.OrderByDescending(kvp => weights[kvp.Key]).First().Key; //orders the key-value pairs (kvp) in the tokensToDistribute dictionary in descending order based on the weight values from the weights dictionary. The kvp.Key is used to access the corresponding weight in the weights dictionary.
                tokensToDistribute[maxWeightType]--;
                totalTokensToDistribute--;
                Debug.Log($"Extra token was generated, removing extra token from {maxWeightType}");
            }


            foreach (var tokenDistribution in tokensToDistribute)
            {
                // Check if the interactable type is present on the tank
                bool interactablePresent = tank.interactableList.Any(i => i.brain != null &&
                                                                                 i.brain.mySpecificType == tokenDistribution.Key &&
                                                                                 !tokenActivatedInteractables.Contains(i));

                if (interactablePresent)
                {
                    Debug.Log($"Interactable present: {tokenDistribution.Key}");
                    for (int i = 0; i < tokenDistribution.Value; i++)
                    {
                        DistributeToken(tokenDistribution.Key);
                    }
                }
                else //distribute the token to a different interactable of the same classification
                {
                    // Get the group of the current interactable type
                    var group = InteractableLookups.typeToGroupMap[tokenDistribution.Key];

                    // Get the list of interactables in the same group
                    var groupInteractables = InteractableLookups.typesInGroup[group];

                    // Find a suitable interactable in the same group that is present on the tank
                    for (int i = 0; i < tokenDistribution.Value; i++)
                    {
                        var suitableInteractable = groupInteractables
                            .FirstOrDefault(interactable => tank.interactableList.Any(i => i.brain != null && i.brain.mySpecificType == interactable && !tokenActivatedInteractables.Contains(i))); //any open interactable in the same group
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
        
        public bool TankIsRightOfTarget()
        {
            return tank != null && tank.treadSystem.transform.position.x > targetTank.treadSystem.transform.position.x;
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

        #endregion


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            //draw circle for view range
            if (!enabled) return;
            Gizmos.color = Color.red;
            if (tank == null) return;
            Vector3 tankPos = tank.treadSystem.transform.position;
            if (fsm != null) { if (fsm._currentState.GetType() != typeof(TankPursueState)) Gizmos.DrawWireSphere(tankPos, aiSettings.viewRange); }
            int i = 0;
            foreach (var interactable in tokenActivatedInteractables)
            {
                //draw a box for each of their bounds
                Gizmos.color = Color.blue;
                Bounds bnds = interactable.script.thisCollider.bounds;
                Gizmos.DrawWireCube(interactable.script.transform.position, bnds.size);
                GUIStyle style = new GUIStyle();
                style.fontSize = 20; 
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                Handles.Label(interactable.script.transform.position + Vector3.up * 2, $"{i}", style: style);
                style.normal.textColor = Color.cyan;
                Handles.Label(tank.treadSystem.transform.position + Vector3.up * 25, $"AI STATE: {fsm._currentState.GetType().Name}", style: style);
                style.normal.textColor = Color.yellow;
                Handles.Label(tank.treadSystem.transform.position + Vector3.up * 22, $"Available Tokens: {currentTokenCount}", style: style);
                style.normal.textColor = Color.red;
                Handles.Label(tank.treadSystem.transform.position + Vector3.up * 19, $"Total Tokens: {aiSettings.tankEconomy}", style: style);
                i++;
            }

            //draw circle for engagement range
            Gizmos.color = Color.yellow;
            if (fsm != null) { if (fsm._currentState.GetType() != typeof(TankEngageState)) Gizmos.DrawWireSphere(tankPos, aiSettings.maxEngagementRange); }
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tankPos, aiSettings.preferredFightDistance);
        }
#endif
    }
}
