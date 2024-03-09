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
    public bool repairInputHeld;
    private float cooldown = 0;

    [SerializeField] private PlayerInput playerInputComponent;
    private int playerIndex;
    InputActionMap inputMap;
    private PlayerHUD playerHUD;
    private float vel;

    //Values
    private float coal = 0;
    public float maxCoal; //maximum coal allowed in Firebox
    private float currentCoalBurnValue = 0;
    public float coalBurnSpeed; //how fast coal burns
    public float coalBump; //bump to temp & pressure when adding coal

    private float temperature = 0;
    public float temperatureRiseSpeed; //how fast temperature rises due to coal
    public float lowTempThreshold; //threshold temp needs to be above for pressure to begin

    private float pressure = 0;
    public float pressureRiseSpeed; //how fast pressure rises due to temperature
    public float pressureReleaseSpeed; //how fast pressure drops when holding release valve
    public float dangerZoneThreshold; //threshold pressure needs to be above for overdrive
    private bool overdriveActive = false;

    //UI
    private float loadCounter = 0;
   
    private TextMeshProUGUI coalText;
    private Image coalProgressBar;
    private TextMeshProUGUI tempText;
    private Image tempBar;
    private TextMeshProUGUI pressureText;
    private Image pressureBar;
    private TextMeshProUGUI overdriveText;
    private TextMeshProUGUI highPressureText;
    private TextMeshProUGUI highTempText;
    private TextMeshProUGUI maxCoalText;

    public Color temperatureLowColor;
    public Color temperatureHighColor;

    // Start is called before the first frame update
    void Start()
    {
        if (playerInputComponent != null) LinkPlayerInput(playerInputComponent);
        coalText = GameObject.Find("CoalText").GetComponent<TextMeshProUGUI>();
        coalProgressBar = GameObject.Find("CoalProgress").GetComponent<Image>();
        tempText = GameObject.Find("TempText").GetComponent<TextMeshProUGUI>();
        tempBar = GameObject.Find("TempBar").GetComponent<Image>();
        pressureText = GameObject.Find("PressureText").GetComponent<TextMeshProUGUI>();
        pressureBar = GameObject.Find("PressureBar").GetComponent<Image>();
        overdriveText = GameObject.Find("OverdriveText").GetComponent<TextMeshProUGUI>();
        highPressureText = GameObject.Find("HighPressureText").GetComponent<TextMeshProUGUI>();
        highTempText = GameObject.Find("HighTempText").GetComponent<TextMeshProUGUI>();
        maxCoalText = GameObject.Find("MaxCoalText").GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (coal > 0) BurnCoal();
        UpdateTemperature();
        UpdatePressure();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coalText != null) coalText.text = "Coal: " + coal;
        if (coalProgressBar != null)
        {
            coalProgressBar.rectTransform.localScale = new Vector3(1, (currentCoalBurnValue / 10));
        }

        if (tempText != null) tempText.text = "Temperature: " + Mathf.Round(temperature);
        if (tempBar != null)
        {
            tempBar.rectTransform.localScale = new Vector3(1, (temperature / 100f));
        }

        if (pressureBar != null)
        {
            pressureBar.rectTransform.localScale = new Vector3(1, (pressure / 100f));
        }

        if (overdriveActive)
        {
            overdriveText.enabled = true;
        }
        else overdriveText.enabled = false;

        if (pressure >= dangerZoneThreshold)
        {
            highPressureText.enabled = true;
        }
        else highPressureText.enabled = false;

        if (temperature >= 80)
        {
            highTempText.enabled = true;
        }
        else highTempText.enabled = false;

        if (coal >= maxCoal)
        {
            maxCoalText.enabled = true;
        }
        else maxCoalText.enabled = false;
    }

    private void BurnCoal() //Depletes coal over time based on coalBurnSpeed
    {
        currentCoalBurnValue += 1f * coalBurnSpeed * Time.deltaTime;
        if (currentCoalBurnValue >= 10f)
        {
            coal -= 1;
            currentCoalBurnValue = 0;
        }
    }

    private void UpdateTemperature() //Increases Temperature over time while coal burning
    {
        float riseSpeed = 1f * temperatureRiseSpeed * Time.deltaTime;
        float lowerSpeed = -pressureReleaseSpeed * Time.deltaTime * 0.4f;
        float heatDif = (100f - (temperature * 0.5f)) / 100f;
        
        if (coal > 0)
        {
            riseSpeed = riseSpeed * heatDif;
            temperature += riseSpeed;
            if (temperature > 100f) temperature = 100f;
        }
        else if (temperature > 0)
        {
            temperature -= riseSpeed * 5f;
            if (temperature < 0) temperature = 0;
        }

        if (repairInputHeld && temperature > 0)
        {
            temperature += lowerSpeed;
            if (temperature < 0) temperature = 0;
        }

            tempBar.color = Color.Lerp(temperatureLowColor, temperatureHighColor, temperature / 100f);

        /*
        if (temperature > 0)
        {
            if (!GameManager.Instance.AudioManager.IsPlaying("FireBurningSFX"))
            {
                GameManager.Instance.AudioManager.Play("FireBurningSFX");
            }
        }
        else
        {
            if (GameManager.Instance.AudioManager.IsPlaying("FireBurningSFX")) GameManager.Instance.AudioManager.Stop("FireBurningSFX");
        }*/
    }

    private void UpdatePressure()
    {
        float riseSpeed = 1f * pressureRiseSpeed * Time.deltaTime;
        float lowerSpeed = -pressureReleaseSpeed * Time.deltaTime;
        float pressureDif = (100f - (pressure * 0.9f)) / 100f;

        if (temperature > lowTempThreshold)
        {
            riseSpeed = riseSpeed * (temperature * 15f) * pressureDif;
            pressure += riseSpeed;
            if (pressure > 100f) pressure = 100f;
        }
        else if (pressure > 0)
        {
            pressure += lowerSpeed;
            if (pressure < 0) pressure = 0;
        }

        if (repairInputHeld && pressure > 0)
        {
            pressure += lowerSpeed;
            if (pressure < 0) pressure = 0;

            if (pressure >= dangerZoneThreshold)
            {
                overdriveActive = true;
            }

            if (!GameManager.Instance.AudioManager.IsPlaying("JetpackRocket"))
            {
                GameManager.Instance.AudioManager.Play("JetpackRocket");
            }
        }
        else if (GameManager.Instance.AudioManager.IsPlaying("JetpackRocket"))
        {
            GameManager.Instance.AudioManager.Stop("JetpackRocket");
            overdriveActive = false;
        }

        if (pressure > 0)
        {
            if (!GameManager.Instance.AudioManager.IsPlaying("TankIdle"))
            {
                GameManager.Instance.AudioManager.Play("TankIdle");
            }
        }
        else
        {
            if (GameManager.Instance.AudioManager.IsPlaying("TankIdle")) GameManager.Instance.AudioManager.Stop("TankIdle");
            //GameManager.Instance.AudioManager.Play("EngineDyingSFX");
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
            case "Look": OnLook(ctx); break;
            case "Interact": OnInteract(ctx); break;
            case "Cancel": OnCancel(ctx); break;
            case "Cycle Interactables": OnCycle(ctx); break;
            case "Jetpack": OnJetpack(ctx); break;
            case "Repair": OnRepair(ctx); break;
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

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            coal += 1; //Adds 1 coal
            GameManager.Instance.AudioManager.Play("ThrottleClick");

            if (coal > maxCoal)
            {
                coal = maxCoal;
                GameManager.Instance.AudioManager.Play("InvalidAlert");
            }
            else
            {
                temperature += coalBump;
                //pressure += coalBump;

                if (temperature > 100) temperature = 100;
                if (pressure > 100) pressure = 100;
            }


            /*
            loadCounter += 1;
            if (loadCounter == 2) { };
            if (loadCounter == 4)
            {
                loadCounter = 0;
            }*/
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
        repairInputHeld = ctx.ReadValue<float>() > 0;
        if (ctx.ReadValue<float>() > 0 && pressure > 0) GameManager.Instance.AudioManager.Play("JetpackStartup");
    }

    public void OnPause(InputAction.CallbackContext ctx) //Reset Tank
    {
        
    }

    #endregion
}
