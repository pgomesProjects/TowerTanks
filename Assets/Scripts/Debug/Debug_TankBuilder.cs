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
    public GameObject[] roomList;
    public int roomSelected;
    public bool enableSounds;

    //Input
    private Vector2 moveInput;
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
        
        if (isDeployed && GameManager.Instance.AudioManager.IsPlaying("TankIdle") && !enableSounds)
        {
            GameManager.Instance.AudioManager.Stop("TankIdle");
        }

        if (isDeployed && !GameManager.Instance.AudioManager.IsPlaying("TankIdle") && enableSounds)
        {
            GameManager.Instance.AudioManager.Play("TankIdle");
        }
    }

    private void SpawnRoom(bool random, int roomToSpawn = 0)
    {
        if (cooldown <= 0)
        {
            Transform _transform = this.transform;
            var _room = roomList[0];
            var temp = room;

            if (random)
            {
                int roomNo = Random.Range(0, roomList.Length);
                roomSelected = roomNo;
                roomToSpawn = roomNo;
                _room = Instantiate(roomList[roomToSpawn], _transform);
            }
            else
            {
                _transform = temp.transform;
                _room = Instantiate(roomList[roomToSpawn], transform);
                _room.transform.position = _transform.position;
                Destroy(temp.gameObject);
            }

            room = _room.GetComponent<Room>();
            cooldown = 0.1f;
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
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        float moveSensitivity = 0.2f;

        if (room != null && !isDeployed) //Move the Room around
        {
            if (ctx.performed && moveInput.y > moveSensitivity)
            {
                room.debugMoveUp = true;
                
            }

            if (ctx.performed && moveInput.y < -moveSensitivity)
            {
                room.debugMoveDown = true;
                
            }

            if (ctx.performed && moveInput.x > moveSensitivity)
            {
                room.debugMoveRight = true;
                
            }

            if (ctx.performed && moveInput.x < -moveSensitivity)
            {
                room.debugMoveLeft = true;

            }
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
    }

    public void OnCycle(InputAction.CallbackContext ctx) //Cycle to the next Room in the List
    {
        var input = ctx.ReadValue<Vector2>();

        if (room != null && ctx.started)
        {
            roomSelected += Mathf.RoundToInt(input.x);
            if (roomSelected >= roomList.Length) roomSelected = 0;
           
            if (roomSelected < 0) roomSelected = roomList.Length - 1;
            
            SpawnRoom(random: false, roomToSpawn: roomSelected);
            if (enableSounds) GameManager.Instance.AudioManager.Play("UseSFX");
        }
    }

    public void OnBuild(InputAction.CallbackContext ctx) 
    {
        if (cooldown <= 0 && ctx.performed && !isDeployed)
        {
            if (room != null) //Try to Mount the Current Room
            {
                if (enableSounds) GameManager.Instance.AudioManager.Play("ConnectRoom");
                room.debugMount = true;
                room = null;
            }
            else //Spawn a new Random Room
            {
                SpawnRoom(random: true);
                if (enableSounds) GameManager.Instance.AudioManager.Play("UseSFX");
            }
            cooldown = 0.1f;
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
    }

    public void OnPause(InputAction.CallbackContext ctx) //Reset Tank
    {
        //ResetTank();
    }

    #endregion
}
