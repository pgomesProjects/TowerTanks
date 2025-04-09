using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace TowerTanks.Scripts
{
    public class PlayerMovement : Character
    {
        #region Fields and Properties

        //input
        [Header("Player Specific Options:")] public bool isDebugPlayer;
        public bool jetpackInputHeld;

        public bool interactInputHeld;

        [SerializeField] private TextMeshProUGUI playerNameText;

        [SerializeField] private PlayerInput playerInputComponent;

        InputActionMap inputMap;
        private bool isHoldingDown = false;

        [Header("Joystick Spin Detection Options")] [SerializeField]
        private float spinAngleCheckUpdateTimer = 0.1f;

        [SerializeField] [Range(0.0f, 180.0f)] private float spinValidAngleLimit = 30.0f;
        [SerializeField] private int validSpinCheckRows = 1;
        [SerializeField] private float cannonScrollSensitivity = 3f;
        public bool isSpinningJoystick = false;
        private float spinningDirection = 1; //1 = Clockwise, -1 = CounterClockwise
        public float spinningForce = 0;

        //Joystick spin detection
        private Vector2 lastJoystickInput = Vector2.zero;
        private bool isCheckingSpinInput = false;
        private int validSpinCheckCounter = 0;
        public bool isUsingCompositeAiming = false; //whether or not the active player is using composite aiming controls or not, ie whether they are using a joystick to aim or not

        private Cell buildCell; //Cell player is currently building a stack interactable in (null if not building)
        private float timeBuilding = 0; //Time spent in build mode

        [Header("Objects")] public LayerMask carryableObjects;
        public bool isCarryingSomething; //true if player is currently carrying something
        public Cargo currentObject; //what object the player is currently carrying

        [Header("Building & Repairing")]
        [SerializeField, Tooltip("What cell the player is currently repairing")]
        private GameObject buildGhost; //current ghost the player is using to build

        [SerializeField, Tooltip("Active timer used to gauge time spent on a job.")]
        private float jobTimer = 0;

        private float maxShakeIntensity = 0.5f;
        private float currentShakeTime;
        private bool isShaking = false;

        private PlayerData playerData;

        public static Action OnPlayerDeath;

        private CharacterLegFloater legFloater;

        [SerializeField]
        [Tooltip("For coupler drop downs, how far the stick must be pushed to drop the coupler.")]
        [Range(0, 1)]
        private float couplerStickDeadzone;

        [SerializeField]
        [Tooltip("For ladders, how far the stick must be pushed left or right to dismount the ladder.")]
        [Range(0, 1)]
        private float ladderDismountDeadzone;

        [SerializeField]
        [Tooltip("For ladders, how far the stick must be pushed down or up to enter the ladder.")]
        [Range(0, 1)]
        private float ladderEnterDeadzone;

        [SerializeField] private float ladderDetectionRadius;

        [SerializeField]
        [Tooltip(
            "Player speed would reach zero at this slope value. The higher this value is set, the faster the player will walk on slopes up until this value. ")]
        private float maxSlope = 100;

        [SerializeField] private float maxYVelocity, maxXVelocity;
        
        #endregion

        #region Unity Methods

        protected override void Awake()
        {
            base.Awake();

            

            if (isDebugPlayer)
            {
                PlayerInput debugInput = GameObject.Find("Debug_TankBuilder").GetComponent<PlayerInput>();
                if (debugInput != null) AddDebuggerPlayerInput(debugInput);
            }

            if (TryGetComponent<CharacterLegFloater>(out CharacterLegFloater _legFloater))
            {
                legFloater = _legFloater;
            }
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GetComponentInChildren<CanvasGroup>().alpha = GameManager.Instance.UIManager.isVisible ? 1 : 0;
        }

        private void OnDisable()
        {
            if (playerInputComponent != null)
            {
                if (isDebugPlayer)
                    inputMap.actionTriggered -= OnDebugInput;

                else
                    inputMap.actionTriggered -= OnPlayerInput;

                playerInputComponent.onDeviceLost -= OnDeviceLost;
                playerInputComponent.onDeviceRegained -= OnDeviceRegained;
            }
        }

        public void AddDebuggerPlayerInput(PlayerInput debugPlayerInput)
        {
            LinkPlayerInput(debugPlayerInput);
        }

        protected override void Update()
        {
            base.Update();

            //Debug.Log(rb.velocity);
            if (rb.velocity.y > maxYVelocity)
            {
                rb.velocity = new Vector2(rb.velocity.x, maxYVelocity);
            }

            if (!isAlive)
            {
                if (GameManager.Instance.AudioManager.IsPlaying("JetpackRocket", gameObject))
                {
                    GameManager.Instance.AudioManager.Stop("JetpackRocket", gameObject);
                }

                return;
            }

            if (isShaking)
                ShakePlayer();
            var lastLadder = currentLadder;
            currentLadder = Physics2D.OverlapCircle(transform.position, ladderDetectionRadius, ladderLayer)?.gameObject;
            if (!currentLadder && !CheckSurfaceCollider(couplerLayer) && !CheckSurfaceCollider(hatchLayer))
            { //if no ladder is detected on us, and we arent on a coupler (layer 18), check below. this is for ladders that go up out of a tank
                currentLadder = Physics2D.OverlapCircle(transform.position - transform.up, ladderDetectionRadius, ladderLayer)?.gameObject;
            }
            
            if (lastLadder != currentLadder && currentLadder != null && currentState == CharacterState.CLIMBING) //will only be active for the frame that we found a new ladder
            {
                Vector3 localPosition = currentLadder.transform.InverseTransformPoint(transform.position);
                if (!Mathf.Approximately(localPosition.x, 0))
                {
                    AlignPlayerWithLadder(); //fixes issues with climbing ladders that aren't aligned in a straight line
                }
            }

            //Interactable building:
            if (buildCell != null) //Player is currently in build mode
            {
                currentState = CharacterState.OPERATING;
                transform.position = buildCell.repairSpot.position;
                StartBuilding();

                timeBuilding += Time.deltaTime; //Increment build time tracker
                if (timeBuilding >= characterSettings.buildTime) //Player has finished build
                {
                    StackManager.EndBuildingStackItem(0, this);
                    TankInteractable currentInteractable = StackManager.BuildTopStackItem();             
                    currentInteractable.InstallInCell(buildCell); //Install interactable from top of stack into designated build cell
                    if (currentInteractable.interactableType == TankInteractable.InteractableType.CONSUMABLE) currentInteractable.gameObject.GetComponent<TankConsumable>().Use();
                    StopBuilding(); //Indicate that build has stopped

                    //Build Effects
                    GameManager.Instance.AudioManager.Play("UseWrench", currentInteractable.gameObject);
                    GameManager.Instance.AudioManager.Play("ConnectRoom", currentInteractable.gameObject);
                    Vector2 particlePos = new Vector2(currentInteractable.transform.position.x, currentInteractable.transform.position.y - 0.5f);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(19, particlePos, 0.1f, currentInteractable.transform);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(6, particlePos, 0.4f, currentInteractable.transform);
                    GameManager.Instance.ParticleSpawner.SpawnParticle(7, particlePos, 0.4f, currentInteractable.transform);

                    //Add the interactable built to the stats
                    GetComponentInParent<TankController>()?.AddInteractableToStats(currentInteractable);
                }
            }

            //Handle Jetpack
            if (jetpackInputHeld && currentState != CharacterState.OPERATING)
            {
                currentFuel -= fuelDepletionRate * Time.deltaTime;
                if (currentFuel < 0) currentFuel = 0;

                if (!GameManager.Instance.AudioManager.IsPlaying("JetpackRocket", gameObject))
                {
                    GameManager.Instance.AudioManager.Play("JetpackRocket", gameObject);
                }
                characterHUD?.UpdateFuelBar((currentFuel / characterSettings.fuelAmount) * 100f);
            }
            else
            {
                if (CheckGround() || currentState == CharacterState.OPERATING)
                {
                    currentFuel += fuelRegenerationRate * Time.deltaTime;
                    if (currentFuel > 100) currentFuel = 100;
                }

                if (GameManager.Instance.AudioManager.IsPlaying("JetpackRocket", gameObject))
                {
                    GameManager.Instance.AudioManager.Stop("JetpackRocket", gameObject);
                }

                if (jetpackVisuals.activeInHierarchy == true)
                {
                    jetpackVisuals.SetActive(false);
                    jetpackSmoke.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }

                characterHUD?.UpdateFuelBar((currentFuel / characterSettings.fuelAmount) * 100f);
            }

            //If we're manning an Interactable
            if (currentInteractable != null)
            {
                currentState = CharacterState.OPERATING;

                if (interactInputHeld && currentInteractable.isContinuous)
                {
                    currentInteractable.Use();
                }

                if (currentInteractable.canAim)
                {
                    if (!isUsingCompositeAiming)
                    {
                        CheckJoystickSpinning();
                        if (isSpinningJoystick)
                        {
                            currentInteractable.Rotate(spinningForce);
                        }
                        else currentInteractable.Rotate(0);
                    }
                    else
                    {
                        GunController gunScript = currentInteractable.GetComponent<GunController>();
                        if (gunScript != null) 
                        {
                            if (gunScript.gunType == GunController.GunType.MORTAR)
                            {
                                if (Mathf.Abs(moveInput.x) > 0.2f) currentInteractable.Rotate(-moveInput.x);
                                else currentInteractable.Rotate(0);
                            }
                            else
                            {
                                if (Mathf.Abs(moveInput.y) > 0.2f) currentInteractable.Rotate(moveInput.y);
                                else currentInteractable.Rotate(0);
                            }
                        }
                    }
                }
            }

            //If we've got a job
            if (currentJob?.jobType != CharacterJobType.NONE)
            {
                currentState = CharacterState.OPERATING;
                transform.position = currentJob.GetJobPosition();
                StartNewJob(currentJob);

                jobTimer += Time.deltaTime; //Increment job time tracker
                if (jobTimer >= currentJob.jobTime) //Player has finished job
                {
                    //Which tool are we using?
                    CargoMelee tool = currentObject?.GetComponent<CargoMelee>();
                    float durabilityLoss = 50f;

                    //Job Effects
                    switch (currentJob.jobType)
                    {
                        case CharacterJobType.FIX:
                            {
                                //Fix a Broken Interactable or Treadsystem
                                GameObject targetObject = currentJob.interactable?.gameObject;
                                currentJob.interactable?.Fix();
                                TreadSystem treads = currentJob.zone?.transform.GetComponentInParent<TreadSystem>();
                                if (treads != null) { treads.Unjam(); targetObject = treads.gameObject; }
                                GameManager.Instance.AudioManager.Play("ItemPickup", targetObject);
                                GameManager.Instance.AudioManager.Play("UseWrench", targetObject);
                                Vector2 particlePos = new Vector2(targetObject.transform.position.x, targetObject.transform.position.y);
                                GameManager.Instance.ParticleSpawner.SpawnParticle(6, particlePos, 0.5f, targetObject.transform);
                                GameManager.Instance.ParticleSpawner.SpawnParticle(7, particlePos, 0.4f, targetObject.transform);
                            }
                            break;
                        case CharacterJobType.UNINSTALL:
                            {
                                //Uninstall an Interactable from a cell
                                GameManager.Instance.AudioManager.Play("ConnectRoom", currentJob.interactable.gameObject);
                                Vector2 particlePos = new Vector2(currentJob.interactable.transform.position.x, currentJob.interactable.transform.position.y - 0.5f);
                                Transform treads = currentJob.interactable.tank?.treadSystem.transform;
                                
                                GameManager.Instance.ParticleSpawner.SpawnParticle(6, particlePos, 0.35f, treads);
                                GameManager.Instance.ParticleSpawner.SpawnParticle(19, particlePos, 0.1f, treads);

                                HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("QuickJolt");
                                GameManager.Instance.SystemEffects.ApplyControllerHaptics(this.GetPlayerData().playerInput, setting); //Apply haptics
                                GameManager.Instance.ParticleSpawner.SpawnParticle(28, currentJob.interactable.transform.position, 0.3f, treads);
                                currentJob.interactable.parentCell.AddInteractablesFromCell(true);

                                durabilityLoss *= 4f;
                            }
                            break;
                        case CharacterJobType.CLAIM:
                            {
                                //Claim a Tank
                                TankController tank = currentJob.zone.GetComponentInParent<TankController>();
                                if (tank != null) { TankManager.Instance.TransferPlayerTank(tank); }
                                Debug.Log("Tried to claim a tank!");
                            }
                            break;
                    }
                    StopCurrentJob(); //Indicate that job has stopped
                    CancelInteraction(startJump: true);
                    if (tool != null)
                    {
                        tool.durability -= (int)durabilityLoss;
                        tool.CheckDurability();
                    }
                }
            }

            //If we're carrying something
            if (currentObject != null)
            {
                currentObject.transform.position = hands.position;

                if (currentObject.type == Cargo.CargoType.TOOL)
                {
                    CargoSprayer sprayer = currentObject.GetComponent<CargoSprayer>();
                    if (sprayer != null)
                    {
                        sprayer.UpdateNozzle(moveInput);
                    }
                }
            }

            if (moveInput.y <= -0.5) isHoldingDown = true;
            else isHoldingDown = false;

            UpdateCharacterDirection();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (!isCharacterLoaded || !isAlive)
                return;

            //If the jetpack is held and there's fuel and the player isn't operating anything, the jetpack is active
            if (jetpackInputHeld && currentFuel > 0 || isOperator)
                jetpackCanBeUsed = false;
            else
                jetpackCanBeUsed = true;
        }

        public void UpdateCharacterDirection()
        {
            if (moveInput.x > 0.2) faceDirection = 1; //right
            else if (moveInput.x < -0.2) faceDirection = -1; //left

            Vector2 newPos = new Vector2(Mathf.Abs(hands.transform.localPosition.x) * faceDirection, hands.transform.localPosition.y);
            hands.transform.localPosition = newPos;
        }

        public float GetCharacterDirection()
        {
            float direction = faceDirection;

            return direction;
        }
        
        #endregion

        #region Movement

        protected override void MoveCharacter()
        {
            if (!isAlive)
                return;

            if (currentJob?.jobType != CharacterJobType.NONE) currentJob.jobType = CharacterJobType.NONE;
            if (buildCell != null) buildCell = null;
            
            if (rb.bodyType != RigidbodyType2D.Dynamic) rb.bodyType = RigidbodyType2D.Dynamic;

            var slope = transform.eulerAngles.z < 180 ? transform.eulerAngles.z : transform.eulerAngles.z - 360;
            //^ im gonna change this to use a raycast and a normal, but for now this is fine and works for checking in-tank slopes

            float deAccel = Mathf.InverseLerp(maxSlope, 0, Mathf.Abs(slope));

            if (Mathf.Sign(slope) == Mathf.Sign(moveInput.x))
            {
                currentGroundMoveSpeed = groundMoveSpeed * deAccel;
            }
            else currentGroundMoveSpeed = groundMoveSpeed;

            Vector2 force = transform.right *
                            (moveInput.x * ((CheckGround()) ? currentGroundMoveSpeed : horizontalAirSpeed));

            if (CheckGround())
            {
                rb.AddForce(force, ForceMode2D.Impulse);
                //draw gizmo for force
                if (!jetpackInputHeld)
                {
                    Vector2 localVel = transform.InverseTransformDirection(rb.velocity);
                    if (localVel.y is >= 0 and < 1.5f) localVel.y = 0; // if we are grounded, and not trying to move up,
                    // we always want our local up axis, which is aligned with the tank, to be 0
                    // if we are over 2 velocity, this code will not run, this fixes
                    // issues where you would stop moving in the middle of flying up a coupler
                    rb.velocity = transform.TransformDirection(localVel);
                }
            }
            else
            {
                rb.AddForce(force * 10,
                    ForceMode2D
                        .Force); //* 5 just makes the air force value more intuitive when compared to ground speed, air force requires more force because
                // its using force.force instead of impulse
            }

            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

            localVelocity.x = Mathf.Lerp(localVelocity.x, 0,
                (CheckGround() ? groundDeAcceleration : airDeAcceleration) * Time.deltaTime);

            rb.velocity = transform.TransformDirection(localVelocity);

            if (Mathf.Abs(rb.velocity.x) > maxXVelocity && moveInput.x != 0)
            {
                rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxXVelocity, rb.velocity.y);
            }

            if (moveInput.x == 0 && CheckGround()) //slip down slopes
            {
                rb.AddForce(-transform.right.normalized * ((slope * .33f) * slipSlopeValue), ForceMode2D.Force);
            }


            if (jetpackInputHeld && currentFuel > 0)
            {
                PropelJetpack();
                if (jetpackCanBeUsed)
                    jetpackCanBeUsed = false;
                jetpackVisuals.SetActive(true);
                jetpackSmoke.Play();
            }
            else if (!jetpackCanBeUsed)
                jetpackCanBeUsed = true;
            else
            {
                jetpackVisuals.SetActive(false);
                jetpackSmoke.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }


        protected override void ClimbLadder()
        {
            Collider2D onCoupler = Physics2D.OverlapBox(transform.position, Vector3.one * .33f, transform.eulerAngles.z,
                (1 << LayerMask.NameToLayer("Coupler")) |
                          (1 << LayerMask.NameToLayer("Hatch"))); 
            Collider2D onLadder = Physics2D.OverlapBox(transform.position, Vector3.one * .33f, transform.eulerAngles.z,
                1 << LayerMask.NameToLayer("Ladder"));
            RaycastHit2D hitGround = Physics2D.Raycast(transform.position, -transform.up, transform.localScale.y * .7f,
                1 << LayerMask.NameToLayer("Ground"));
            RaycastHit2D hitStopUp = Physics2D.Raycast(transform.position, transform.up, transform.localScale.y * .7f,
                1 << LayerMask.NameToLayer("StopPlayer"));
            RaycastHit2D hitStopDown = Physics2D.Raycast(transform.position, -transform.up, transform.localScale.y * .7f,
                1 << LayerMask.NameToLayer("StopPlayer"));
            Collider2D ladderUnderMe = Physics2D.OverlapBox(transform.position - transform.up, Vector3.one * .33f, transform.eulerAngles.z,
                1 << LayerMask.NameToLayer("Ladder"));

            Vector3 displacement = transform.up * ((moveInput.y > 0 ? ladderClimbUpSpeed : ladderClimbDownSpeed) *
                                                   moveInput.y * Time.fixedDeltaTime);
            bool inGroundNextFrame = false;
            if (moveInput.y < 0) inGroundNextFrame = Physics2D.OverlapBox(transform.position + displacement, Vector3.one * .33f,
                transform.eulerAngles.z, 1 << LayerMask.NameToLayer("Ground"));

            // If you hit ground, and you're trying to move down, stop climbing.
            // If you're not on a coupler or ladder, and you're trying to move up, stop climbing.
            // This makes it so you can climb up through couplers, if the coupler is at the end of the ladder.
            if ((hitGround && !ladderUnderMe && moveInput.y < 0) || (!onCoupler && !onLadder && moveInput.y > 0) || (hitStopUp && moveInput.y > 0) || (hitStopDown && moveInput.y < 0))
            {
                displacement = Vector3.zero;
                if (inGroundNextFrame)
                {
                    // this gets rid of any inconsitencies with where the player stops on the ground from climbing down the ladder.
                    transform.position = new Vector3(transform.position.x, hitGround.point.y + transform.localScale.y / 2, transform.position.z);
                }
            }
            
            if (!onCoupler &&
                !onLadder && !ladderUnderMe) //final failsafe for if you're somehow in this state and not in a ladder or coupler
            {
                CancelInteraction();
            }
            currentFuel += fuelRegenerationRate * Time.fixedDeltaTime;
            Vector3 newPosition = transform.position + displacement;
            rb.MovePosition(newPosition);
            
            if (Mathf.Abs(moveInput.x) > ladderDismountDeadzone)
            {
                if (!CheckGround()) CancelInteraction(startJump:true);
                else CancelInteraction();
            }

            if (jetpackInputHeld)
            {
                CancelInteraction();
            }
        }

        public void SetFuel(float value)
        {
            currentFuel = value;
            GameManager.Instance.AudioManager.Play("JetpackRefuel", gameObject);
        }

        #endregion

        #region Input

        public void LinkPlayerInput(PlayerInput newInput)
        {
            playerInputComponent = newInput;
            playerData = PlayerData.ToPlayerData(newInput);
            characterIndex = playerInputComponent.playerIndex;

            //Gets the player input action map so that events can be subscribed to it
            if (isDebugPlayer)
            {
                inputMap = playerInputComponent.actions.FindActionMap("Debug");
                inputMap.actionTriggered += OnDebugInput;
            }
            else
            {
                inputMap = playerInputComponent.actions.FindActionMap("Player");
                inputMap.actionTriggered += OnPlayerInput;
            }

            //Subscribes events for control lost / regained
            playerInputComponent.onDeviceLost += OnDeviceLost;
            playerInputComponent.onDeviceRegained += OnDeviceRegained;

            characterColor = GameManager.Instance.MultiplayerManager.GetPlayerColors()[newInput.playerIndex];
            UpdatePlayerNameDisplay();
        }

        public void OnDeviceLost(PlayerInput playerInput)
        {
            FindObjectOfType<CornerUIController>()?.OnDeviceLost(characterIndex);
        }

        public void OnDeviceRegained(PlayerInput playerInput)
        {
            FindObjectOfType<CornerUIController>()?.OnDeviceRegained(characterIndex);
        }

        private void OnPlayerInput(InputAction.CallbackContext ctx)
        {
            //If the player is in a menu, ignore input
            if (GameManager.Instance.InGameMenu)
                return;

            //Gets the name of the action and calls the appropriate events
            switch (ctx.action.name)
            {
                case "Move":
                    OnMove(ctx);
                    break;
                case "Jetpack":
                    OnJetpack(ctx);
                    break;
                case "Control Steering":
                    OnControlSteering(ctx);
                    break;
                case "Interact":
                    OnInteract(ctx);
                    break;
                case "Cancel":
                    OnCancel(ctx);
                    break;
                case "Pickup":
                    OnPickup(ctx);
                    break;
                case "Build":
                    OnBuild(ctx);
                    break;
                case "Persona3":
                    OnSelfDestruct(ctx);
                    break;
            }
        }

        private void OnDebugInput(InputAction.CallbackContext ctx)
        {
            //If the player is in a menu, ignore input
            if (GameManager.Instance.InGameMenu)
                return;

            //Gets the name of the action and calls the appropriate events
            switch (ctx.action.name)
            {
                case "Move":
                    OnMove(ctx);
                    break;
                case "1":
                    OnJetpack(ctx);
                    break;
                case "Control Steering":
                    OnControlSteering(ctx);
                    break;
                case "2":
                    OnInteract(ctx);
                    break;
                case "3":
                    OnPickup(ctx);
                    break;
                case "4":
                    OnCancel(ctx);
                    break;
                case "Build":
                    OnBuild(ctx);
                    break;
                case "Persona3":
                    OnSelfDestruct(ctx);
                    break;
            }
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;

            var binding = ctx.action.GetBindingForControl(ctx.control);
            if (binding.Value.isPartOfComposite) isUsingCompositeAiming = true;
            else isUsingCompositeAiming = false;

            SetCharacterMovement(ctx.ReadValue<Vector2>());

            if ((moveInput.y > ladderEnterDeadzone || moveInput.y < -ladderEnterDeadzone) &&
                currentLadder != null && currentState != CharacterState.CLIMBING && currentState != CharacterState.OPERATING 
                && (!jetpackInputHeld || CheckSurfaceCollider(LayerMask.NameToLayer("Ground"))))
            {
                SetLadder();
                //if up is pressed above the deadzone,
                //if down is pressed under the deadzone and we aren't grounded, if we are near a ladder and we aren't already climbing,
                // and if we arent trying to jetpack mid-air currently.
                //SetLadder();
            }

            if (moveInput.y < -couplerStickDeadzone && (CheckSurfaceCollider(couplerLayer) || CheckSurfaceCollider(hatchLayer))) //todo: change to "checkCoupler" method
            {
                Collider2D platform = null;
                if (CheckSurfaceCollider(couplerLayer))
                {
                    platform = CheckSurfaceCollider(couplerLayer);
                }
                if (CheckSurfaceCollider(hatchLayer))
                {
                    platform = CheckSurfaceCollider(hatchLayer);
                }
                
                bool onEnemyTank = platform.transform.root.TryGetComponent(out TankController tc) &&
                                   tc.tankType == TankId.TankType.ENEMY; //we dont want to be able to go into hatches on enemy tanks
                
                if (platform.gameObject.TryGetComponent(out PlatformCollisionSwitcher collSwitcher) && !onEnemyTank)
                {
                    StartCoroutine(
                        collSwitcher.DisableCollision(GetComponent<Collider2D>())); //Disable collision with platform
                } // if leg floater is present, 
                // disable use on platform  (leg floater floats the character collider by a few pixels which means the character won't slip and also won't trip over small changes in terrain collision)
            }
        }

        public void OnJetpack(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;
            if (buildCell != null) return;
            if (currentJob.jobType != CharacterJobType.NONE) return;

            jetpackInputHeld = ctx.ReadValue<float>() > 0;
            if (ctx.ReadValue<float>() > 0 && currentState != CharacterState.OPERATING)
            {
                GameManager.Instance.AudioManager.Play("JetpackStartup", gameObject);
                GameManager.Instance.ParticleSpawner.SpawnParticle(19, jetpackVisuals.transform.position, 0.05f, null);
            }
        }

        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;
            if (buildCell != null) return;
            if (currentJob.jobType != CharacterJobType.NONE) return;

            interactInputHeld = ctx.ReadValue<float>() > 0;

            if (ctx.started)
            {
                if (currentZone != null && !isCarryingSomething)
                {
                    if (!Physics2D.Linecast(transform.position, currentZone.transform.position, obstructionMask))
                        currentZone.Interact(this.gameObject);
                    else
                    {
                        Debug.Log("Interactable is obstructed!");
                        //GameManager.Instance.AudioManager.Play("InvalidAlert"); 
                    }
                }

                if (currentInteractable != null && isOperator)
                {
                    currentInteractable.Use();
                }

                if (currentInteractable == null)
                {
                    if (isCarryingSomething && currentJob.jobType == CharacterJobType.NONE)
                    {
                        if (currentObject != null) currentObject.Use();
                    }
                }
            }

            if (ctx.action.WasReleasedThisFrame())
            {
                if (currentInteractable != null)
                {
                    currentInteractable.CancelUse();
                }
            }
        }

        public void OnBuild(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;

            if (StackManager.stack.Count > 0 && ctx.performed && !isOperator && !isCarryingSomething)
            {
                //Check if build is valid:
                Collider2D cellColl =
                    Physics2D.OverlapPoint(transform.position,
                        LayerMask.GetMask("Cell")); //Get cell player is currently on top of (if any)
                if (cellColl != null && cellColl.TryGetComponent(out Cell cell)) //Player is on top of a cell
                {
                    if (cell.room.targetTank.tankType == TankId.TankType.PLAYER && cell.interactable == null &&
                        cell.playerBuilding == null)
                    {
                        buildCell = cell; //Indicate that player is building in this cell
                        cell.playerBuilding = this; //Indicate that this player is building in given cell
                        print("started building");
                        taskProgressBar = GameManager.Instance.UIManager.AddRadialTaskBar(this.gameObject, new Vector2(0.5f, 0.5f), characterSettings.buildTime, GetCharacterColor(), true);
                        taskProgressBar?.StartTask(characterSettings.buildTime);
                        StackManager.StartBuildingStackItem(0, this, characterSettings.buildTime);
                    }
                    else print("tried to start building");
                }
            }

            //Tool related Jobs
            if (ctx.performed && isCarryingSomething && currentObject != null)
            {
                if (currentObject.isContinuous)
                {
                    currentObject.Use(true);
                }
                else if (currentObject.type == Cargo.CargoType.TOOL)
                {
                    if (currentZone != null) //if we're standing near a Zone
                    {
                        //Try to start a job
                        if (!Physics2D.Linecast(transform.position, currentZone.transform.position, obstructionMask))
                        {
                            CharacterJobType type = CharacterJobType.FIX;

                            CargoMelee tool = currentObject.GetComponent<CargoMelee>();
                            if (tool != null)
                            {
                                if (tool.isWrench) type = CharacterJobType.FIX;
                                else type = CharacterJobType.UNINSTALL;
                            }
                            TankInteractable interactable = currentZone.GetComponentInParent<TankInteractable>();
                            TreadSystem treads = currentZone.GetComponentInParent<TreadSystem>();
                            JobZone zone = currentZone.GetComponent<JobZone>();

                            bool jobsGood = true;
                            if (type == CharacterJobType.FIX && interactable != null) { if (!interactable.isBroken) jobsGood = false; } //If it ain't broke don't fix it
                            if (type == CharacterJobType.FIX && treads != null) { if (!treads.isJammed) jobsGood = false; } //If it ain't jammed don't unjam it
                            if (zone != null)
                            {
                                type = zone.jobType;
                                if (zone.requiresItem)
                                {
                                    if (tool?.type != zone.requiredItem) jobsGood = false; //if we don't have the right tool, don't do the job
                                }
                            }
                            if (jobsGood)
                            {
                                //Determine the Job
                                CharacterJob newJob = new CharacterJob();
                                newJob.jobType = type;
                                newJob.interactable = interactable;
                                newJob.zone = zone;

                                newJob.jobTime = newJob.GetJobTime();

                                //Determine What Animation to do
                                string animationHash = "";
                                if (type == CharacterJobType.FIX) animationHash = "WrenchFix";
                                if (type == CharacterJobType.UNINSTALL) animationHash = "CrowbarPull";
                                tool?.CancelMelee();
                                tool?.AnimateJob(animationHash);
                                
                                //Setup Progress Bar
                                taskProgressBar = GameManager.Instance.UIManager.AddRadialTaskBar(this.gameObject, new Vector2(0, 0), newJob.jobTime, GetCharacterColor(), true);
                                taskProgressBar?.StartTask(newJob.jobTime);
                                taskProgressBar.transform.localScale = new Vector3(1.8f, 1.8f, 1);

                                //Start the Job
                                currentJob = newJob;
                            }
                        }
                        else
                        {
                            Debug.Log("Interactable is obstructed!");
                            //GameManager.Instance.AudioManager.Play("InvalidAlert"); 
                        }
                    }
                }
            }

            //Non-Tool Jobs
            if (ctx.performed && !isCarryingSomething)
            {
                if (currentZone != null) //if we're standing near a Zone
                {
                    if (currentZone.GetComponent<JobZone>() == null) return; //ignore non-job zones, these usually require tools

                    //Try to start a job
                    if (!Physics2D.Linecast(transform.position, currentZone.transform.position, obstructionMask))
                    {
                        CharacterJobType type = CharacterJobType.CLAIM;

                        TankInteractable interactable = currentZone.GetComponentInParent<TankInteractable>();
                        JobZone zone = currentZone.GetComponent<JobZone>();

                        bool jobsGood = true;
                        if (zone != null)
                        {
                            type = zone.jobType;
                            if (zone.requiresItem) jobsGood = false; //if the zone requires an item, don't try and do it
                            if (type == CharacterJobType.FIX) { if (!interactable.isBroken) jobsGood = false; } //If it ain't broke don't fix it
                        }
                        if (jobsGood)
                        {
                            //Determine the Job
                            CharacterJob newJob = new CharacterJob();
                            newJob.jobType = type;
                            newJob.interactable = interactable;
                            newJob.zone = zone;

                            newJob.jobTime = newJob.GetJobTime();

                            //Setup Progress Bar
                            taskProgressBar = GameManager.Instance.UIManager.AddRadialTaskBar(this.gameObject, new Vector2(0, 0), newJob.jobTime, GetCharacterColor(), true);
                            taskProgressBar?.StartTask(newJob.jobTime);
                            taskProgressBar.transform.localScale = new Vector3(1.8f, 1.8f, 1);

                            //Start the Job
                            currentJob = newJob;
                        }
                    }
                    else
                    {
                        GameObject obstruction = Physics2D.Linecast(transform.position, currentZone.transform.position, obstructionMask).collider.gameObject;
                        Debug.Log("Zone is obstructed by " + obstruction.name + "!");
                        //GameManager.Instance.AudioManager.Play("InvalidAlert"); 
                    }
                }
            }

            if (ctx.canceled) //Released Button
            {
                if (currentObject != null)
                {
                    currentObject.CancelUse();
                }

                if (buildCell != null)
                {
                    StopBuilding();
                    StackManager.EndBuildingStackItem(0, this);
                }

                if (currentJob.jobType != CharacterJobType.NONE)
                {
                    StopCurrentJob();
                    if (currentState == CharacterState.OPERATING) currentState = CharacterState.NONCLIMBING;
                    if (taskProgressBar != null) taskProgressBar.EndTask();
                }
            }
        }

        public void OnCancel(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;

            if (ctx.started)
            {
                if (currentInteractable != null)
                {
                    currentInteractable.Exit(true);
                }

                if (currentObject != null && currentJob.jobType == CharacterJobType.NONE)
                {
                    currentObject.Drop(this, true, moveInput);
                }
            }
        }

        public void OnControlSteering(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;

            float steeringValue = ctx.ReadValue<float>();
            int _steeringValue = 0;
            if (currentInteractable != null && isOperator && Mathf.Abs(steeringValue) > 0.5f)
            {
                if (steeringValue > 0.5f) _steeringValue = 1;
                if (steeringValue < -0.5f) _steeringValue = -1;

                currentInteractable.Shift(_steeringValue);
            }
        }

        public void OnPickup(InputAction.CallbackContext ctx)
        {
            if (!isAlive) return;
            if (buildCell != null) return;
            if (currentJob.jobType != CharacterJobType.NONE) return;

            if (ctx.performed) //Button Pressed
            {
                if (currentInteractable == null)
                {
                    //Items
                    Pickup();
                }

                //Interactables
                if (currentInteractable != null) currentInteractable.SecondaryUse(true);
            }

            if (ctx.canceled) //Let go
            {
                if (currentInteractable != null) currentInteractable.SecondaryUse(false);
            }
        }

        public void OnSelfDestruct(InputAction.CallbackContext ctx)
        {
            //If the player is not alive, ignore this
            if (!isAlive) return;

            if (ctx.started)
            {
                isShaking = true;
                currentShakeTime = 0f;
            }

            if (ctx.performed)
            {
                characterVisualParent.localPosition = Vector3.zero;
                KillCharacterImmediate();
                isShaking = false;
            }

            if (ctx.canceled)
            {
                characterVisualParent.localPosition = Vector3.zero;
                isShaking = false;
            }
        }

        private void ShakePlayer()
        {
            currentShakeTime += Time.deltaTime;

            float maxShakeTime = 3f;

            if (currentShakeTime >= 1f)
            {
                // Normalize the shake intensity between 1 second and 3 seconds
                float normalizedTime = Mathf.Clamp01((currentShakeTime - 1f) / (maxShakeTime - 1f));
                float shakeIntensity = normalizedTime * maxShakeIntensity;

                // Use Perlin noise for smooth random movement in each axis
                float offsetX = (Mathf.PerlinNoise(Time.time * 10f, 0f) - 0.5f) * 2f * shakeIntensity;
                float offsetY = (Mathf.PerlinNoise(Time.time * 10f, 100f) - 0.5f) * 2f * shakeIntensity;
                float offsetZ = (Mathf.PerlinNoise(Time.time * 10f, 200f) - 0.5f) * 2f * shakeIntensity;

                // Apply the shake offset to the characterVisualParent's local position
                characterVisualParent.localPosition = new Vector3(offsetX, offsetY, offsetZ);
            }
        }

        private void CheckJoystickSpinning()
        {
            //If the current movement vector is different from the previous movement vector and spinning input is not being checked
            if (moveInput != lastJoystickInput && !isCheckingSpinInput)
            {
                //Check for spin input
                isCheckingSpinInput = true;
                StartCoroutine(JoystickSpinningDetection());
            }

            //If the number of spin checks is equal to number of spins that are needed, the joystick has been properly spun
            if (validSpinCheckCounter == validSpinCheckRows)
            {
                isSpinningJoystick = true;
            }

            //If not, the joystick is not spinning properly
            else
            {
                isSpinningJoystick = false;
            }
        }

        private IEnumerator JoystickSpinningDetection()
        {
            //Store the movement variable for later use
            lastJoystickInput = moveInput;

            //Wait for a bit to check for a spin angle
            yield return new WaitForSeconds(spinAngleCheckUpdateTimer);

            //If the angle between the last known movement vector and the current movement vector reaches a specified amount
            if (Vector2.Angle(lastJoystickInput, moveInput) >= spinValidAngleLimit)
            {
                //Detect rotation direction
                var spinAngle = Vector2.SignedAngle(lastJoystickInput, moveInput);
                if (spinAngle > 0) spinningDirection = -1;
                else if (spinAngle < 0) spinningDirection = 1;

                //Assign rotation force variable
                spinningForce = (spinAngle / 100);

                //Register this as a joystick spin
                validSpinCheckCounter++;
                validSpinCheckCounter = Mathf.Clamp(validSpinCheckCounter, 0, validSpinCheckRows);
            }
            //If not, there is not enough movement to consider the action a spin. Reset
            else
            {
                validSpinCheckCounter = 0;
            }

            //End the check
            isCheckingSpinInput = false;


        }

        public override void LinkPlayerHUD(PlayerHUD newHUD)
        {
            characterHUD = newHUD;
            characterHUD.InitializeHUD(characterIndex, playerData.GetPlayerName());
        }

        #endregion

        #region Character Functions

        public void Pickup()
        {
            Vector2 zone = new Vector2(1.2f, 1.2f);
            List<Cargo> objects = new List<Cargo>();
            Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, zone, 0f, carryableObjects);

            if (colliders.Length > 0)
            {
                foreach (Collider2D collider in colliders) //Look through each collider we're touching
                {
                    Cargo item = collider.transform.GetComponent<Cargo>();
                    if (item != null) objects.Add(item); //Add it to the list of cargo 
                }

                float distance = 5f;
                foreach (Cargo item in objects)
                {
                    float _distance = Vector2.Distance(item.transform.position, transform.position);
                    if (_distance < distance) distance = _distance;
                }

                foreach (Cargo item in objects)
                {
                    float _distance = Vector2.Distance(item.transform.position, transform.position);
                    if (_distance == distance)
                    {
                        if (!Physics2D.Linecast(transform.position, item.transform.position, obstructionMask))
                        {
                            if (currentObject != null) { currentObject.Drop(this, false, moveInput); } //drop what we're currently holding before grabbing the next thing
                            item.Pickup(this);
                        }
                        else
                        {
                            Debug.Log("Object is obstructed!");
                            //GameManager.Instance.AudioManager.Play("InvalidAlert");
                        }
                    }
                }
            }
        }

        public void StartBuilding()
        {
            if (buildCell == null) return;

            if (buildGhost == null)
            {
                GameObject ghost = null;

                //Find the object we're building
                TankInteractable interactable = StackManager.GetTopStackItem();
                foreach(TankInteractable _interactable in GameManager.Instance.interactableList)
                {
                    if (_interactable.stackName == interactable.stackName) //Found Valid Ref
                    {
                        //Spawn the Ghost
                        GameObject prefab = _interactable.ghostPrefab;
                        if (prefab != null) ghost = Instantiate(prefab, buildCell.transform);
                    }
                }

                //Play Build Animation
                if (ghost != null)
                {
                    Animator ghostAnimator = ghost.GetComponent<Animator>();
                    //Play
                }

                //Assign the Ghost to the Player
                buildGhost = ghost;
            }
        }

        public void StopBuilding(bool forceStopBuild = false)
        {
            if (buildGhost != null) Destroy(buildGhost);
            if (buildCell == null) return; //Do nothing if player is not building
            buildCell.playerBuilding = null; //Indicate to cell that it is no longer being built in
            buildCell = null; //Clear cell reference
            timeBuilding = 0; //Reset build time tracker
            CancelInteraction(true);
            if(!forceStopBuild)
                taskProgressBar?.EndTask();
            print("stopped building");
        }

        public void StartNewJob(CharacterJob job)
        {
            if (currentJob.jobType == CharacterJobType.NONE) return;

            //currentJobType = jobType;
        }

        public void StopCurrentJob(bool forceStop = false)
        {
            if (currentJob.jobType == CharacterJobType.NONE) return;
            currentJob.jobType = CharacterJobType.NONE;
            currentState = CharacterState.NONCLIMBING;
            jobTimer = 0;
            if (!forceStop)
                if (taskProgressBar != null) taskProgressBar.EndTask();
        }

        public void UpdatePlayerNameDisplay()
        {
            playerNameText.text = playerData.GetPlayerName();
        }

        public PlayerData GetPlayerData() => playerData;

        protected override void OnCharacterDeath(bool respawn = false)
        {
            base.OnCharacterDeath(respawn);

            //Death Haptics
            HapticsSettings setting = GameManager.Instance.SystemEffects.GetHapticsSetting("ImpactRumble");
            GameManager.Instance.SystemEffects.ApplyControllerHaptics(this.GetPlayerData().playerInput, setting); //Apply haptics

            if (permaDeath)
            {
                OnPlayerDeath?.Invoke();
            }
        }

        public override void InterruptRespawn()
        {
            base.InterruptRespawn();

            OnPlayerDeath?.Invoke();
        }

        protected override void ResetPlayer()
        {
            base.ResetPlayer();

            interactInputHeld = false;
            jetpackInputHeld = false;
            isShaking = false;
            characterVisualParent.localPosition = Vector3.zero;

            if (currentObject != null)
                currentObject.Drop(this, true, moveInput);

            if (TankManager.Instance != null)
                SetAssignedTank(TankManager.Instance.playerTank);
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}