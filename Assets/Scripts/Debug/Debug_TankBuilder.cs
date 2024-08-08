using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Debug_TankBuilder : MonoBehaviour
{
    //Assets
    public TankController[] tanks;
    public Room room;
    private TankInteractable interactable;
    public GameObject[] roomList;
    public RoomData roomData;
    public int roomSelected;
    public int interactableSelected;

    //Settings
    public enum BuildMode { ROOM, INTERACTABLE };
    public BuildMode currentMode;
    private float moveTimer; //how long you have to hold down the move input before the selector moves faster
    public float moveCooldown;
    public float moveCooldownTimer;
    public bool enableSounds;

    //Input
    public Vector2 moveInput;
    private bool jetpackInputHeld;
    private float cooldown = 0;
    private bool isDeployed = false;

    [SerializeField] private PlayerInput playerInputComponent;
    private int playerIndex;
    InputActionMap inputMap;
    private PlayerHUD playerHUD;
    private float vel;

    private PlayerControlSystem playerControlSystem;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
        //playerControlSystem.Player.Cancel.performed += _ => OnRotate(_);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (playerInputComponent != null) LinkPlayerInput(playerInputComponent);

        //Find all tanks we're debugging & freeze them
        tanks = FindObjectsOfType<TankController>();
        if (tanks.Length > 0)
        {
            foreach (TankController tank in tanks)
            {
                var rb = tank.treadSystem.GetComponent<Rigidbody2D>();
                rb.isKinematic = true;
            }
        }
    }

    private void OnEnable()
    {
        playerControlSystem?.Enable();
    }

    private void OnDisable()
    {
        playerControlSystem?.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
        }

        if (moveCooldownTimer > 0)
        {
            moveCooldownTimer -= Time.deltaTime;
        }

        MoveSelection();
        
        if (isDeployed && GameManager.Instance.AudioManager.IsPlaying("TankIdle") && !enableSounds)
        {
            GameManager.Instance.AudioManager.Stop("TankIdle");
        }

        if (isDeployed && !GameManager.Instance.AudioManager.IsPlaying("TankIdle") && enableSounds)
        {
            GameManager.Instance.AudioManager.Play("TankIdle");
        }
    }

    private void MoveSelection()
    {
        float moveSensitivity = 0.4f;

        if (room != null && !isDeployed) //Move the Room around
        {
            if (Mathf.Abs(moveInput.x) > moveSensitivity || Mathf.Abs(moveInput.y) > moveSensitivity)
            {
                moveTimer += Time.deltaTime;
            }
            else { moveTimer = 0; }

            if (moveTimer >= 0.8f)
            {
                if (Mathf.Abs(moveInput.x) > moveSensitivity && moveCooldownTimer <= 0)
                {

                    if (moveInput.x > moveSensitivity)
                    {
                        room.debugMoveRight = true;

                    }

                    if (moveInput.x < -moveSensitivity)
                    {
                        room.debugMoveLeft = true;

                    }

                    moveCooldownTimer = moveCooldown;
                }

                if (Mathf.Abs(moveInput.y) > moveSensitivity && moveCooldownTimer <= 0)
                {
                    if (moveInput.y > moveSensitivity)
                    {
                        room.debugMoveUp = true;

                    }

                    if (moveInput.y < -moveSensitivity)
                    {
                        room.debugMoveDown = true;

                    }

                    moveCooldownTimer = moveCooldown;
                }
            }
        }

        if (interactable != null && !isDeployed) //Move the Interactable around
        {
            if (Mathf.Abs(moveInput.x) > moveSensitivity || Mathf.Abs(moveInput.y) > moveSensitivity)
            {
                moveTimer += Time.deltaTime;
            }
            else { moveTimer = 0; }

            if (moveTimer >= 0.8f)
            {
                if (Mathf.Abs(moveInput.x) > moveSensitivity && moveCooldownTimer <= 0)
                {

                    if (moveInput.x > moveSensitivity)
                    {
                        interactable.debugMoveRight = true;

                    }

                    if (moveInput.x < -moveSensitivity)
                    {
                        interactable.debugMoveLeft = true;

                    }

                    moveCooldownTimer = moveCooldown;
                }

                if (Mathf.Abs(moveInput.y) > moveSensitivity && moveCooldownTimer <= 0)
                {
                    if (moveInput.y > moveSensitivity)
                    {
                        interactable.debugMoveUp = true;

                    }

                    if (moveInput.y < -moveSensitivity)
                    {
                        interactable.debugMoveDown = true;

                    }

                    moveCooldownTimer = moveCooldown;
                }
            }
        }
    }

    private void SpawnSelection(bool random, int thingToSpawn = 0)
    {
        if (cooldown <= 0)
        {
            Transform _transform = this.transform;

            if (currentMode == BuildMode.ROOM) //Spawn a new Room
            {
                if (interactable != null)
                {
                    Destroy(interactable.gameObject);
                    interactable = null;
                }

                var _room = roomList[0];
                var temp = room;

                if (random)
                {
                    int roomNo = Random.Range(0, roomList.Length);
                    roomSelected = roomNo;
                    thingToSpawn = roomNo;
                    _room = Instantiate(roomList[thingToSpawn], _transform);
                }
                else
                {
                    if (temp != null) _transform = temp.transform;
                    _room = Instantiate(roomList[thingToSpawn], transform);
                    if (temp != null)
                    {
                        _room.transform.position = _transform.position;
                        Destroy(temp.gameObject);
                    }
                }

                room = _room.GetComponent<Room>();
                cooldown = 0.1f;
            }

            if (currentMode == BuildMode.INTERACTABLE) //Spawn a new Interactable
            {
                if (room != null)
                {
                    Destroy(room.gameObject);
                    room = null;
                }

                var _interactable = roomData.interactableList[0];
                var temp = interactable;

                if (random)
                {
                    int intNo = Random.Range(0, roomData.interactableList.Length);
                    interactableSelected = intNo;
                    thingToSpawn = intNo;
                    _interactable = Instantiate(roomData.interactableList[thingToSpawn], _transform);
                }
                else
                {
                    if (temp != null) _transform = temp.transform;
                    _interactable = Instantiate(roomData.interactableList[thingToSpawn], transform);
                    if (temp != null)
                    {
                        _interactable.transform.position = _transform.position;
                        Destroy(temp.gameObject);
                    }
                }

                interactable = _interactable.GetComponent<TankInteractable>();
                cooldown = 0.1f;
            }
        }
    }

    #region Input
    public void LinkPlayerInput(PlayerInput newInput)
    {
        playerInputComponent = newInput;

        //Gets the player input action map so that events can be subscribed to it
        inputMap = playerInputComponent.actions.FindActionMap("Debug");
        inputMap.actionTriggered += OnPlayerInput;
    }

    private void OnPlayerInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "1": OnBuild(ctx); break;
            case "Move": OnMove(ctx); break;
            case "Look": OnLook(ctx); break;
            case "4": OnRotate(ctx); break;
            case "Cycle": OnCycle(ctx); break;
            case "Cancel": OnDeploy(ctx); break;
            case "Flip": OnFlip(ctx); break;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        float moveSensitivity = 0.8f;

        if (ctx.performed && moveTimer < 0.8f && moveCooldownTimer <= 0 && room != null)
        {
            if (moveInput.x > moveSensitivity)
            {
                room.debugMoveRight = true;

            }

            if (moveInput.x < -moveSensitivity)
            {
                room.debugMoveLeft = true;

            }

            if (moveInput.y > moveSensitivity)
            {
                room.debugMoveUp = true;

            }

            if (moveInput.y < -moveSensitivity)
            {
                room.debugMoveDown = true;

            }

            moveCooldownTimer = moveCooldown;
        }

        if (ctx.performed && moveTimer < 0.8f && moveCooldownTimer <= 0 && interactable != null)
        {
            if (moveInput.x > moveSensitivity)
            {
                interactable.debugMoveRight = true;

            }

            if (moveInput.x < -moveSensitivity)
            {
                interactable.debugMoveLeft = true;

            }

            if (moveInput.y > moveSensitivity)
            {
                interactable.debugMoveUp = true;

            }

            if (moveInput.y < -moveSensitivity)
            {
                interactable.debugMoveDown = true;

            }

            moveCooldownTimer = moveCooldown;
        }

    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();

        if (isDeployed) //Move the Tank
        {
            //treads.debugDrive = moveInput.x;
            if (enableSounds)
            {
                if (Mathf.Abs(moveInput.x) > 0.1f)
                {
                    if (!GameManager.Instance.AudioManager.IsPlaying("TreadsRolling"))
                    {
                        GameManager.Instance.AudioManager.Play("TreadsRolling");
                    }
                }
                else GameManager.Instance.AudioManager.Stop("TreadsRolling");
            }
        }
    }

    public void OnRotate(InputAction.CallbackContext ctx) //Rotate the Room 90 deg
    {
        if (room != null)
        {
            if (ctx.performed)
            {
                if (enableSounds) GameManager.Instance.AudioManager.Play("RotateRoom");
                room.debugRotate = true;
            }
        }

        if (ctx.started && interactable != null)
        {
            if (enableSounds) GameManager.Instance.AudioManager.Play("RotateRoom");
            interactable.Flip();
        }
    }

    public void OnCycle(InputAction.CallbackContext ctx) //Cycle to the next Room in the List
    {
        var input = ctx.ReadValue<Vector2>();

        if (room != null && ctx.started)
        {
            roomSelected += Mathf.RoundToInt(input.x);
            if (roomSelected >= roomList.Length) roomSelected = 0;
           
            if (roomSelected < 0) roomSelected = roomList.Length - 1;
            
            SpawnSelection(random: false, thingToSpawn: roomSelected);
            if (enableSounds) GameManager.Instance.AudioManager.Play("UseSFX");
        }

        if (interactable != null && ctx.started)
        {
            interactableSelected += Mathf.RoundToInt(input.x);
            if (interactableSelected >= roomData.interactableList.Length) interactableSelected = 0;

            if (interactableSelected < 0) interactableSelected = roomData.interactableList.Length - 1;

            SpawnSelection(random: false, thingToSpawn: interactableSelected);
            if (enableSounds) GameManager.Instance.AudioManager.Play("UseSFX");
        }
    }

    public void OnBuild(InputAction.CallbackContext ctx) 
    {
        if (cooldown <= 0 && ctx.performed && !isDeployed)
        {
            if (currentMode == BuildMode.ROOM)
            {
                if (room != null) //Try to Mount the Current Room
                {
                    if (enableSounds) GameManager.Instance.AudioManager.Play("ConnectRoom");
                    room.Mount();
                    Vector3 roomPos = room.transform.position;

                    room = null;
                    this.transform.position = roomPos;
                }
                else //Spawn a new Random Room
                {
                    SpawnSelection(random: true);
                    if (enableSounds) GameManager.Instance.AudioManager.Play("UseSFX");
                }
                cooldown = 0.1f;
            }

            if (currentMode == BuildMode.INTERACTABLE)
            {
                if (interactable != null) //Try to Mount the Current Interactable
                {
                    if (enableSounds) GameManager.Instance.AudioManager.Play("ConnectRoom");
                    interactable.DebugPlace();
                    Vector3 intPos = interactable.transform.position;

                    interactable = null;
                    this.transform.position = intPos;
                }
                else //Spawn a new Interactable
                {
                    SpawnSelection(random: false, thingToSpawn: interactableSelected);
                    if (enableSounds) GameManager.Instance.AudioManager.Play("UseSFX");
                }
                cooldown = 0.1f;
            }
        }
    }

    public void OnDeploy(InputAction.CallbackContext ctx) //Deploy the Tanks
    {
        if (tanks.Length > 0)
        {
            foreach (TankController tank in tanks)
            {
                var rb = tank.treadSystem.GetComponent<Rigidbody2D>();
                rb.isKinematic = false;
            }
        }
        isDeployed = true;
        if (room != null) Destroy(room.gameObject);
        if (interactable != null) Destroy(interactable.gameObject);
    }

    public void OnFlip(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (enableSounds) GameManager.Instance.AudioManager.Play("UseSFX");

            if (currentMode == BuildMode.ROOM)
            {
                currentMode = BuildMode.INTERACTABLE;
                SpawnSelection(false, interactableSelected);
            }
            else
            {
                currentMode = BuildMode.ROOM;
                SpawnSelection(false, roomSelected);
            }
        }
        
    }

    public void OnPause(InputAction.CallbackContext ctx) //Reset Tank
    {
        //ResetTank();
    }

    #endregion
}
