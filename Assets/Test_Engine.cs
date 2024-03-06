using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class Test_Engine : MonoBehaviour
{
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

    //UI
    private TextMeshPro coalText; 

    // Start is called before the first frame update
    void Start()
    {
        if (playerInputComponent != null) LinkPlayerInput(playerInputComponent);
    }

    // Update is called once per frame
    void Update()
    {
        
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
            case "Jump": OnJump(ctx); break;
            case "Repair": OnDeploy(ctx); break;
            case "Pause": OnPause(ctx); break;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        float moveSensitivity = 0.2f;

    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        float moveSensitivity = 0.2f;
    }

    public void OnRotate(InputAction.CallbackContext ctx) //Rotate the Room 90 deg
    {

    }

    public void OnCycle(InputAction.CallbackContext ctx) //Cycle to the next Room in the List
    {

    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
       
    }

    public void OnDeploy(InputAction.CallbackContext ctx) //Deploy the Tank
    {
        
    }

    public void OnPause(InputAction.CallbackContext ctx) //Reset Tank
    {
        
    }

    #endregion
}
