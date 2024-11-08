using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TankAI : MonoBehaviour
    {
        private StateMachine _stateMachine;
        private TankController _tank, targetTank;
        private TankManager _tankManager;
        private GunController[] _guns;
        private int currentTokenCount;
        private List<InteractableId> tokenActivatedInteractables = new List<InteractableId>();
        private Vector3 movePoint;

        [Header("AI Configuration")]
        public TankAISettings aiSettings;

        private void Awake()
        {
            _tank = GetComponent<TankController>();
            if (_tank.tankType == TankId.TankType.PLAYER)
            {
                Destroy(this);
                return;
            }
            currentTokenCount = aiSettings.tankEconomy;
            _tankManager = FindObjectOfType<TankManager>();
        }
        
        private void Start()
        {
            _stateMachine = new StateMachine();
            var patrolState = new TankPatrolState(this);
            var pursueState = new TankPursueState(this);
            var engageState = new TankEngageState(this);
            var surrenderState = new TankSurrenderState(this);
            
            Debug.Log($"PlayerInViewRange: {Vector2.Distance(_tank.treadSystem.transform.position, _tankManager.playerTank.transform.position)}");
            bool PlayerInViewRange() => Vector2.Distance(_tank.treadSystem.transform.position, _tankManager.playerTank.transform.position) < aiSettings.viewRange;
            bool TargetInEngagementRange() => Vector2.Distance(_tank.transform.position, targetTank.transform.position) < aiSettings.engagementRange;
            
            
            void At(IState from, IState to, Func<bool> condition) => _stateMachine.AddTransition(from, to, condition);
            
            //patrol state transitions
            At(patrolState, pursueState, PlayerInViewRange);
            
            //pursue state transitions
            At(pursueState, engageState ,TargetInEngagementRange);
            _stateMachine.SetState(patrolState);
        }

        public void SetTarget(TankController tank)
        {
            targetTank = tank;
        }

        public TankController GetTarget()
        {
            return targetTank;
        }

        private void Update()
        {
            _stateMachine.FrameUpdate();
            var tanks = Physics2D.OverlapCircleAll(transform.position, aiSettings.viewRange, 1 << LayerMask.NameToLayer("Treads"));
        }

        private void FixedUpdate()
        {
            _stateMachine.PhysicsUpdate();
        }

        #region Tank AI Token System
        
        public void DistributeToken(Type toInteractable)
        {
            if (currentTokenCount <= 0) return;
            
            
            InteractableId interactable = _tank.interactableList
                .FirstOrDefault(i => i.brain.GetType() == toInteractable && !tokenActivatedInteractables.Contains(i));
            //uses the FirstOrDefault LINQ method to find the first interactable in the list whose type matches the toInteractable type.
            //i => is a lambda expression that checks if the type of the interactable i is equal to the toInteractable type. also makes sure the interactable isnt already added
            
            
            if (interactable != null && !tokenActivatedInteractables.Contains(interactable))
            {
                tokenActivatedInteractables.Add(interactable);
                interactable.ReceiveToken(this);
                currentTokenCount--;
            }
            else
            {
                Debug.LogError("Could not distribute token: Interactable already in use, or not found");
            }
        }

        public void RetrieveToken(InteractableId interactableToTakeFrom)
        {
            if (tokenActivatedInteractables.Contains(interactableToTakeFrom))
            {
                tokenActivatedInteractables.Remove(interactableToTakeFrom);
                interactableToTakeFrom.tokenActivated = false;
                currentTokenCount++;
            }
            
        }
        
        /*public void DistributeAllWeightedTokens(Dictionary<TankInteractable.InteractableType, float> weights)
        {
            float totalWeight = weights.Values.Sum();
            
            // scales weights to add up to 100%
            if (totalWeight != 100)
            {
                // the ratio to squash or stretch weights to a total of 100
                float ratio = 100 / totalWeight;

                // adjusts each weight accordingly
                foreach (var key in weights.Keys.ToList())
                {
                    weights[key] *= ratio;
                }
            }
            
            // Calculate tokens for each type based on weight percentage
            Dictionary<TankInteractable.InteractableType, int> tokensToDistribute = new Dictionary<TankInteractable.InteractableType, int>();
            foreach (var weight in weights)
            {
                int tokens = Mathf.FloorToInt((weight.Value / 100) * currentTokenCount);
                tokensToDistribute[weight.Key] = tokens;
            }
            
            int totalTokensToDistribute = tokensToDistribute.Values.Sum();

            /#1#/ if we have too many tokens to distribute, take away from the type with the most weight
            while (totalTokensToDistribute > currentTokenCount)
            { 
                var maxWeightType = tokensToDistribute.OrderByDescending(kvp => weights[kvp.Key]).First().Key; //orders the key-value pairs (kvp) in the tokensToDistribute dictionary in descending order based on the weight values from the weights dictionary. The kvp.Key is used to access the corresponding weight in the weights dictionary.
                tokensToDistribute[maxWeightType]--;
                totalTokensToDistribute--;
            }
            
            // If we have leftover tokens to distribute (this happens when tokens are lost from float rounding), distribute them.
            while (totalTokensToDistribute < currentTokenCount)
            {
                var remainingTokens = currentTokenCount - totalTokensToDistribute;
                var unusedInteractables = GetUnusedInteractables()
                    .Where(i => weights.ContainsKey(i.type))
                    .ToList();

                if (unusedInteractables.Count > 0)
                {
                    foreach (var weight in weights)
                    {
                        var type = weight.Key;
                        var percentage = weight.Value / 100;
                        var tokensToAdd = Mathf.FloorToInt(remainingTokens * percentage);

                        if (tokensToAdd > 0 && unusedInteractables.Any(i => i.type == type))
                        {
                            tokensToDistribute[type] += tokensToAdd;
                            totalTokensToDistribute += tokensToAdd;

                            if (totalTokensToDistribute >= currentTokenCount)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }#1#


            foreach (var tokenDistribution in tokensToDistribute)
            {
                var interactables = _tank.interactableList.Select(i => i.script)
                    .Where(i => i.interactableType == tokenDistribution.Key)
                    .ToList();

                for (int i = 0; i < tokenDistribution.Value && i < interactables.Count; i++)
                {
                    DistributeToken(interactables[i].GetType());
                }
            }
            
        }*/


        #endregion
        
        
        #region Tank AI Functionality Methods
        
        /// <summary>
        /// Will set the tank's throttle towards it's set movepoint value.
        /// </summary>
        /// <param name="speedSetting">
        /// The speed setting to set the throttle to.
        /// </param>
        public void ChangeThrottleTowardsMovepoint(int speedSetting)
        {
            int dir = _tank.transform.position.x < movePoint.x ? 1 : -1;
            _tank.SetTankGear(speedSetting, .1f);
        }
        
        public void SetMovePoint(Vector3 point)
        {
            movePoint = point;
        }
        
        public List<InteractableId> GetUnusedInteractables()
        {
            return _tank.interactableList
                .Where(i => !tokenActivatedInteractables.Contains(i))
                .ToList();
        }

        #endregion


        private void OnDrawGizmos()
        {
            //draw circle for view range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_tank.treadSystem.transform.position, aiSettings.viewRange);
            
            //draw circle for engagement range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_tank.treadSystem.transform.position, aiSettings.engagementRange);
        }
    }
}
