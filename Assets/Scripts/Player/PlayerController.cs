using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts.Deprecated
{
    public struct PlayerButtonPrompt
    {
        public Sprite buttonSprite;
        public string buttonText;
    }

    public class PlayerController : SerializedMonoBehaviour
    {
        public enum PlayerActions { NONE, BUILDING, REPAIRING, EXTINGUISHING, SELLING, INTERACTING };

        [Header("Movement Settings")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float distance = 3f;
        [SerializeField] private Vector2 throwForceRange;
        [SerializeField] private float throwForce;
        [SerializeField] private LayerMask ladderMask;
        [SerializeField] private float downSpeedMultiplier = 1.25f;
        [Space(10)]

        [Header("Technical Settings")]
        [DictionaryDrawerSettings(KeyLabel = "Player Action", ValueLabel = "Player Binding")]
        public Dictionary<PlayerActions, PlayerButtonPrompt> playerButtonPrompts = new Dictionary<PlayerActions, PlayerButtonPrompt>();
        [SerializeField] private float timeToUseWrench = 3;
        [SerializeField] private float fireRemoverSpeed = 3;
        [SerializeField] private float timeToBuild = 3;
        [SerializeField] private float timeToSell = 3;
        [SerializeField] private int maxAmountOfScrap = 8;
        [SerializeField, Tooltip("The amount of scrap used to repair up to 25% of a layer's health.")] private int scrapToRepair = 1;
        [SerializeField, Tooltip("The fill image.")] private Image progressFill;
        [SerializeField, Tooltip("The color for the sell progress ba.r")] private Color sellColor;
        [Space(10)]

        [Header("Joystick Spin Detection Options")]
        [SerializeField] private float spinAngleCheckUpdateTimer = 0.1f;
        [SerializeField] [Range(0.0f, 180.0f)] private float spinValidAngleLimit = 30.0f;
        [SerializeField] private int validSpinCheckRows = 1;
        [SerializeField] private float cannonScrollSensitivity = 3f;
        [Space(10)]

        [Header("Particle Effects")]
        [SerializeField] private GameObject buildscrap;
        [SerializeField] private GameObject firefoam;
        [SerializeField] private GameObject leftfirefoam;
        [SerializeField] private GameObject smabuildscraps;
        public Transform particleSpawn; //place to spawn certain particles during animations

        //Runtime Variables
        private int playerIndex;
        private InputActionMap inputMap;
        private PlayerHUD playerHUD;

        //Movement
        private Vector2 movement;
        internal float steeringValue;
        private Rigidbody2D rb;
        private Animator playerAnimator;

        internal int previousLayer = 0;
        internal int currentLayer = 0;

        private bool canMove = false;
        private bool isFacingRight;
        private bool canClimb;
        private bool isClimbing;
        private bool waitingToClimb;
        private float defaultGravity;
        private RaycastHit2D ladderRaycast;
        private bool isRepairingLayer;
        private Color defaultProgressColor;

        //Interactables
        internal InteractableController currentInteractableItem;
        internal PriceIndicator currentInteractableToBuy;
        private GameObject interactableHover;
        private GameObject scrapNumber;
        private int scrapValue;
        private GameObject buildIndicator;
        private GameObject progressBarCanvas;
        private Slider progressBarSlider;
        private bool taskInProgress;
        private IEnumerator currentLoadAction;
        private float currentActionSpeed;
        private bool buildModeActive;

        //Items
        private Transform scrapHolder;
        public SpriteRenderer scrapBag;
        private Item closestItem;
        private Item itemHeld;
        private bool isHoldingItem;

        //Joystick spin detection
        private Vector2 lastJoystickInput = Vector2.zero;
        private bool isCheckingSpinInput = false;
        private int validSpinCheckCounter = 0;
        private float cannonScroll;

        private bool isSpinningCannon = false;

        private PlayerInput playerInputComponent;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            rb = GetComponent<Rigidbody2D>();
            playerAnimator = GetComponent<Animator>();

            defaultGravity = rb.gravityScale;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name == "GameScene")
            {
                ResetPlayer();
                GetComponent<SpriteRenderer>().flipX = false;
                interactableHover.SetActive(false);

                if (IsHoldingScrap())
                    foreach (var scrap in scrapHolder.GetComponentsInChildren<Rigidbody2D>())
                        Destroy(scrap.gameObject);

                OnScrapUpdated(0);
            }
        }

        /// <summary>
        /// Calls events for the player.
        /// </summary>
        /// <param name="ctx">The context of the input that was triggered.</param>
        private void OnPlayerInput(InputAction.CallbackContext ctx)
        {
            //Gets the name of the action and calls the appropriate events
            switch (ctx.action.name)
            {
                case "Move": OnMove(ctx); break;
                case "Repair": OnRepair(ctx); OnUseInteractable(ctx); break;
                case "Interact": OnInteract(ctx); OnUseInteractable(ctx); break;
                case "Build": OnBuild(ctx); OnUseInteractable(ctx); break;
                case "Cancel": OnCancel(ctx); break;
                case "Pause": OnPause(ctx); break;
                case "Control Steering": OnControlSteering(ctx); break;
                case "Cycle Interactables": OnCycleInteractable(ctx); break;
                case "On Ladder Enter": OnLadderEnter(ctx); break;
                case "On Ladder Exit": OnLadderExit(ctx); break;
                case "Cannon Scroll": OnCannonScroll(ctx); break;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            ResetPlayer();
        }

        private void ResetPlayer()
        {
            scrapHolder = transform.Find("ScrapHolder");
            interactableHover = transform.Find("HoverPrompt").gameObject;
            scrapNumber = transform.Find("ScrapNumber").gameObject;
            buildIndicator = transform.Find("BuildIndicator").gameObject;
            progressBarCanvas = transform.Find("TaskProgressBar").gameObject;
            progressBarSlider = progressBarCanvas.GetComponentInChildren<Slider>();

            previousLayer = 0;
            currentLayer = 0;
            isHoldingItem = false;
            canClimb = false;
            waitingToClimb = false;
            isFacingRight = true;
            buildModeActive = false;

            defaultProgressColor = progressFill.color;
            rb.gravityScale = defaultGravity;
        }

        // Update is called once per frame
        private void Update()
        {
            if (PlayerCanInteract())
            {
                if (playerInputComponent.currentControlScheme == "Gamepad")
                {
                    //Check to see if the joystick is spinning
                    CheckJoystickSpinning();
                }
                else if (playerInputComponent.currentControlScheme == "Keyboard and Mouse")
                {
                    //Check to see if the mouse scroll is scrolling
                    CheckMouseScroll();
                }
            }
        }

        // For dealing with physics or movement related functionality
        void FixedUpdate()
        {
            if (LevelManager.Instance != null && !GameManager.Instance.isPaused)
            {
                //Move the player horizontally
                if (canMove)
                {
                    playerAnimator.SetFloat("PlayerX", Mathf.Abs(movement.x));
                    //Debug.Log("Player Can Move!");
                    rb.velocity = new Vector2(movement.x * speed, rb.velocity.y);


                    //Clamp the player's position to be within the range of the tank
                    Vector3 playerPos = transform.localPosition;

                    //playerPos.x = Mathf.Clamp(playerPos.x, -LevelManager.Instance.GetPlayerTank().tankBarrierRange, LevelManager.Instance.GetPlayerTank().tankBarrierRange);
                    transform.localPosition = playerPos;

                    //If the input is moving the player right and the player is facing left
                    if (movement.x > 0 && !isFacingRight)
                        Flip();

                    //If the input is moving the player left and the player is facing right
                    else if (movement.x < 0 && isFacingRight)
                        Flip();
                }
                else
                {
                    playerAnimator.SetFloat("PlayerX", 0);
                    rb.velocity = Vector2.zero;
                }

                //Check to see if the player is colliding with the ladder
                ladderRaycast = Physics2D.Raycast(transform.position, Vector2.up, distance, ladderMask);

                Debug.DrawRay(transform.position, Vector2.up, ladderRaycast.collider == null ? Color.red : Color.green, distance);

                if (ladderRaycast.collider != null)
                    Debug.Log("Omg a " + ladderRaycast.collider.name + "!");

                //If the player is not colliding with the ladder
                if (ladderRaycast.collider == null && isClimbing)
                {
                    ExitLadder();
                }
                //If the player is trying to climb and can climb, let them
                else if (waitingToClimb && ladderRaycast.collider != null)
                {
                    EnterLadder();
                }

                //If the player is climbing, move up and get rid of gravity temporarily
                if (isClimbing)
                {
                    rb.velocity = new Vector2(0, movement.y > 0 ? movement.y * speed : movement.y * speed * downSpeedMultiplier);
                    rb.gravityScale = 0;
                    playerAnimator.SetFloat("PlayerY", movement.y);
                    if (IsHoldingScrap()) scrapBag.enabled = true;
                }

                //Once the player stops climbing, bring back gravity
                else
                {
                    rb.gravityScale = defaultGravity;
                    playerAnimator.SetFloat("PlayerY", 0);
                    scrapBag.enabled = false;
                }
            }
            else
                playerAnimator.SetFloat("PlayerX", 0);
        }

        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            isFacingRight = !isFacingRight;

            if (isFacingRight)
            {
                GetComponent<SpriteRenderer>().flipX = false;
                transform.Find("Outline").GetComponent<SpriteRenderer>().flipX = false;
                AdjustScrapHolderPosition(new Vector2(-scrapHolder.localPosition.x, scrapHolder.localPosition.y));
            }
            else
            {
                GetComponent<SpriteRenderer>().flipX = true;
                transform.Find("Outline").GetComponent<SpriteRenderer>().flipX = true;
                AdjustScrapHolderPosition(scrapHolder.localPosition);
            }
        }

        public void AdjustScrapHolderPosition(Vector2 newPosition)
        {
            scrapHolder.localPosition = new Vector2(isFacingRight ? newPosition.x : -newPosition.x, newPosition.y);
        }

        public void AdjustScrapHolderPositionX(float newX)
        {
            scrapHolder.localPosition = new Vector2(isFacingRight ? newX : -newX, scrapHolder.localPosition.y);
        }

        public void AdjustScrapHolderPositionY(float newY)
        {
            scrapHolder.localPosition = new Vector2(scrapHolder.localPosition.x, newY);
        }

        public void AdjustParticleSpawnX(float newX)
        {
            particleSpawn.localPosition = new Vector2(isFacingRight ? newX : -newX, particleSpawn.localPosition.y);
        }

        public void AdjustParticleSpawnY(float newY)
        {
            particleSpawn.localPosition = new Vector2(particleSpawn.localPosition.x, newY);
        }

        #region OnInputFunctions

        public void LinkPlayerInput(PlayerInput newInput)
        {
            playerInputComponent = newInput;
            playerIndex = playerInputComponent.playerIndex;

            //Gets the player input action map so that events can be subscribed to it
            inputMap = playerInputComponent.actions.FindActionMap("Player");
            inputMap.actionTriggered += OnPlayerInput;

            //Subscribes events for control lost / regained
            playerInputComponent.onDeviceLost += OnDeviceLost;
            playerInputComponent.onDeviceRegained += OnDeviceRegained;
        }

        public void LinkPlayerHUD(PlayerHUD newHUD)
        {
            playerHUD = newHUD;
            playerHUD.InitializeHUD(playerIndex);
        }

        //Send value from Move callback to the horizontal Vector2
        public void OnMove(InputAction.CallbackContext ctx) => movement = ctx.ReadValue<Vector2>();

        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (PlayerCanInteract())
            {
                //If the player presses the interact button
                if (ctx.started)
                {
                    //If there is something to interact with
                    if (currentInteractableItem != null)
                    {
                        //Call the interaction event
                        currentInteractableItem.OnInteraction(this);
                        ShowScrap(false);
                    }
                }

                if (ctx.performed || ctx.canceled)
                {
                    if (taskInProgress && currentInteractableItem != null)
                    {
                        //Call the cancel interaction event
                        currentInteractableItem.OnCancel();
                        ShowScrap(true);
                    }
                }
            }
        }

        public void OnUseInteractable(InputAction.CallbackContext ctx)
        {
            if (PlayerCanInteract())
            {
                //If the player presses any main button but the cancel button
                if (ctx.started)
                {
                    if (currentInteractableItem != null)
                    {
                        currentInteractableItem.OnUseInteractable();
                        ShowScrap(true);
                    }
                }
            }
        }

        public void OnRepair(InputAction.CallbackContext ctx)
        {
            if (PlayerCanInteract() && canMove)
            {
                if (ctx.started)
                {
                    //If the fire remover is successfully used, do not do anything use
                    if (CheckForFireRemoverUse())
                        return;

                    //If the layer is being properly built, return
                    if (CheckForWrenchUse())
                        return;
                }
            }

            if (ctx.canceled)
            {
                if (taskInProgress)
                    CancelProgressBar();
            }
        }

        public void OnBuild(InputAction.CallbackContext ctx)
        {
            if (PlayerCanInteract())
            {
                if (ctx.started)
                {
                    //Try to buy an interactable
                    foreach (var i in GameObject.FindGameObjectsWithTag("GhostObject"))
                    {
                        //If a player can purchase an interactable, try to purchase it
                        if (i.GetComponent<PriceIndicator>().PlayerCanPurchase(this))
                        {
                            i.GetComponent<PriceIndicator>().PurchaseInteractable();
                            //Instantiate(smabuildscraps, transform.position, Quaternion.identity);
                        }
                    }

                    //If the player is outside of the tank, try to build a layer
                    if (IsPlayerOutsideTank() && canMove)
                        BuildLayer();
                }

                if (ctx.canceled)
                {
                    if (taskInProgress)
                        CancelProgressBar();
                }
            }
        }

        public void OnCancel(InputAction.CallbackContext ctx)
        {
            if (PlayerCanInteract())
            {
                if (ctx.started)
                {
                    //If the player is locked into an interactable, end the interaction
                    if (currentInteractableItem != null && !canMove)
                    {
                        currentInteractableItem.OnEndInteraction(this);
                        return;
                    }

                    //If the player is not in build mode or interacting with anything, throw any scrap that they may have on them
                    if (IsHoldingScrap() && !isClimbing)
                    {
                        //Throw each scrap piece and set it to despawn
                        foreach (var scrap in scrapHolder.GetComponentsInChildren<Rigidbody2D>())
                        {
                            scrap.isKinematic = false;
                            float throwAngle = UnityEngine.Random.Range(throwForceRange.x, throwForceRange.y);
                            Vector2 throwVector = GetThrowVector(throwAngle * Mathf.Deg2Rad);

                            //If the player is facing left, flip the x
                            if (!isFacingRight)
                                throwVector = new Vector2(-throwVector.x, throwVector.y);

                            scrap.AddForce(throwVector * (throwForce * 100));

                            scrap.transform.SetParent(null);
                            scrap.GetComponent<ScrapPiece>().DespawnScrap();
                        }

                        OnScrapUpdated(0);  //Reset the scrap number
                    }

                    //If the player is near an interactable, try to sell it
                    if (currentInteractableItem != null && canMove)
                    {
                        StartInteractableSell();
                        return;
                    }
                }
                if (ctx.canceled)
                {
                    if (taskInProgress)
                        CancelProgressBar();
                }
            }
        }

        public void OnCycleInteractable(InputAction.CallbackContext ctx)
        {
            if (PlayerCanInteract())
            {
                //If the player presses the cycle interactable button
                if (ctx.performed)
                {
                    if (IsHoldingScrap() && currentInteractableToBuy != null)
                    {
                        int incrementValue = ctx.ReadValue<float>() < 0 ? -1 : 1;
                        FindObjectOfType<InteractableSpawnerManager>().UpdateGhostInteractable(currentInteractableToBuy.transform.parent.GetComponent<InteractableSpawner>(), incrementValue);
                    }
                }
            }
        }

        public void OnLadderEnter(InputAction.CallbackContext ctx)
        {
            if (LevelManager.Instance != null && !GameManager.Instance.isPaused)
            {
                //If the player presses the ladder climb button
                if (ctx.performed)
                {
                    if (canClimb)
                    {
                        //If the player is not on a ladder
                        if (!isClimbing)
                        {
                            //If the player is colliding with the ladder and wants to climb
                            if (ladderRaycast.collider != null)
                            {
                                Debug.Log("Gonna Climb");
                                EnterLadder();
                                return;
                            }
                        }
                    }
                    waitingToClimb = true;
                }
            }
        }

        public void OnLadderExit(InputAction.CallbackContext ctx)
        {
            if (LevelManager.Instance != null && !GameManager.Instance.isPaused)
            {
                //If the player presses the ladder climb button
                if (ctx.performed)
                {
                    if (canClimb)
                    {
                        //If the player is on a ladder
                        if (isClimbing)
                        {
                            ExitLadder();
                        }
                    }
                }
            }
        }

        public void OnControlSteering(InputAction.CallbackContext ctx) => steeringValue = ctx.ReadValue<float>();
        public void OnCannonScroll(InputAction.CallbackContext ctx) => cannonScroll = ctx.ReadValue<float>();

        public void OnPause(InputAction.CallbackContext ctx)
        {
            //If the player presses the pause button
            if (ctx.started)
            {
                if (LevelManager.Instance != null && !GameManager.Instance.isPaused)
                {
                    //Pause the game
                    Debug.Log("Player " + (playerIndex + 1).ToString() + " Paused.");
                    //LevelManager.Instance?.PauseToggle(playerIndex);
                }
            }
        }
        #endregion

        /// <summary>
        /// Sets the build mode for the player.
        /// </summary>
        /// <param name="buildMode">If true, the player is in build mode. If false, the player is not in build mode.</param>
        public void SetBuildMode(bool buildMode)
        {
            buildModeActive = buildMode;    //Sets the build mode for the player
            buildIndicator.SetActive(buildMode);

            //Debug.Log("Player Layer: " + currentLayer);
            //Debug.Log("Outside Tank: " + IsPlayerOutsideTank());

            //If build mode is active
            if (buildModeActive)
            {
                /*
                Debug.Log("Build Mode For Player " + (playerIndex + 1) + ": On");
                if (!IsPlayerOutsideTank())
                    LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer).GetComponent<GhostInteractables>().CreateGhostInteractables(this);
                else if(IsHoldingScrap())
                    LevelManager.Instance.AddGhostLayer();
                */
            }

            //If build mode is not active
            else
            {
                /*
                Debug.Log("Build Mode For Player " + (playerIndex + 1) + ": Off");
                if(!IsPlayerOutsideTank())
                    LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer).GetComponent<GhostInteractables>().DestroyGhostInteractables(this);
                else
                    LevelManager.Instance.RemoveGhostLayer();
                */
            }
        }

        private void EnterLadder()
        {
            isClimbing = true;
            playerAnimator.SetBool("IsOnLadder", true);
            transform.localPosition = new Vector2(0, transform.localPosition.y);
            waitingToClimb = false;
            ShowScrap(false);
        }

        private void ExitLadder()
        {
            //Move them off the ladder
            isClimbing = false;
            playerAnimator.SetBool("IsOnLadder", false);
            ShowScrap(true);
        }

        public float GetCannonMovement()
        {
            if (playerInputComponent.currentControlScheme == "Gamepad")
            {
                //If the player is spinning the joystick and cannot move, send the cannon the player's joystick spin angle
                if (isSpinningCannon && !canMove)
                {
                    return Vector2.SignedAngle(lastJoystickInput, movement);
                }
            }
            else if (playerInputComponent.currentControlScheme == "Keyboard and Mouse")
            {
                if (isSpinningCannon && !canMove)
                {
                    return cannonScroll * cannonScrollSensitivity;
                }
            }

            return 0;
        }

        public bool IsPlayerSpinningCannon() => isSpinningCannon && !canMove;

        private void ChangeItemTransparency(float alpha)
        {
            if (itemHeld != null)
            {
                Color currentColor = itemHeld.GetComponentInChildren<SpriteRenderer>().color;
                currentColor.a = alpha;
                itemHeld.GetComponentInChildren<SpriteRenderer>().color = currentColor;
            }
        }

        /// <summary>
        /// Starts the process of building a layer if the player has the resources to do so.
        /// </summary>
        private void BuildLayer()
        {
            //If the player is outside of the tank and can afford a new layer
            if (scrapValue >= LevelManager.Instance.GetItemPrice("NewLayer"))
            {
                if (LevelManager.Instance.levelPhase != GAMESTATE.GAMEOVER || LevelManager.Instance.totalLayers < 2)
                {
                    if (timeToBuild > 0)
                        StartProgressBar(timeToBuild, false, PurchaseLayer, PlayerActions.BUILDING);
                }
            }
        }

        private void StartInteractableSell()
        {
            if (LevelManager.Instance.levelPhase != GAMESTATE.GAMEOVER && !currentInteractableItem.AnyPlayersLockedIn() && currentInteractableItem.CanBeSold())
            {
                if (timeToSell > 0)
                    StartProgressBar(timeToSell, false, SellInteractable, PlayerActions.SELLING);
            }
        }

        private void SellInteractable()
        {
            if (currentInteractableItem != null)
                currentInteractableItem.Sell();
        }

        /// <summary>
        /// Purchases a new layer using the player's scrap.
        /// </summary>
        private void PurchaseLayer()
        {
            UseScrap(LevelManager.Instance.GetItemPrice("NewLayer"));
            LevelManager.Instance.PurchaseLayer(this);
        }

        private bool CheckForFireRemoverUse()
        {
            if (IsPlayerOutsideTank()) return false;  //If the player is outside of the tank, return false
            if (buildModeActive) return false;        //If the player is in build mode, return false

            FireBehavior fire = null;
            /*if (LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer) != null)
                fire = LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer).GetComponentInChildren<FireBehavior>();*/

            //If the layer the player is on is on fire
            if (fire != null && fire.IsLayerOnFire())
            {
                if (fireRemoverSpeed > 0)
                {
                    isRepairingLayer = true;
                    StartProgressBar(fireRemoverSpeed, true, UseFireRemover, PlayerActions.EXTINGUISHING);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancels any active repairs that the player is making.
        /// </summary>
        public void CancelLayerRepair()
        {
            if (isRepairingLayer && taskInProgress)
                CancelProgressBar();
        }

        private bool CheckForWrenchUse()
        {
            if (IsPlayerOutsideTank()) return false;  //If the player is outside of the tank, return false

            /*LayerManager layerHealth = LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer);

            //If the layer the player is damaged and the player has scrap, use the wrench
            if (layerHealth.GetLayerHealth() < layerHealth.GetMaxHealth() && scrapHolder.childCount > 0)
            {
                if (timeToUseWrench > 0)
                {
                    isRepairingLayer = true;
                    StartProgressBar(timeToUseWrench, false, UseWrench, PlayerActions.REPAIRING);
                }
                return true;
            }
            */

            return false;
        }

        private void UseFireRemover()
        {
            /*
            FireBehavior fire = LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer).GetComponentInChildren<FireBehavior>();
            fire.gameObject.SetActive(false);   //Gets rid of the fire
            isRepairingLayer = false;

            LevelManager.Instance.CancelAllLayerRepairs();
            */
        }

        private void UseWrench()
        {
            //Restore the layer the player is on to max health
            /*LayerManager layerHealth = LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer);

            int healthToMax = layerHealth.GetMaxHealth() - layerHealth.GetLayerHealth();    //Get the amount of health needed to repair the layer to max
            int scrapRequired = (int)Mathf.CeilToInt(healthToMax / 25f) * scrapToRepair;    //Get the amount of scrap required to repair to max health (x scrap per 25% health)

            //If the player does not have enough scrap to repair, use up as much as possible
            if(scrapRequired > scrapHolder.childCount)
                scrapRequired = scrapHolder.childCount;

            int price = scrapRequired * 25;

            layerHealth.RepairLayer(price);
            isRepairingLayer = false;

            //Use some scrap
            UseScrap(price);

            LevelManager.Instance.CancelAllLayerRepairs();
            */
        }

        public void DisplayPlayerAction(PlayerActions currentAction)
        {
            //interactableHover.transform.Find("Prompt").GetComponent<TextMeshProUGUI>().text = message;
            //interactableHover.SetActive(true);

            PlayerButtonPrompt currentButtonPromptData;

/*            if (currentAction == PlayerActions.NONE)
                playerHUD.ShowButtonPrompt(null);
            else
            {
                playerButtonPrompts.TryGetValue(currentAction, out currentButtonPromptData);
                playerHUD.ShowButtonPrompt(currentButtonPromptData.buttonSprite);
            }*/
        }

        public void StartProgressBar(float interval, bool useSpeed, Action actionOnComplete, PlayerActions currentPlayerAction = PlayerActions.NONE)
        {
            if (!taskInProgress)
            {
                taskInProgress = true;
                progressBarCanvas.SetActive(true);
                progressBarSlider.value = 0;
                if (useSpeed)
                    currentLoadAction = SpeedProgressBarLoad(actionOnComplete);
                else
                    currentLoadAction = SecondsProgressBarLoad(interval, actionOnComplete);
                canMove = false;

                ShowScrap(false);

                //Player an animation based on the action that the player is performing
                switch (currentPlayerAction)
                {
                    case PlayerActions.BUILDING:
                        playerAnimator.SetBool("IsBuilding", true);
                        break;
                    case PlayerActions.REPAIRING:
                        playerAnimator.SetBool("IsRepairing", true);
                        break;
                    case PlayerActions.EXTINGUISHING:
                        playerAnimator.SetBool("IsExtinguishing", true);
                        currentActionSpeed = fireRemoverSpeed;
                        //LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer).GetComponentInChildren<FireBehavior>().AddPlayerPuttingOutFire(this);
                        break;
                    case PlayerActions.SELLING:
                        progressFill.color = sellColor;
                        break;
                }

                StartCoroutine(currentLoadAction);
                //Instantiate(buildscrap, transform.position, Quaternion.identity);
            }
        }

        IEnumerator SecondsProgressBarLoad(float secondsTillCompletion, Action actionOnComplete)
        {
            float currentPercentage = 0;
            float completionRate = secondsTillCompletion / 100;

            //While the task is in progress and the task bar is not full, fill the task bar
            while (progressBarSlider.value < 100 && taskInProgress)
            {
                currentPercentage += (1 / completionRate) * Time.deltaTime;
                progressBarSlider.value = Mathf.Clamp(currentPercentage, 0, 100);
                yield return null;
            }

            ChangeItemTransparency(1.0f);
            actionOnComplete.Invoke();
            ResetAnimatorBools();
            HideProgressBar();
            ShowScrap(true);
            canMove = true;
            progressFill.color = defaultProgressColor;
        }

        IEnumerator SpeedProgressBarLoad(Action actionOnComplete)
        {
            float currentPercentage = 0;

            //While the task is in progress and the task bar is not full, fill the task bar
            while (progressBarSlider.value < 100 && taskInProgress)
            {
                Debug.Log("Current Action Speed: " + currentActionSpeed);
                Debug.Log("Action Speed Increment: " + currentActionSpeed * Time.deltaTime);

                currentPercentage += currentActionSpeed * Time.deltaTime;
                progressBarSlider.value = Mathf.Clamp(currentPercentage, 0, 100);
                yield return null;
            }

            ChangeItemTransparency(1.0f);
            actionOnComplete.Invoke();
            ResetAnimatorBools();
            HideProgressBar();
            ShowScrap(true);
            canMove = true;
            progressFill.color = defaultProgressColor;
        }

        public void CancelProgressBar()
        {
            if (!taskInProgress)
                return;

            StopCoroutine(currentLoadAction);
            taskInProgress = false;
            ShowScrap(true);

            if (playerAnimator.GetBool("IsExtinguishing"))
            {
                /*
                FireBehavior fire = LevelManager.Instance.GetPlayerTank().GetLayerAt(currentLayer).GetComponentInChildren<FireBehavior>();
                if (fire != null)
                    fire.RemovePlayerPuttingOutFire(this);
                */
            }

            ResetAnimatorBools();
            HideProgressBar();
            canMove = true;
            progressFill.color = defaultProgressColor;
        }

        private void ResetAnimatorBools()
        {
            playerAnimator.SetBool("IsBuilding", false);
            playerAnimator.SetBool("IsRepairing", false);
            playerAnimator.SetBool("IsExtinguishing", false);
        }

        public void HideProgressBar()
        {
            progressBarCanvas.SetActive(false);
            progressBarSlider.value = 0;
        }

        public void ShowProgressBar()
        {
            progressBarCanvas.SetActive(true);
            progressBarSlider.value = 0;
        }

        public bool IsProgressBarActive() => progressBarCanvas.activeInHierarchy;

        public void AddToProgressBar(float value)
        {
            progressBarSlider.value += value;
        }

        public bool IsProgressBarFull() => progressBarSlider.value >= 100;

        public void DestroyItem()
        {
            Destroy(itemHeld.gameObject);
            itemHeld = null;
            isHoldingItem = false;
        }

        public void PickupItem()
        {
            Debug.Log("Is Holding Item: " + isHoldingItem);

            //If the player is not holding an item
            if (!isHoldingItem)
            {
                //If the closest item to the player exists and is not picked up
                if (closestItem != null && !closestItem.IsItemPickedUp())
                {
                    TakeItem();
                }
            }
            else
            {
                //If the player is still holding an item
                if (itemHeld != null)
                {
                    DropItem(false);
                }
            }
        }

        private void TakeItem()
        {
            itemHeld = closestItem;

            //Make the current item the player's child and stick it to the player
            itemHeld.transform.position = gameObject.transform.position;
            itemHeld.transform.rotation = Quaternion.identity;
            itemHeld.GetComponent<Rigidbody2D>().gravityScale = 0;
            //itemHeld.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            //itemHeld.GetComponent<Rigidbody2D>().angularVelocity = 0;
            itemHeld.GetComponent<Rigidbody2D>().isKinematic = true;
            itemHeld.transform.parent = gameObject.transform;
            itemHeld.SetRotateConstraint(true);

            Debug.Log("Picked Up Item!");

            if (PlayerHasItem("Hammer") && !IsPlayerOutsideTank())
            {
                //LevelManager.instance.CheckInteractablesOnLayer(currentLayer);
            }
            else if (PlayerHasItem("Hammer") && IsPlayerOutsideTank())
            {
                LevelManager.Instance.AddGhostLayer();
            }

            /*        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
                    {
                        if (TutorialController.main.currentTutorialState == TUTORIALSTATE.PICKUPHAMMER && PlayerHasItem("Hammer"))
                        {
                            //Tell tutorial that task is complete
                            TutorialController.main.OnTutorialTaskCompletion();
                        }
                    }*/

            //If the item can deal damage, remove that
            if (itemHeld.GetComponent<DamageObject>() != null)
            {
                Destroy(itemHeld.GetComponent<DamageObject>());
            }

            GameManager.Instance.AudioManager.Play("ItemPickup");

            itemHeld.SetPickUp(true);
            isHoldingItem = true;
            closestItem = null;
        }

        /// <summary>
        /// Function called when the player gains or loses scrap in their scrap holder.
        /// </summary>
        /// <param name="newScrapChildCount">The new child count of the scrap holder.</param>
        public void OnScrapUpdated(int newScrapChildCount = -1)
        {
            //If the player is holding scrap
            if (IsHoldingScrap() || newScrapChildCount != 0)
            {
                //If the player is outside of the tank, add a ghost layer
                if (IsPlayerOutsideTank())
                    LevelManager.Instance.AddGhostLayer();

                if (!scrapNumber.activeInHierarchy)
                    scrapNumber.SetActive(true);

                if (newScrapChildCount == -1)
                    scrapValue = scrapHolder.childCount * LevelManager.Instance.GetScrapValue();
                else
                    scrapValue = newScrapChildCount * LevelManager.Instance.GetScrapValue();
            }
            //If they are not, their scrap value is 0
            else
                scrapValue = 0;

            scrapNumber.GetComponentInChildren<TextMeshProUGUI>().text = scrapValue.ToString();
            //playerHUD.UpdateScrapCount(scrapValue);

            //If the player has no more scrap
            if (scrapValue <= 0 && scrapNumber.activeInHierarchy)
            {
                scrapNumber.SetActive(false);
                //Make sure that they cannot try to buy anything
                foreach (var priceIndicator in FindObjectsOfType<PriceIndicator>())
                    priceIndicator.ReleasePlayerFromBuying(this, false);

                SetBuildMode(false);
            }

            playerAnimator.SetBool("IsHoldingScrap", scrapValue > 0); //Show scrap animations depending on whether they're holding scrap or not
        }

        /// <summary>
        /// Sets whether the scrap should be shown or not.
        /// </summary>
        /// <param name="showScrap">If true, the scrap is shown. If false, the scrap is hidden.</param>
        public void ShowScrap(bool showScrap)
        {
            if (!IsHoldingScrap()) return; //If the player has no scrap, return

            foreach (var scrap in scrapHolder.GetComponentsInChildren<Transform>())
                scrap.GetComponentInChildren<SpriteRenderer>().enabled = showScrap;
        }

        /// <summary>
        /// Uses the players scrap for an action.
        /// </summary>
        /// <param name="price">The price of the action that uses scrap.</param>
        public void UseScrap(int price)
        {
            int numberOfScrapsUsed = price / LevelManager.Instance.GetScrapValue();

            //Debug.Log("Destroying " + numberOfScrapsUsed + " Scrap");

            int initialScrapCount = scrapHolder.childCount - 1;

            //Destroys the scrap in increments of their value
            for (int i = 0; i < numberOfScrapsUsed; i++)
                Destroy(scrapHolder.GetChild(initialScrapCount - i).gameObject);

            OnScrapUpdated(initialScrapCount - numberOfScrapsUsed + 1);
        }

        private void DropItem(bool throwItem)
        {
            /*
            //Remove the item from the player and put it back in the level
            itemHeld.GetComponent<Rigidbody2D>().gravityScale = itemHeld.GetDefaultGravityScale();
            itemHeld.GetComponent<Rigidbody2D>().isKinematic = false;
            itemHeld.transform.parent = LevelManager.Instance.GetPlayerTank().GetItemContainer();
            itemHeld.SetRotateConstraint(false);
            ChangeItemTransparency(1);

            Debug.Log("Dropped Item!");

            if (taskInProgress)
            {
                CancelProgressBar();
            }

            if (PlayerHasItem("Hammer"))
            {
                foreach (var i in GameObject.FindGameObjectsWithTag("GhostObject"))
                {
                    Destroy(i);
                }
                LevelManager.Instance.RemoveGhostLayer();
            }

            itemHeld.SetPickUp(false);
            isHoldingItem = false;

            //If the player is supposed to throw the item, add some force
            if (throwItem)
            {
                //If the item is a shell, make it do damage
                if(itemHeld.GetComponent<ShellItemBehavior>() != null)
                    itemHeld.gameObject.AddComponent<DamageObject>().damage = itemHeld.GetComponent<ShellItemBehavior>().GetDamage();

                Vector2 throwVector = GetThrowVector(UnityEngine.Random.Range(throwForceRange.x, throwForceRange.y) * Mathf.Deg2Rad);

                //If the player is facing left, flip the x
                if (!isFacingRight)
                    throwVector = new Vector2(-throwVector.x, throwVector.y);

                itemHeld.GetComponent<Rigidbody2D>().AddForce(throwVector * (throwForce * 100));
            }

            itemHeld = null;
            */
        }

        private Vector2 GetThrowVector(float radians)
        {
            //Trigonometric function to get the vector (hypotenuse) of the current throw angle
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private void CheckJoystickSpinning()
        {
            //If the current movement vector is different from the previous movement vector and spinning input is not being checked
            if (movement != lastJoystickInput && !isCheckingSpinInput)
            {
                //Check for spin input
                isCheckingSpinInput = true;
                StartCoroutine(JoystickSpinningDetection());
            }

            //If the number of spin checks is equal to number of spins that are needed, the joystick has been properly spun
            if (validSpinCheckCounter == validSpinCheckRows)
            {
                isSpinningCannon = true;
            }

            //If not, the joystick is not spinning properly
            else
            {
                isSpinningCannon = false;
            }
        }

        private IEnumerator JoystickSpinningDetection()
        {
            //Store the movement variable for later use
            lastJoystickInput = movement;

            //Wait for a bit to check for a spin angle
            yield return new WaitForSeconds(spinAngleCheckUpdateTimer);

            //If the angle between the last known movement vector and the current movement vector reaches a specified amount
            if (Vector2.Angle(lastJoystickInput, movement) >= spinValidAngleLimit)
            {
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

        private void CheckMouseScroll()
        {
            if (cannonScroll != 0)
                isSpinningCannon = true;
            else
                isSpinningCannon = false;
        }

        public void PlayFootstepSFX() => GameManager.Instance.AudioManager.Play("Footstep", gameObject);
        public void PlayLadderClimbSFX() => GameManager.Instance.AudioManager.Play("LadderClimb", gameObject);
        public void PlayHammerSFX() => GameManager.Instance.AudioManager.Play("TankImpact", gameObject);
        public void PlayWrenchSFX() => GameManager.Instance.AudioManager.Play("UseWrench", gameObject);

        public void SpawnParticle(GameObject particle)
        {
            if (particle.name == "foam" && !isFacingRight) Instantiate(particle, particleSpawn.position, Quaternion.identity);
            else if (particle.name == "foam" && isFacingRight) Instantiate(leftfirefoam, particleSpawn.position, Quaternion.identity);
            else Instantiate(particle, particleSpawn.position, Quaternion.identity);
        }

        public bool PlayerCanInteract()
        {
            if (LevelManager.Instance != null)
                return !GameManager.Instance.isPaused && !LevelManager.Instance.readingTutorial;

            return false;
        }

        public bool IsPlayerOutsideTank() => currentLayer >= LevelManager.Instance.totalLayers;
        public bool IsPlayerClimbing() => isClimbing;
        public void SetPlayerClimb(bool climb) => canClimb = climb;
        public void SetPlayerMove(bool movePlayer) => canMove = movePlayer;
        public bool InBuildMode() => buildModeActive;
        public bool IsHoldingScrap() => scrapHolder.childCount > 0;
        public int GetScrapValue() => scrapValue;
        public int MaxScrapAmount() => maxAmountOfScrap;
        public void MarkClosestItem(Item item) => closestItem = item;
        public bool IsPlayerHoldingItem() => isHoldingItem;
        public Item GetPlayerItem() => itemHeld;
        public bool PlayerHasItem(string name) => itemHeld != null && itemHeld.CompareTag(name);
        public float GetFireRemoverSpeed() => fireRemoverSpeed;
        public float GetActionSpeed() => currentActionSpeed;
        public void SetActionSpeed(float actionSpeed) => currentActionSpeed = actionSpeed;
        public float GetDefaultGravity() => defaultGravity;

        public PlayerInput GetPlayerInput() => playerInputComponent;
        public int GetPlayerIndex() => playerIndex;
        public void SetPlayerIndex(int index) => playerIndex = index;

        public Color GetPlayerColor() => GetComponent<Renderer>().material.color;

        public void OnDeviceLost(PlayerInput playerInput)
        {
            Debug.Log("Player " + (playerIndex + 1) + " Controller Disconnected!");
            FindObjectOfType<CornerUIController>().OnDeviceLost(playerIndex);
        }

        public void OnDeviceRegained(PlayerInput playerInput)
        {
            Debug.Log("Player " + (playerIndex + 1) + " Controller Reconnected!");
            FindObjectOfType<CornerUIController>().OnDeviceRegained(playerIndex);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            inputMap.actionTriggered -= OnPlayerInput;
            playerInputComponent.onDeviceLost -= OnDeviceLost;
            playerInputComponent.onDeviceRegained -= OnDeviceRegained;
        }
    }
}
