using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayerController : MonoBehaviour
{
    private int playerIndex;

    private Vector2 movement;
    internal float steeringValue;
    [SerializeField] private float speed = 8f;
    private Rigidbody2D rb;
    private PlayerTankController playerTank;
    private Animator playerAnimator;

    internal int currentLayer = 1;

    [SerializeField] private float distance = 3f;
    [SerializeField, Range(0, 360)] private float throwAngle;
    [SerializeField] private float throwForce;
    [SerializeField] private LayerMask ladderMask;
    [SerializeField] private float timeToUseWrench = 3;

    private bool canMove = false;
    private bool hasMoved;
    private bool isFacingRight;
    private bool canClimb;
    private bool isClimbing;
    private float defaultGravity;

    internal InteractableController currentInteractableItem;
    internal ToggleInteractBuy currentInteractableToBuy;

    private Item closestItem;
    private Item itemHeld;
    private bool isHoldingItem;

    private GameObject interactableHover;
    private GameObject progressBarCanvas;
    private Slider progressBarSlider;
    private bool taskInProgress;

    private RaycastHit2D ladderRaycast;
    private IEnumerator currentLoadAction;


    [Header("Joystick Spin Detection Options")]
    [SerializeField] private float spinAngleCheckUpdateTimer = 0.1f;
    [SerializeField][Range(0.0f, 180.0f)] private float spinValidAngleLimit = 30.0f;
    [SerializeField] private int validSpinCheckRows = 1;

    private Vector2 lastJoystickInput = Vector2.zero;
    private bool isCheckingSpinInput = false;
    private int validSpinCheckCounter = 0;

    private bool isSpinningJoystick = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
        playerAnimator = GetComponent<Animator>();
        defaultGravity = rb.gravityScale;
        isHoldingItem = false;
        hasMoved = false;
        canClimb = false;
        isFacingRight = true;
        interactableHover = transform.Find("HoverPrompt").gameObject;
        progressBarCanvas = transform.Find("TaskProgressBar").gameObject;
        progressBarSlider = progressBarCanvas.GetComponentInChildren<Slider>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (UIAllowPlayerInteract())
        {
            //Check to see if the joystick is spinning
            CheckJoystickSpinning();
        }
    }

    void FixedUpdate()
    {
        if (!LevelManager.instance.isPaused)
        {
            //Move the player horizontally
            if (canMove)
            {
                if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
                {
                    //If the player has moved, tell the tutorial state
                    if(MathF.Abs(movement.x) > 0)
                    {
                        hasMoved = true;
                    }
                }

                playerAnimator.SetFloat("PlayerX", MathF.Abs(movement.x));
                //Debug.Log("Player Can Move!");
                rb.velocity = new Vector2(movement.x * speed, rb.velocity.y);

                //Clamp the player's position to be within the range of the tank
                float playerRange = playerTank.transform.position.x +
                    playerTank.tankBarrierRange;
                Vector3 playerPos = transform.position;
                playerPos.x = Mathf.Clamp(playerPos.x, -playerRange, playerRange);
                transform.position = playerPos;

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

            //If the player is not colliding with the ladder
            if (ladderRaycast.collider == null)
            {
                isClimbing = false;
                playerAnimator.SetBool("IsOnLadder", false);
            }

            //If the player is climbing, move up and get rid of gravity temporarily
            if (isClimbing)
            {
                rb.velocity = new Vector2(0, movement.y * speed);
                rb.gravityScale = 0;
                playerAnimator.SetFloat("PlayerY", movement.y);
            }

            //Once the player stops climbing, bring back gravity
            else
            {
                rb.gravityScale = defaultGravity;
                playerAnimator.SetFloat("PlayerY", 0);
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
        }
        else
        {
            GetComponent<SpriteRenderer>().flipX = true;
            transform.Find("Outline").GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    //Send value from Move callback to the horizontal Vector2
    public void OnMove(InputAction.CallbackContext ctx) => movement = ctx.ReadValue<Vector2>();

    public void OnLadderEnter(InputAction.CallbackContext ctx)
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
                        isClimbing = true;
                        playerAnimator.SetBool("IsOnLadder", true);
                        transform.position = new Vector2(0, transform.position.y);
                    }
                }
            }
        }
    }

    public void OnLadderExit(InputAction.CallbackContext ctx)
    {
        //If the player presses the ladder climb button
        if (ctx.performed)
        {
            if (canClimb)
            {
                //If the player is on a ladder
                if (isClimbing)
                {
                    //Move them off the ladder
                    isClimbing = false;
                    playerAnimator.SetBool("IsOnLadder", false);
                }
            }
        }
    }

    public void OnControlSteering(InputAction.CallbackContext ctx) => steeringValue = ctx.ReadValue<float>();

    public float GetCannonMovement()
    {
        //If the player is spinning the joystick and cannot move, send the cannon the player's joystick spin angle
        if (isSpinningJoystick && !canMove)
        {
            return Vector2.SignedAngle(lastJoystickInput, movement);
        }
        else
            return 0;
    }

    public bool IsPlayerSpinningCannon() => isSpinningJoystick && !canMove;

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (UIAllowPlayerInteract())
        {
            //If the player presses the interact button
            if (ctx.started)
            {
                //If there is something to interact with
                if (currentInteractableItem != null)
                {
                    if (currentInteractableItem is CannonController && itemHeld != null && itemHeld.GetComponent<ShellItemBehavior>() != null)
                    {
                        currentInteractableItem.GetComponent<CannonController>().CheckForReload(this);
                    }
                    else
                    {
                        //Call the interaction event
                        currentInteractableItem.OnInteraction(this);
                    }
                }
            }

            if (ctx.performed || ctx.canceled)
            {
                if (taskInProgress && currentInteractableItem != null)
                {
                    //Call the cancel interaction event
                    currentInteractableItem.OnCancel();
                }
            }
        }
    }

    public void OnUse(InputAction.CallbackContext ctx)
    {
        if (UIAllowPlayerInteract())
        {
            //If the player presses the use button
            if (ctx.started)
            {
                //If the player is holding the fire remover and uses the button, check for fire
                if (PlayerHasItem("FireRemover"))
                {
                    CheckForFireRemoverUse();
                }

                //If the player is holding the hammer and uses the button, attempt to add a new layer
                else if (PlayerHasItem("Hammer"))
                {
                    CheckForHammerUse();
                }

                //If the player is near an interactable
                else if (currentInteractableItem != null)
                {
                    //If the interactable is a cannon
                    if (currentInteractableItem.GetComponent<CannonController>() != null)
                    {
                        //If the player is interacting with the cannon
                        if (currentInteractableItem.IsInteractionActive())
                        {
                            //Check to see if the player can fire the cannon
                            currentInteractableItem.GetComponent<CannonController>().CheckForCannonFire();
                        }
                    }

                    //If the interactable is an engine
                    if (currentInteractableItem.GetComponent<CoalController>() != null)
                    {
                        //If the player is interacting with the engine
                        if (currentInteractableItem.IsInteractionActive())
                        {
                            //Progress the engine fill animation
                            currentInteractableItem.GetComponent<CoalController>().ProgressCoalFill();
                        }
                    }
                }

                //If nothing else applies, the player has no item. Use the wrench, if possible
                else
                {
                    //CheckForWrenchUse();
                }
            }

            if (ctx.canceled)
            {
                if (taskInProgress)
                {
                    CancelProgressBar();
                }
            }
        }
    }

    private void CheckForHammerUse()
    {
        //If the player is outside of the tank (in a layer that does not exist inside the tank) and can afford a new layer
        if (IsPlayerOutsideTank() && LevelManager.instance.CanPlayerAfford("NewLayer"))
        {
            if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
            {
                if(LevelManager.instance.totalLayers < 2)
                {
                    if (itemHeld.GetTimeToUse() > 0)
                        StartProgressBar(itemHeld.GetTimeToUse(), LevelManager.instance.PurchaseLayer);
                }
            }
            else
            {
                if (itemHeld.GetTimeToUse() > 0)
                    StartProgressBar(itemHeld.GetTimeToUse(), LevelManager.instance.PurchaseLayer);
            }
        }
        else
        {
            //Try to buy an interactable
            foreach (var i in GameObject.FindGameObjectsWithTag("GhostObject"))
            {
                //If a player can purchase an interactable, try to purchase it
                if (i.GetComponent<ToggleInteractBuy>().PlayerCanPurchase())
                {
                    i.GetComponent<ToggleInteractBuy>().PurchaseInteractable();
                }
            }
        }
    }

    private void CheckForFireRemoverUse()
    {
        FireBehavior fire = playerTank.GetLayerAt(currentLayer - 1).GetComponentInChildren<FireBehavior>();

        //If the layer the player is on is on fire
        if (fire != null && fire.IsLayerOnFire())
        {
            if (itemHeld.GetTimeToUse() > 0)
                StartProgressBar(itemHeld.GetTimeToUse(), UseFireRemover);
        }
    }

    private void CheckForWrenchUse()
    {
        LayerHealthManager layerHealth = playerTank.GetLayerAt(currentLayer - 1);

        //If the layer the player is damaged
        if (layerHealth.GetLayerHealth() < layerHealth.GetMaxHealth())
        {
            if (timeToUseWrench > 0)
            {
                //Play sound effect
                FindObjectOfType<AudioManager>().PlayOneShot("UseWrench", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

                StartProgressBar(timeToUseWrench, UseWrench);
            }
        }
    }

    private void UseFireRemover()
    {
        FireBehavior fire = playerTank.GetLayerAt(currentLayer - 1).GetComponentInChildren<FireBehavior>();
        //Get rid of the fire
        fire.gameObject.SetActive(false);
    }

    private void UseWrench()
    {
        //Restore the layer the player is on to max health
        LayerHealthManager layerHealth = playerTank.GetLayerAt(currentLayer - 1);
        layerHealth.RepairLayer();
    }

    public void OnPrevInteractable(InputAction.CallbackContext ctx)
    {
        if (UIAllowPlayerInteract())
        {
            //If the player presses the previous interactable button
            if (ctx.performed)
            {
                if (PlayerHasItem("Hammer"))
                {
                    if (currentInteractableToBuy != null)
                    {
                        FindObjectOfType<InteractableSpawnerManager>().UpdateGhostInteractable(currentInteractableToBuy.transform.parent.GetComponent<InteractableSpawner>(), -1);
                    }
                }
            }
        }
    }

    public void OnNextInteractable(InputAction.CallbackContext ctx)
    {
        if (UIAllowPlayerInteract())
        {
            //If the player presses the next interactable button
            if (ctx.performed)
            {
                if (PlayerHasItem("Hammer"))
                {
                    if (currentInteractableToBuy != null)
                    {
                        FindObjectOfType<InteractableSpawnerManager>().UpdateGhostInteractable(currentInteractableToBuy.transform.parent.GetComponent<InteractableSpawner>(), 1);
                    }
                }
            }
        }
    }

    public void DisplayInteractionPrompt(string message)
    {
        interactableHover.transform.Find("Prompt").GetComponent<TextMeshProUGUI>().text = message;
        interactableHover.SetActive(true);
    }

    public void HideInteractionPrompt()
    {
        Debug.Log("Hide Interaction Prompt");
        interactableHover.SetActive(false);
    }

    public void StartProgressBar(float secondsTillCompletion, Action actionOnComplete)
    {
        if (!taskInProgress)
        {
            taskInProgress = true;
            progressBarCanvas.SetActive(true);
            progressBarSlider.value = 0;
            currentLoadAction = ProgressBarLoad(secondsTillCompletion, actionOnComplete);
            StartCoroutine(currentLoadAction);
            playerAnimator.SetBool("IsBuilding", true);
        }
    }

    IEnumerator ProgressBarLoad(float secondsTillCompletion, Action actionOnComplete)
    {
        float currentPercentage = 0;
        float completionRate = secondsTillCompletion / 100;

        //While the task is in progress and the task bar is not full, fill the task bar
        while(progressBarSlider.value < 100 && taskInProgress)
        {
            currentPercentage += (1 / completionRate) * Time.deltaTime;
            progressBarSlider.value = currentPercentage;
            yield return null;
        }

        actionOnComplete.Invoke();
        playerAnimator.SetBool("IsBuilding", false);
        HideProgressBar();
    }

    public void CancelProgressBar()
    {
        StopCoroutine(currentLoadAction);
        taskInProgress = false;
        playerAnimator.SetBool("IsBuilding", false);
        HideProgressBar();
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

    public void OnPause(InputAction.CallbackContext ctx)
    {
        //If the player presses the pause button
        if (ctx.started)
        {
            //Pause the game
            LevelManager.instance.PauseToggle(playerIndex);
        }
    }

    public bool IsPlayerOutsideTank()
    {
        return currentLayer > LevelManager.instance.totalLayers;
    }

    public bool IsPlayerClimbing()
    {
        return isClimbing;
    }

    public bool HasPlayerMoved() => hasMoved;

    public void SetPlayerClimb(bool climb)
    {
        canClimb = climb;
    }

    public void SetPlayerMove(bool movePlayer)
    {
        canMove = movePlayer;
    }

    public void MarkClosestItem(Item item)
    {
        closestItem = item;
    }

    public bool IsPlayerHoldingItem()
    {
        return isHoldingItem;
    }

    public Item GetPlayerItem()
    {
        return itemHeld;
    }

    public bool PlayerHasItem(string name)
    {
        if(itemHeld != null)
        {
            if (itemHeld.CompareTag(name))
                return true;
        }

        return false;
    }

    public void DestroyItem()
    {
        Destroy(itemHeld.gameObject);
        itemHeld = null;
        isHoldingItem = false;
    }

    public void OnPickup(InputAction.CallbackContext ctx)
    {
        if (UIAllowPlayerInteract())
        {
            if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
            {
                if((int)TutorialController.main.currentTutorialState >= (int)TUTORIALSTATE.PICKUPHAMMER)
                {
                    //If the player presses the pickup button
                    if (ctx.started)
                    {
                        PickupItem();
                    }
                }
            }
            else
            {
                //If the player presses the pickup button
                if (ctx.started)
                {
                    PickupItem();
                }
            }
        }
    }

    public void OnThrow(InputAction.CallbackContext ctx)
    {
        if (UIAllowPlayerInteract())
        {
            if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
            {
                if ((int)TutorialController.main.currentTutorialState >= (int)TUTORIALSTATE.PICKUPHAMMER)
                {
                    //If the player presses the throw button
                    if (ctx.started)
                    {
                        //If the player is still holding an item
                        if (itemHeld != null)
                        {
                            DropItem(true);
                        }
                    }
                }
            }
            else
            {
                //If the player presses the throw button
                if (ctx.started)
                {
                    //If the player is still holding an item
                    if (itemHeld != null)
                    {
                        DropItem(true);
                    }
                }
            }
        }
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
        itemHeld.GetComponent<Rigidbody2D>().isKinematic = true;
        itemHeld.transform.parent = gameObject.transform;
        itemHeld.SetRotateConstraint(true);

        Debug.Log("Picked Up Item!");

        if (PlayerHasItem("Hammer") && !IsPlayerOutsideTank())
        {
            LevelManager.instance.CheckInteractablesOnLayer(currentLayer);
        }
        else if (PlayerHasItem("Hammer") && IsPlayerOutsideTank())
        {
            LevelManager.instance.AddGhostLayer();
        }

        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if (TutorialController.main.currentTutorialState == TUTORIALSTATE.PICKUPHAMMER && PlayerHasItem("Hammer"))
            {
                //Tell tutorial that task is complete
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }

        //If the item can deal damage, remove that
        if (itemHeld.GetComponent<DamageObject>() != null)
        {
            Destroy(itemHeld.GetComponent<DamageObject>());
        }

        FindObjectOfType<AudioManager>().PlayAtRandomPitch("ItemPickup", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        itemHeld.SetPickUp(true);
        isHoldingItem = true;
        closestItem = null;
    }

    private void DropItem(bool throwItem)
    {
        //Remove the item from the player and put it back in the level
        itemHeld.GetComponent<Rigidbody2D>().gravityScale = itemHeld.GetDefaultGravityScale();
        itemHeld.GetComponent<Rigidbody2D>().isKinematic = false;
        itemHeld.transform.parent = null;
        itemHeld.SetRotateConstraint(false);

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
            LevelManager.instance.RemoveGhostLayer();
        }

        itemHeld.SetPickUp(false);
        isHoldingItem = false;

        //If the player is supposed to throw the item, add some force
        if (throwItem)
        {
            //If the item is a shell, make it do damage
            if(itemHeld.GetComponent<ShellItemBehavior>() != null)
                itemHeld.gameObject.AddComponent<DamageObject>().damage = itemHeld.GetComponent<ShellItemBehavior>().GetDamage();

            Vector2 throwVector = GetThrowVector(throwAngle * Mathf.Deg2Rad);

            //If the player is facing left, flip the x
            if (!isFacingRight)
                throwVector = new Vector2(-throwVector.x, throwVector.y);

            itemHeld.GetComponent<Rigidbody2D>().AddForce(throwVector * (throwForce * 100));
        }

        itemHeld = null;
    }

    private Vector2 GetThrowVector(float radians)
    {
        //Trigonometric function to get the vector (hypotenuse) of the current throw angle
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void CheckJoystickSpinning()
    {
        //If the current movement vector is different from the previous movement vector and spinning input is not being checked
        if(movement != lastJoystickInput && !isCheckingSpinInput)
        {
            //Check for spin input
            isCheckingSpinInput = true;
            StartCoroutine(JoystickSpinningDetection());
        }

        //If the number of spin checks is equal to number of spins that are needed, the joystick has been properly spun
        if(validSpinCheckCounter == validSpinCheckRows)
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
        lastJoystickInput = movement;

        //Wait for a bit to check for a spin angle
        yield return new WaitForSeconds(spinAngleCheckUpdateTimer);

        //If the angle between the last known movement vector and the current movement vector reaches a specified amount
        if(Vector2.Angle(lastJoystickInput, movement) >= spinValidAngleLimit)
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

    public void PlayFootstepSFX()
    {
        FindObjectOfType<AudioManager>().PlayAtRandomPitch("Footstep", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
    }

    public void PlayLadderClimbSFX()
    {
        FindObjectOfType<AudioManager>().PlayAtRandomPitch("LadderClimb", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
    }

    public void PlayHammerSFX()
    {
        FindObjectOfType<AudioManager>().PlayAtRandomPitch("TankImpact", PlayerPrefs.GetFloat("SFXVolume", 0.75f));
    }

    public bool UIAllowPlayerInteract()
    {
        return !LevelManager.instance.isPaused && !LevelManager.instance.readingTutorial;
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    public Color GetPlayerColor() => transform.Find("Outline").GetComponent<Renderer>().material.color;

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
}
