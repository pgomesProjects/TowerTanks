using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Debug_TankBuilder : MonoBehaviour
{
    //Assets
    public Rigidbody2D tank;
    public Room room;
    public GameObject[] roomList;
    public int roomSelected;

    //Input
    private Vector2 moveInput;
    private bool jetpackInputHeld;
    private float cooldown = 0;

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
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cooldown > 0)
        {
            cooldown -= Time.deltaTime;
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
                Debug.Log("Room " + roomList[roomToSpawn].name + " Spawned At " + transform.ToString() + " With The Parent " + _transform.name);
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
        playerHUD.InitializeHUD(GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerIndex]);
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
            case "Cancel": OnRotate(ctx); break;
            case "Cycle Interactables": OnCycle(ctx); break;
            case "Jump": OnJump(ctx); break;
            case "Repair": OnDeploy(ctx); break;

        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        float moveSensitivity = 0.2f;

        if (room != null)
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

    public void OnRotate(InputAction.CallbackContext ctx)
    {

        if (room != null)
        {
            if (ctx.performed)
            {
                room.debugRotate = true;
            }
        }
    }

    public void OnCycle(InputAction.CallbackContext ctx)
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
            GameManager.Instance.AudioManager.Play("UseSFX");
        }
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (cooldown <= 0 && ctx.performed)
        {
            if (room != null)
            {
                room.debugMount = true;
                room = null;
            }
            else
            {
                SpawnRoom(random: true);
                GameManager.Instance.AudioManager.Play("UseSFX");
            }
            cooldown = 0.1f;
        }
    }

    public void OnDeploy(InputAction.CallbackContext ctx)
    {
        tank.isKinematic = false;
    }

    #endregion
}
