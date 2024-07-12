using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class Test_Cannon : MonoBehaviour
{
    //Input
    private Vector2 moveInput;
    public bool repairInputHeld;
    private float cooldown = 0;

    [SerializeField] private PlayerInput playerInputComponent;
    private int playerIndex;
    InputActionMap inputMap;
    private PlayerHUD playerHUD;
    private float vel;

    private float rotationSpeed = 0;

    [Header("Joystick Spin Detection Options")]
    [SerializeField] private float spinAngleCheckUpdateTimer = 0.1f;
    [SerializeField] [Range(0.0f, 180.0f)] private float spinValidAngleLimit = 30.0f;
    [SerializeField] private int validSpinCheckRows = 1;
    [SerializeField] private float cannonScrollSensitivity = 3f;
    private bool isSpinningCannon = false;
    public float spinningDirection = 1; //1 = Clockwise, -1 = CounterClockwise

    //Joystick spin detection
    private Vector2 lastJoystickInput = Vector2.zero;
    private bool isCheckingSpinInput = false;
    private int validSpinCheckCounter = 0;
    private float cannonScroll;

    private Vector3 currentRotation; //curent cannon rotation

    //UI
    private TextMeshProUGUI aimText;
    private TextMeshProUGUI rotateText;
    private TextMeshProUGUI directionText;
    private Image stick;
    private Image circle;

    public Transform barrel;
    public float rotateSpeed = 10f;

    // Start is called before the first frame update
    void Start()
    {
        if (playerInputComponent != null) LinkPlayerInput(playerInputComponent);
        aimText = GameObject.Find("CannonText").GetComponent<TextMeshProUGUI>();
        rotateText = GameObject.Find("RotateText").GetComponent<TextMeshProUGUI>();
        directionText = GameObject.Find("DirectionText").GetComponent<TextMeshProUGUI>();
        stick = GameObject.Find("Stick").GetComponent<Image>();
        circle = GameObject.Find("Circle_1").GetComponent<Image>();

        currentRotation = new Vector3(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        aimText.text = "( " + (Mathf.Round(moveInput.x * 100f) / 100f) + " , " + (Mathf.Round(moveInput.y * 100f) / 100f) + " )";
        rotateText.text = "" + Mathf.Round(Vector2.Angle(lastJoystickInput, moveInput));
        RotateStick();
        CheckJoystickSpinning();

        RotateBarrel();
    }

    private void RotateStick()
    {

        Vector3 moveVector = (Vector3.up * moveInput.y - Vector3.left * moveInput.x);
        if (moveInput.x != 0 || moveInput.y != 0)
        {
            stick.transform.rotation = Quaternion.LookRotation(Vector3.forward, moveVector);
        }
        
        var posX = moveInput.x * 150f;
        var posY = moveInput.y * 150f;
        circle.rectTransform.localPosition = new Vector3(posX, posY, 0f);

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
        lastJoystickInput = moveInput;

        //Wait for a bit to check for a spin angle
        yield return new WaitForSeconds(spinAngleCheckUpdateTimer);

        //If the angle between the last known movement vector and the current movement vector reaches a specified amount
        if (Vector2.Angle(lastJoystickInput, moveInput) >= spinValidAngleLimit)
        {

            var spinAngle = Vector2.SignedAngle(lastJoystickInput, moveInput);
            if (spinAngle > 0) spinningDirection = -1;
            else if (spinAngle < 0) spinningDirection = 1;

            directionText.text = "" + spinningDirection;

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

    public void RotateBarrel()
    {
        float speed = rotateSpeed * Time.deltaTime;

        if (isSpinningCannon)
        {
            currentRotation += new Vector3(0, 0, (Vector2.SignedAngle(lastJoystickInput, moveInput) / 100) * speed);

            if (currentRotation.z > 30) currentRotation = new Vector3(0, 0, 30);
            if (currentRotation.z < -30) currentRotation = new Vector3(0, 0, -30);

            barrel.localEulerAngles = currentRotation;
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
            case "Interact": OnInteract(ctx); break;
            case "Cancel": OnCancel(ctx); break;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        //float moveSensitivity = 0.2f;

    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
        //float moveSensitivity = 0.2f;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            
        }
    }

    public void OnCancel(InputAction.CallbackContext ctx) //Rotate the Room 90 deg
    {

    }

    public void OnCycle(InputAction.CallbackContext ctx) //Cycle to the next Room in the List
    {

    }

    public void OnJetpack(InputAction.CallbackContext ctx)
    {

    }

    public void OnRepair(InputAction.CallbackContext ctx) //Release Valve
    {
        
    }

    public void OnPause(InputAction.CallbackContext ctx) //Reset Tank
    {

    }

    #endregion
}
