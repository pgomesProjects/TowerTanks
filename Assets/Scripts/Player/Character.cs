using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TowerTanks.Scripts
{
    public abstract class Character : SerializedMonoBehaviour, IDamageable
    {
        #region Fields and Properties

        public enum CharacterState { CLIMBING, NONCLIMBING, OPERATING }; //Simple state system, in the future this will probably be refactored
        public enum CharacterJobType { NONE, FIX, UNINSTALL, CLAIM }; //jobs characters are capable of executing

        public CharacterState currentState;                  //to an FSM.

        //Components
        protected Rigidbody2D rb;
        protected CapsuleCollider2D characterHitbox;
        protected GameObject currentLadder;
        protected PlayerHUD characterHUD;
        protected GameObject currentButtonPrompt;
        protected int characterIndex;
        protected Transform hands;
        protected Transform characterVisualParent;
        protected TaskProgressBar taskProgressBar;
        protected GameObject jetpackVisuals;
        protected ParticleSystem jetpackSmoke;

        protected Color characterColor = new Color(1, 1, 1, 1);

        [Header("Character Information")]
        [SerializeField] protected CharacterSettings characterSettings;
        protected float currentHealth;

        [FormerlySerializedAs("moveSpeed")]
        [Header("Movement")]
        [SerializeField] protected float groundMoveSpeed;
        protected float faceDirection = 1;

        protected float currentGroundMoveSpeed;
        [SerializeField] protected float horizontalAirSpeed;
        [SerializeField] protected float groundDeAcceleration;
        [SerializeField] protected float airDeAcceleration;
        [SerializeField] protected float jetpackForce;
        [SerializeField] protected float ladderClimbUpSpeed;
        [SerializeField] protected float ladderClimbDownSpeed;
        [SerializeField] protected float slipSlopeValue;
        [Range(0, 2)]
        [SerializeField] protected float groundedBoxX, groundedBoxY;

        [SerializeField] protected float groundedBoxOffset;
        
        [Header("Jetpack values")]
        [SerializeField] protected float fuelDepletionRate;
        [SerializeField] protected float fuelRegenerationRate;
        protected float currentFuel;

        [SerializeField] private float characterDeathParticleSize = 0.03f;
        [SerializeField] protected float respawnTime = 3f;
        private float currentRespawnTime;
        private bool isRespawning;

        private Transform dismountPoint;
        [SerializeField] 
        [Tooltip("The distance the player must be from the tank to fully dismount. (no more tank vel inheritance)")]
        private float tankDistanceToFullDismount;
        private bool softTankDismount; // if we have left the tank, but are still in it's vicinity
        private bool fullTankDismount; //if we have left the tank and have left it's vicinity

        //objects
        [Header("Interactables")]
        public InteractableZone currentZone = null;
        public bool isOperator; //true if player is currently operating an interactable
        public TankInteractable currentInteractable; //what interactable player is currently operating
        public LayerMask obstructionMask; //layers that block players from interacting with things
        
        /// <summary>
        /// Information about a task this character is performing.
        /// </summary>
        [System.Serializable]
        public class CharacterJob
        {
            [Tooltip("Type of job this is.")] public Character.CharacterJobType jobType = CharacterJobType.NONE;
            [Tooltip("Interactable associated with this job.")] public TankInteractable interactable;
            [Tooltip("JobZone associated with this job.")] public JobZone zone;
            [Tooltip("Time in seconds it takes for a character to complete this job.")] public float jobTime;

            public Vector2 GetJobPosition()
            {
                Vector2 position = new Vector2(0, 0);
                if (zone != null) { position = zone.jobSpot.position; }
                if (interactable != null) { position = interactable.transform.position; }

                return position;
            }

            public float GetJobTime()
            {
                float jobTime = 1f;

                switch (jobType)
                {
                    case CharacterJobType.FIX:
                        jobTime = 1.5f;
                        break;
                    case CharacterJobType.UNINSTALL:
                        jobTime = 1f;
                        break;
                    case CharacterJobType.CLAIM:
                        jobTime = 3f;
                        break;
                }

                return jobTime;
            }
        }

        [Header("Jobs")]
        public CharacterJob currentJob = null; //what interactable the character is currently fixing/breaking - NOT the same as currentInteractable

        [Header("Conditions")]
        [SerializeField] public bool isOnFire;
        protected float burnDamageRate = 1f;
        protected float burnDamageTimer = 0f;
        protected GameObject flames;
        protected bool isAlive;
        protected bool permaDeath;
        protected bool jetpackCanBeUsed;
        protected bool isCharacterLoaded;

        //internal movement
        protected Vector2 moveInput;
        private Transform lastCellJoint;
        private int cellLayerIndex = 15;
        
        protected LayerMask ladderLayer;

        private TankController assignedTank;
        
        protected List<Collider2D> currentOtherColliders = new List<Collider2D>();

        [Button(ButtonSizes.Medium)]
        private void DebugModifyPlayerHealth()
        {
            ModifyHealth(healthToModify);
        }

        [Button(ButtonSizes.Medium)]
        private void DebugKillPlayer()
        {
            KillCharacterImmediate();
        }
        private int healthToModify = -10;

        private Rigidbody2D lastFoundTankRb;

        #endregion

        #region Unity Methods
        protected virtual void Awake()
        {
            ladderLayer = 1 << LayerMask.NameToLayer("Ladder");
            rb = GetComponent<Rigidbody2D>();
            characterHitbox = GetComponent<CapsuleCollider2D>();
            currentHealth = characterSettings.maxHealth;
            characterVisualParent = transform.GetChild(0);
            hands = characterVisualParent.Find("Hands");
            flames = characterVisualParent.Find("flames").gameObject;
            jetpackVisuals = characterVisualParent.Find("jetpack").gameObject;
            jetpackSmoke = characterVisualParent.Find("smokeTrail").GetComponent<ParticleSystem>();
            flames.SetActive(false);
            jetpackVisuals.SetActive(false);
            jetpackSmoke.Stop();
        }

        protected virtual void Start()
        {
            ResetPlayer();
            taskProgressBar = GetComponent<TaskProgressBar>();
            isAlive = true;
            permaDeath = false;
            dismountPoint = transform;

            //If there is a radar camera in the scene, assign it to the offscreen tracker
            GameObject radarCam = GameObject.FindGameObjectWithTag("RadarCam");
            if (radarCam != null)
                GetComponent<OffscreenTracker>()?.AssignCamera(radarCam.GetComponent<Camera>());
        }

        protected virtual void OnEnable()
        {
            StartCoroutine(ResetAtEndOfFrame());
        }

        private bool useAirDrag;
        protected virtual void Update()
        {
            if (!isAlive)
            {
                if (isRespawning)
                    RespawnTimer();

                return;
            }
            
            currentFuel = Mathf.Clamp(currentFuel, 0, characterSettings.fuelAmount);
            var includeLayer = 1 << LayerMask.NameToLayer("Cell");
            Transform newCellJoint = Physics2D.OverlapBox(
                transform.position,
                transform.localScale * 1.5f,
                0f, 
                includeLayer)?.gameObject.transform;
            
            if (softTankDismount && !fullTankDismount) // if we left the tank, but we're still near the tank
            {
                var t = CheckSurfaceCollider(LayerMask.NameToLayer("Ground"));
                if (t != null)
                {
                    if (t.transform.parent.gameObject.layer != LayerMask.NameToLayer("Cell") && !CheckSurfaceCollider(LayerMask.NameToLayer("Coupler")))
                    {
                        FullyDismountTank();
                    }
                }
                else
                {
                    if (Vector3.Distance(transform.position, dismountPoint.position) > tankDistanceToFullDismount)
                    {
                        FullyDismountTank(lastFoundTankRb);
                    }
                }
                
            }

            if (lastCellJoint != newCellJoint) // will only run once every time a new cell is entered
            {
                EnterNewCell(newCellJoint);
                if (lastCellJoint != null && lastFoundTankRb != null && lastCellJoint.TryGetComponent<Cell>(out Cell cell)) lastFoundTankRb = cell.room.targetTank.treadSystem.r;
                lastCellJoint = newCellJoint;
            }
        }

        private void FullyDismountTank(Rigidbody2D tankRb = null)
        {
            Debug.Log("TankRB: " + tankRb + "  " + (tankRb ? tankRb.GetPointVelocity(transform.position) : "null"));
            if (tankRb)
            {
                rb.AddForce(tankRb.GetPointVelocity(transform.position) * rb.mass, ForceMode2D.Impulse);
                Debug.Log($"Force applied: {tankRb.GetPointVelocity(transform.position)}");
            }
                
            transform.SetParent(null, true);
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            fullTankDismount = true;

            //If the character is in the build scene and goes out of the tank, respawn them
            if (GameManager.Instance.currentSceneState == SCENESTATE.BuildScene)
            {
                RespawnPlayer();
            }
        }

        private void EnterNewCell(Transform cellToEnter)
        {
            if (cellToEnter != null) // we are still inside of a tank
            {
                rb.interpolation = RigidbodyInterpolation2D.None;
                transform.SetParent(cellToEnter, true);
                softTankDismount = false;
                fullTankDismount = false;
                if (!Mathf.Approximately(transform.eulerAngles.z, cellToEnter.eulerAngles.z))
                {
                    transform.rotation = Quaternion.Euler(new Vector3(0, 0, cellToEnter.eulerAngles.z));
                } //makes sure you're aligned with the new cell that you're entering
            }
            else
            {
                softTankDismount = true;
                fullTankDismount = false;
                dismountPoint = lastCellJoint;
                
                transform.rotation = Quaternion.identity; //player is always internally rotated with the tank,
                //so we need to reset the rotation when they leave a tank to avoid weirdness.
                //it's just the player's visual sprite which is always at 0 rotation
            }
        }
        
        [Button]
        private void RandomForce()
        {
            rb.AddForce(new Vector2(Random.Range(-10, 10), 10), ForceMode2D.Impulse);
        }

        protected virtual void FixedUpdate()
        {
            HandleConditions();

            if (currentState == CharacterState.NONCLIMBING) MoveCharacter();

            else if (currentState == CharacterState.CLIMBING) ClimbLadder();

            else if (currentState == CharacterState.OPERATING) OperateInteractable();
        }

        protected virtual void OnDrawGizmos()
        {
            //visualizes the grounded box for debugging
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(new Vector2(transform.position.x, transform.position.y - groundedBoxOffset), new Vector3(groundedBoxX, groundedBoxY, 0));
            if (!softTankDismount || fullTankDismount) return;
            Gizmos.color = Color.Lerp(Color.blue, Color.red, Mathf.InverseLerp(0, tankDistanceToFullDismount, Vector3.Distance(transform.position, dismountPoint ? dismountPoint.position : transform.position)));
            if (dismountPoint != null) Gizmos.DrawWireSphere(dismountPoint.position, tankDistanceToFullDismount);
            Gizmos.DrawWireCube(transform.position, transform.localScale);
            
        }

        /* TODO: Ladders are not triggers. Change this to use checksurfacecollider with the ladder layerindex
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
            {
                Debug.Log("ladder found");
                currentLadder = other.gameObject;
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
            {
                currentLadder = null;
            }
        }*/
        #endregion

        #region Movement

        public bool CheckGround()
        {
            LayerMask bothLayers = (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Coupler"));

            return Physics2D.OverlapBox(new Vector2(transform.position.x, //if our position is over that ground
                    transform.position.y - groundedBoxOffset),
                new Vector2(groundedBoxX, groundedBoxY),
                0f,
                bothLayers);

        }
        
        protected Collider2D CheckSurfaceCollider(int layer)
        {
            return Physics2D.OverlapBox(new Vector2(transform.position.x, //if our position is over that ground
                    transform.position.y - groundedBoxOffset),
                new Vector2(groundedBoxX, groundedBoxY),
                0f,
                1 << layer);
        }

        protected abstract void MoveCharacter();

        protected void PropelJetpack()
        {
            Vector2 jetpackVelocity = Vector2.up * ((jetpackForce * 10) * Time.fixedDeltaTime);

            rb.velocity = new Vector2(rb.velocity.x, jetpackVelocity.y);
        }

        protected void SetLadder()
        {
            if (currentLadder == null) return;
            currentState = CharacterState.CLIMBING;
            rb.bodyType = RigidbodyType2D.Kinematic;
            AlignPlayerWithLadder();
        }

        protected void AlignPlayerWithLadder()
        {
            if (currentLadder == null) return;
            // converts the player's position from world space to local space relative to the ladder
            // have to do this cause ladders are rotated sometimes
            transform.eulerAngles = currentLadder.transform.eulerAngles;
            Vector3 localPosition = currentLadder.transform.InverseTransformPoint(transform.position);

            // set the local x position to 0 bc ladder transform is in the center of the ladder
            localPosition.x = 0; 

            // Convert the updated local position back to world space
            Vector3 worldPosition = currentLadder.transform.TransformPoint(localPosition);

            transform.position = worldPosition;
        }

        protected virtual void ClimbLadder()
        {

        }

        protected virtual void OperateInteractable()
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public void CancelInteraction(bool startJump = false)
        {
            currentState = CharacterState.NONCLIMBING;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.velocity = Vector2.zero;
            if (startJump) rb.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
        }

        public virtual void LinkPlayerHUD(PlayerHUD newHUD)
        {
            characterHUD = newHUD;
            characterHUD.InitializeHUD(characterIndex);
        }
        #endregion

        #region Character Functions

        public void SetCharacterMovement(Vector2 movement)
        {
            moveInput = movement;
        }
        
        public Vector2 GetCharacterInput()
        {
            return moveInput;
        }

        public void ShowPromptOnHUD(GameAction gameAction)
        {
            ClearButtonPrompts();
            currentButtonPrompt = GameManager.Instance.UIManager.AddButtonPrompt(characterHUD.gameObject, new Vector2(-57.59997f, 17.79999f), 105f, gameAction, PlatformType.Gamepad, GameUIManager.PromptDisplayType.Button, false);
        }

        public void ClearButtonPrompts()
        {
            if (currentButtonPrompt != null)
                Destroy(currentButtonPrompt);
        }

        public float ModifyHealth(float amount)
        {
            return SetCharacterHealth(currentHealth + amount);
        }

        public float Damage(Projectile projectile, Vector2 position)
        {
            return ModifyHealth(-projectile.remainingDamage); //Reduce health by given amount of damage and return remainder
        }
        public float Damage(float damage, bool triggerHitEffects = false)
        {
            ModifyHealth(-damage); //Reduce health by given amount of damage
            return damage;
        }

        private float SetCharacterHealth(float characterHealth)
        {
            float tempHealth = currentHealth;

            currentHealth = Mathf.Clamp(characterHealth, 0f, characterSettings.maxHealth);

            float healthDif = tempHealth - currentHealth;

            //Shake the character HUD if they are taking damage
            if (healthDif > 0)
                characterHUD?.ShakePlayerHUD(0.25f, 7f);

            characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.25f);

            if (currentHealth <= 0)
            {
                bool hasTank = GetAssignedTank() != null;
                OnCharacterDeath(hasTank);
            }

            return healthDif;
        }

        public void KillCharacterImmediate()
        {
            currentHealth = 0;
            characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.01f);
            characterHUD?.ClearButtonPrompts();
            bool hasTank = GetAssignedTank() != null;
            OnCharacterDeath(hasTank);
        }

        public virtual void InterruptRespawn()
        {
            if (isRespawning)
            {
                isRespawning = false;
                PermaKill();
            }
        }

        protected virtual void OnCharacterDeath(bool respawn = false)
        {
            GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
            GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, characterDeathParticleSize, null);

            if (isOnFire) Extinguish();

            isRespawning = respawn;

            if (isRespawning)
            {
                characterHUD?.ShowRespawnTimer(true);
                currentRespawnTime = respawnTime;
            }
            else
            {
                characterHUD?.ShowRespawnTimer(false);
                PermaKill();
            }

            //TODO: (Ryan)
            //Needs to drop any objects/tools currently holding/equipped

            rb.isKinematic = true;
            characterHitbox.enabled = false;
            characterVisualParent?.gameObject.SetActive(false);
            isAlive = false;
            ResetPlayer();
        }

        protected virtual void PermaKill()
        {
            characterHUD?.KillPlayerHUD();
            permaDeath = true;
        }

        protected virtual void ResetPlayer()
        {
            currentHealth = characterSettings.maxHealth;
            currentFuel = characterSettings.fuelAmount;
            characterHUD?.UpdateFuelBar((currentFuel / characterSettings.fuelAmount) * 100f);
            characterHUD?.ClearButtonPrompts();
            currentState = CharacterState.NONCLIMBING;
            currentJob.jobType = CharacterJobType.NONE;

            if (currentInteractable != null)
            {
                currentInteractable.CancelUse();
                currentInteractable.Exit(true);
            }

            transform.parent = null;
            rb.velocity = Vector2.zero;
            moveInput = Vector2.zero;
        }

        protected virtual IEnumerator ResetAtEndOfFrame()
        {
            isCharacterLoaded = false;
            yield return new WaitForEndOfFrame();
            isCharacterLoaded = true;
        }

        private void RespawnTimer()
        {
            currentRespawnTime -= Time.deltaTime;

            if (currentRespawnTime <= 0f)
            {
                RespawnPlayer();
                isRespawning = false;
                isAlive = true;
            }

            characterHUD?.UpdateRespawnBar(1 - (currentRespawnTime / respawnTime), currentRespawnTime);
        }

        private void RespawnPlayer()
        {
            if (assignedTank != null)
                transform.position = assignedTank.GetPlayerSpawnPointPosition();

            characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.01f);
            characterHUD?.ShowRespawnTimer(false);
            rb.isKinematic = false;
            characterHitbox.enabled = true;
            characterVisualParent?.gameObject.SetActive(true);
            StartCoroutine(ResetAtEndOfFrame());
        }

        public Color GetCharacterColor() => characterColor;
        public TankController GetAssignedTank() => assignedTank;
        public void SetAssignedTank(TankController assignedTank) => this.assignedTank = assignedTank;

        protected void HandleConditions()
        {
            //Fire
            if (isOnFire) Burn();

            //TODO: (Ryan) Freeze
        }

        public bool IsDead() => !isAlive;
        public bool IsPermanentDead() => permaDeath;

        [Button(ButtonSizes.Medium)]
        public void Ignite()
        {
            isOnFire = true;
            flames.SetActive(true);
            burnDamageTimer = burnDamageRate;

            GameManager.Instance.AudioManager.Play("CoalLoad", this.gameObject);
        }

        private void Burn()
        {
            burnDamageTimer -= Time.deltaTime;
            if (burnDamageTimer <= 0)
            {
                burnDamageTimer = burnDamageRate;
                ModifyHealth(-1.0f);
            }
        }

        [Button(ButtonSizes.Medium)]
        public void Extinguish()
        {
            isOnFire = false;
            flames.SetActive(false);

            GameManager.Instance.AudioManager.Play("SteamExhaust", this.gameObject);
        }

        #endregion
        
        private void OnCollisionEnter2D(Collision2D other)
        {
            currentOtherColliders.Add(other.collider);
        }
        
        private void OnCollisionExit2D(Collision2D other)
        {
            currentOtherColliders.Remove(other.collider);
        }

        public PlayerHUD GetCharacterHUD() => characterHUD;
        public CharacterSettings GetCharacterSettings() => characterSettings;

        protected virtual void OnDestroy()
        {
            if(characterHUD != null)
                Destroy(characterHUD.gameObject);
        }
    }
}
