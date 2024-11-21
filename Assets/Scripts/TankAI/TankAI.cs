using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using System.Diagnostics;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class TankAI : MonoBehaviour
    {
        #region Transition Conditions
        public bool TargetInViewRange() =>      Vector2.Distance(_tank.treadSystem.transform.position, _tankManager.playerTank.transform.position) < aiSettings.viewRange;
        public bool TargetOutOfView() =>        Vector2.Distance(_tank.treadSystem.transform.position, _tankManager.playerTank.transform.position) > aiSettings.viewRange;
        public bool TargetInEngageRange() =>    Vector2.Distance(_tank.treadSystem.transform.position, _tankManager.playerTank.transform.position) < aiSettings.maxEngagementRange;
        public bool TargetOutOfEngageRange() => Vector2.Distance(_tank.treadSystem.transform.position, _tankManager.playerTank.transform.position) > aiSettings.maxEngagementRange;
        public bool TargetTooClose() =>         Vector2.Distance(_tank.treadSystem.transform.position, _tankManager.playerTank.transform.position) < aiSettings.minEngagementRange;
        bool NoGuns() => !_tank.interactableList.Any(i => i.script is GunController);
        #endregion
        
        public static Dictionary<INTERACTABLE, Type> interactableEnumToBrainMap = new()
        {
            {INTERACTABLE.Cannon, typeof(SimpleCannonBrain)},
            {INTERACTABLE.Mortar, typeof(SimpleMortarBrain)},
            {INTERACTABLE.MachineGun, typeof(SimpleMachineGunBrain)},
            {INTERACTABLE.Throttle, typeof(InteractableBrain)},
        };
        
        private StateMachine fsm;
        private TankController _tank, targetTank;
        private TankManager _tankManager;
        private GunController[] _guns;
        private int currentTokenCount;
        [HideInInspector] public List<InteractableId> tokenActivatedInteractables = new List<InteractableId>();
        private Vector3 movePoint;

        [Header("AI Configuration")]
        public TankAISettings aiSettings;

        private void Awake()
        {
            _tank = GetComponent<TankController>();
            
            currentTokenCount = aiSettings.tankEconomy;
            _tankManager = FindObjectOfType<TankManager>();
        }
        
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.1f); // AI Waits before initialization to give time for any tank room generation

            fsm = new StateMachine();
            var patrolState = new TankPatrolState(this);
            var pursueState = new TankPursueState(this);
            var engageState = new TankEngageState(this);
            var surrenderState = new TankSurrenderState(this);
            
            
            //bool condition if there are no guncontrollers in _tank
            
            
            void At(IState from, IState to, Func<bool> condition) => fsm.AddTransition(from, to, condition);
            void AnyAt(IState to, Func<bool> condition) => fsm.AddAnyTransition(to, condition);
            
            //patrol state transitions
            At(patrolState, pursueState, TargetInViewRange);
            At(pursueState, patrolState ,TargetOutOfView);
            //pursue state transitions
            At(pursueState, engageState ,TargetInEngageRange);
            At(engageState, pursueState ,TargetOutOfEngageRange);
            AnyAt(surrenderState, NoGuns); //this being an "any transition" means that it can be triggered from any state
            
            fsm.SetState(patrolState);
        }

        public bool HasActiveThrottle()
        {
            //see if token activated interactables has a throttle interactable in it
            return tokenActivatedInteractables.Any(i => i.brain.GetType() == interactableEnumToBrainMap[INTERACTABLE.Throttle]);
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
            if (fsm == null) return;
            fsm.FrameUpdate();
            //var tanks = Physics2D.OverlapCircleAll(transform.position, aiSettings.viewRange, 1 << LayerMask.NameToLayer("Treads"));
        }

        private void FixedUpdate()
        {
            if (fsm == null) return;
            fsm.PhysicsUpdate();
        }

        #region Tank AI Token System
        
        public void DistributeToken(INTERACTABLE toInteractable)
        {
            if (currentTokenCount <= 0) return;
            
            List<InteractableId> commonInteractables = _tank.interactableList
                .Where(i => i.brain != null && i.brain.GetType() == interactableEnumToBrainMap[toInteractable]
                && !tokenActivatedInteractables.Contains(i))
                .ToList();
            //list of all interactables which have an AI, 

            if (!commonInteractables.Any()) return; //if there are no interactables of this type, return
            InteractableId interactable = commonInteractables[Random.Range(0, commonInteractables.Count)];
            
            
            //uses the Where LINQ method to find the interactables in the list whose type matches the toInteractable type, who have a valid AI, and who are not already active
            //i => is a lambda expression that checks if the type of the interactable i is equal to the toInteractable type. also makes sure the interactable isnt already added
            
            
            if (interactable != null)
            {
                tokenActivatedInteractables.Add(interactable);
                interactable.ReceiveToken(this);
                interactable.brain.myInteractableType = toInteractable; //nice way to tell the brain what it is automatically
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
                interactableToTakeFrom.ReturnToken();
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
        
        public void DistributeAllWeightedTokens(Dictionary<INTERACTABLE, float> weights)
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
                for (int i = 0; i < tokenDistribution.Value; i++)
                {
                    DistributeToken(tokenDistribution.Key);
                }
            }
            
        }


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
        
        public bool TankIsRightOfTarget()
        {
            return _tank.treadSystem.transform.position.x > targetTank.treadSystem.transform.position.x;
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
                _tank.SetTankGear(s, .15f);
            }
            else
            {
                _tank.SetTankGear(-s, .15f);
            }
        }

        #endregion


        private void OnDrawGizmos()
        {
            //draw circle for view range
            if (!enabled) return;
            Gizmos.color = Color.red;
            if (_tank == null) return;
            Vector3 tankPos = _tank.treadSystem.transform.position;
            Gizmos.DrawWireSphere(tankPos, aiSettings.viewRange);
            int i = 0;
            foreach (var interactable in tokenActivatedInteractables)
            {
                //draw a box for each of their bounds
                Gizmos.color = Color.blue;
                Bounds bnds = interactable.script.thisCollider.bounds;
                Gizmos.DrawWireCube(interactable.script.transform.position, bnds.size);
                GUIStyle style = new GUIStyle();
                style.fontSize = 20; // Set the desired font size
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                Handles.Label(interactable.script.transform.position + Vector3.up * 2, $"{i}", style:style);
                style.normal.textColor = Color.cyan;
                Handles.Label(_tank.treadSystem.transform.position + Vector3.up * 25, $"AI STATE: {fsm._currentState.GetType().Name}", style:style);
                style.normal.textColor = Color.yellow;
                Handles.Label(_tank.treadSystem.transform.position + Vector3.up * 22, $"Available Tokens: {currentTokenCount}", style:style);
                style.normal.textColor = Color.red;
                Handles.Label(_tank.treadSystem.transform.position + Vector3.up * 19, $"Total Tokens: {aiSettings.tankEconomy}", style:style);
                i++;
            }
            
            //draw circle for engagement range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tankPos, aiSettings.maxEngagementRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tankPos, aiSettings.minEngagementRange);
        }

        private void OnDestroy()
        {
            StackTrace stackTrace = new StackTrace(true);
            UnityEngine.Debug.Log($"Component {GetType().Name} on GameObject {gameObject.name} is being destroyed. Stack trace:\n{stackTrace}", this);
        }
    }
}
