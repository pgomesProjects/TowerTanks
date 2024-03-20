using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Debug_TankBuilder : MonoBehaviour
{
    //Assets
    public Rigidbody2D tank;
    private TreadSystem treads;
    public Room room;
    public GameObject[] roomList;
    public int roomSelected;
    public bool enableSounds;
    private Transform resetPoint;

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

    // Start is called before the first frame update
    void Start()
    {
        if (playerInputComponent != null) LinkPlayerInput(playerInputComponent);
        tank = GameObject.Find("TreadSystem").GetComponent<Rigidbody2D>();
        if (tank != null)
        {
            tank.isKinematic = true; //Freeze the tank while we're building
            treads = tank.gameObject.GetComponent<TreadSystem>();
            resetPoint = treads.transform;
        }
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

    private void ResetTank() //Resets tank position to first transform
    {
        treads.transform.position = resetPoint.position;
        treads.transform.rotation = resetPoint.rotation;
        tank.isKinematic = true; //Freeze the tank while we're building
        if (isDeployed) isDeployed = false;
    }

    #region Input
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

    private void OnPlayerInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "Move": OnMove(ctx); break;
            case "Look": OnLook(ctx); break;
            case "Cancel": OnRotate(ctx); break;
            case "Cycle Interactables": OnCycle(ctx); break;
            case "Build": OnBuild(ctx); break;
            case "Repair": OnDeploy(ctx); break;
            case "Pause": OnPause(ctx); break;
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
            treads.debugDrive = moveInput.x;
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

        if (room != null && ctx.performed)
        {
            if (ctx.ReadValue<float>() > 0)
            {
                roomSelected += 1;
                if (roomSelected >= roomList.Length) roomSelected = 0;
            }
            else
            {
                roomSelected -= 1;
                if (roomSelected < 0) roomSelected = roomList.Length - 1;
            }
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

    public void OnDeploy(InputAction.CallbackContext ctx) //Deploy the Tank
    {
        tank.isKinematic = false;
        if (!isDeployed && enableSounds) GameManager.Instance.AudioManager.Play("TankIdle");
        isDeployed = true;
    }

    public void OnPause(InputAction.CallbackContext ctx) //Reset Tank
    {
        //ResetTank();
    }

    #endregion
}
