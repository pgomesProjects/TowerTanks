using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public enum TANKSPEED
{
    REVERSEFAST, REVERSE, STATIONARY, FORWARD, FORWARDFAST
}

public class PlayerTankController : MonoBehaviour
{
    public static float[] throttleSpeedOptions = { -1.5f, -1f, 0f, 1, 1.5f };   //The different options that the player tank's throttle has for speed

    public static float PLAYER_TANK_LAYER_HEIGHT = 8f;

    [SerializeField, Tooltip("The base speed for the player tank.")] private float speed = 4;
    [SerializeField, Tooltip("The change in speed based on the number of layers the player tank has when it has more than 2 layers.")] private float tankWeightMultiplier = 0.8f;
    [SerializeField, Tooltip("The change in speed based on the number of working engines the player tank has.")] private float tankEngineMultiplier = 1.5f;
    [SerializeField, Tooltip("The range for the player and items to walk around in.")] internal float tankBarrierRange = 12;
    [SerializeField, Tooltip("The distance that the tank must travel forward in order to spawn a new enemy.")] private float distanceUntilSpawn = 50;
    [SerializeField, Tooltip("The parent that holds all items.")] private Transform itemContainer;

    [Tooltip("The dust particles for the treads.")] public GameObject[] dustParticles;

    private bool steeringStickMoved;    //If true, the steering stick is currently moving. If false, the steering stick has not moved.
    private int currentThrottleOption;  //The current throttle option based on the position of the throttle

    private Animator treadAnimator; //The animator for the treads

    private List<LayerHealthManager> layers;    //A list of information on all player tank layers

    private float currentSpeed; //The current speed of the player tank
    private float currentTankWeightMultiplier;  //The current tank weight multiplier of the player tank
    private float currentEngineMultiplier;  //The current engine multiplier of the player tank

    private float throttleMultiplier;   //The speed multiplier based on the direction that the throttle is shifted to

    private float currentDistance;  //The current distane the player tank has moved in between waves

    private bool canMove;   //If true, the player tank can move. If false, the players have no control over the player tank's movement.

    private void Awake()
    {
        layers = new List<LayerHealthManager>(2);   //Start the list with room for two layers
        AdjustLayersInList();

        treadAnimator = GetComponentInChildren<Animator>();
        UpdateTreadParticles(); //Update the tread particles
    }

    private void Start()
    {
        //Start the tank with a default stats
        currentSpeed = speed;
        currentTankWeightMultiplier = 1;
        currentEngineMultiplier = 0;
        canMove = true;

        steeringStickMoved = false;
        currentThrottleOption = (int)TANKSPEED.STATIONARY;
        throttleMultiplier = throttleSpeedOptions[currentThrottleOption];
    }

    /// <summary>
    /// Adjusts the layer information list.
    /// </summary>
    public void AdjustLayersInList()
    {
        //Clear the list
        layers.Clear();

        //Insert each layer at the appropriate index
        foreach (var i in GetComponentsInChildren<LayerHealthManager>())
        {
            layers.Add(i);
        }

        //Sort the list by y position
        layers = layers.OrderBy(y => y.transform.position.y).ToList();

        PrintLayerList();
    }

    /// <summary>
    /// Prints a list of the player tank's layer numbers and names to the console.
    /// </summary>
    private void PrintLayerList()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            Debug.Log("Index " + i + ": " + layers[i].name);
        }
    }

    private void Update()
    {
        //Add distance to the player tank
        if(!SpawnDistanceReached())
        {
            currentDistance += GetPlayerSpeed() * Time.deltaTime;
        }

        //Check the tread animation speed
        treadAnimator.speed = GetBasePlayerSpeed() * Mathf.Abs(throttleMultiplier) * Time.deltaTime * 15f;
        treadAnimator.SetFloat("Direction", throttleMultiplier);

        //Move the tank
        MoveTank();
    }


    /// <summary>
    /// Moves the tank horizontally based on the base speed and the game speed
    /// </summary>
    private void MoveTank()
    {
        if (canMove)
        {
            Vector3 tankPosition = transform.position;
            tankPosition.x += GetPlayerSpeed() * Time.deltaTime;
            transform.position = tankPosition;
        }
    }

    /// <summary>
    /// Adjusts the tank's weight multiplier based on the number of layers the player tank has.
    /// </summary>
    /// <param name="numberOfLayers"></param>
    public void AdjustTankWeight(int numberOfLayers)
    {
        float newTankWeight = 1;

        //If the number of layers in the tank is 2 or less, there is no weight change
        if (numberOfLayers <= 2)
        {
            currentTankWeightMultiplier = 1;
            return;
        }

        //Add the multiplier to the tank weight for every additional layer the tank has gotten
        for(int i = 0; i < numberOfLayers - 2; i++)
        {
            newTankWeight *= tankWeightMultiplier; 
        }

        currentTankWeightMultiplier = newTankWeight;
    }

    /// <summary>
    /// Gets the number of working engines.
    /// </summary>
    /// <returns>The number of engines that exist and currently have coal in them.</returns>
    private int GetNumberOfWorkingEngines()
    {
        int counter = 0;

        //Get the number of existing engines in the player tank
        foreach(var i in FindObjectsOfType<CoalController>())
        {
            //If the tank has coal, register it as a working engine
            if(i.HasCoal())
                counter++;
        }

        return counter;
    }

    /// <summary>
    /// Updates the speed of the tank.
    /// </summary>
    /// <param name="speedUpdate">The new speed of the tank.</param>
    public void UpdateSpeed(int speedUpdate)
    {
        SetCurrentThrottleOption(speedUpdate);
        SetThrottleMultiplier(throttleSpeedOptions[speedUpdate]);

        UpdateTreadsSFX();
        UpdateTreadParticles();
    }

    /// <summary>
    /// Updates the speed of the tank.
    /// </summary>
    /// <param name="speed">The new speed of the tank.</param>
    public void UpdateSpeed(float speed)
    {
        SetThrottleMultiplier(speed);
    }

    /// <summary>
    /// Adjust the tank's speed based on the number of engines it has.
    /// </summary>
    public void AdjustEngineSpeedMultiplier()
    {
        float newEngineSpeed = 1;

        int numberOfEngines = GetNumberOfWorkingEngines();

        //Debug.Log("Working Engines: " + numberOfEngines);

        //Add to the tank multiplier based on the number of working engines
        for (int i = 1; i < numberOfEngines; i++)
        {
            newEngineSpeed *= tankEngineMultiplier;
        }

        //If there are no engines, the multiplier is 0 so that the tank does not move
        if (numberOfEngines == 0)
            newEngineSpeed = 0;

        currentEngineMultiplier = newEngineSpeed;
;
        //Update the sound effects
        UpdateEngineSFX(numberOfEngines);
        UpdateTreadsSFX();
    }

    /// <summary>
    /// Update the tank idle sound effect based on the number of active engines.
    /// </summary>
    /// <param name="numberOfEngines">The number of engines that are active.</param>
    private void UpdateEngineSFX(int numberOfEngines)
    {
        //If there is at least one engine running, play the engine sound effect
        if(numberOfEngines > 0)
        {
            if(!FindObjectOfType<AudioManager>().IsPlaying("TankIdle", gameObject))
                FindObjectOfType<AudioManager>().Play("TankIdle", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume), gameObject);
        }
        //If not, stop the sound effect if it's currently playing
        else
        {
            if (FindObjectOfType<AudioManager>().IsPlaying("TankIdle", gameObject))
                FindObjectOfType<AudioManager>().Stop("TankIdle", gameObject);
        }
    }

    /// <summary>
    /// Update the treads sound effect based on whether the player tank is moving or not.
    /// </summary>
    public void UpdateTreadsSFX()
    {
        //If the current speed is stationary
        if (GetPlayerSpeed() == 0)
        {
            FindObjectOfType<AudioManager>().Stop("TreadsRolling", gameObject);
        }
        //If the tank idle isn't already playing, play it
        else if (!FindObjectOfType<AudioManager>().IsPlaying("TreadsRolling", gameObject))
        {
            FindObjectOfType<AudioManager>().Play("TreadsRolling", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume), gameObject);
        }
    }

    /// <summary>
    /// Update the tread particles based on the direction the player tank is going.
    /// </summary>
    public void UpdateTreadParticles()
    {
        //If the player is going right, show tread particles on the left
        if(GetPlayerSpeed() > 0)
        {
            ShowLeftDustParticles(true);
            ShowRightDustParticles(false);
        }
        //If the player is going left, show tread particles on the right
        else if (GetPlayerSpeed() < 0)
        {
            ShowLeftDustParticles(false);
            ShowRightDustParticles(true);
        }
        //If the player is still, hide both tread particles
        else
        {
            ShowLeftDustParticles(false);
            ShowRightDustParticles(false);
        }
    }

    /// <summary>
    /// Collision behavior for when the player tank bounces off of the enemy tank.
    /// </summary>
    /// <param name="collideVelocity">The velocity of the collision force.</param>
    /// <param name="seconds">The number of seconds the event should take place.</param>
    /// <returns></returns>
    public IEnumerator CollideWithEnemyAni(float collideVelocity, float seconds)
    {
        //Stop the tank from moving manually
        canMove = false;
        float timeElapsed = 0;

        //Deal damage to bottom layer
        GetLayerAt(0).DealDamage(10, false);

        //Shake camera on collision
        CameraEventController.instance.ShakeCamera(10f, seconds);


        //Smoothly accelerate backwards
        while (timeElapsed < seconds && this != null)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            float newX = Mathf.Lerp(0, collideVelocity, t);
            transform.position -= new Vector3(newX * Time.deltaTime, 0, 0);

            timeElapsed += Time.deltaTime;

            yield return null;
        }

        timeElapsed = 0;

        //Smoothly decelerate forwards
        while (timeElapsed < seconds && this != null)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            float newX = Mathf.Lerp(collideVelocity, 0, t);
            transform.position -= new Vector3(newX * Time.deltaTime, 0, 0);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        //Let the tank move manually again
        canMove = true;
    }


    public List<LayerHealthManager> GetLayers()
    {
        return layers;
    }

    /// <summary>
    /// Gets the layer object based on the layer number.
    /// </summary>
    /// <param name="index">The layer number of the tank.</param>
    /// <returns></returns>
    public LayerHealthManager GetLayerAt(int index)
    {
        LayerHealthManager layer;

        try
        {
            layer = layers[index];
        }
        catch (Exception ex)
        {
            Debug.Log("Error: " + ex + " - Layer Could Not Be Found");
            layer = null;
        }

        return layer;
    }

    public Transform GetItemContainer() => itemContainer;

    public float GetCurrentTankDistance() => currentDistance;

    public bool SpawnDistanceReached() => currentDistance >= distanceUntilSpawn;

    /// <summary>
    /// Resets the tank's distance
    /// </summary>
    public void ResetTankDistance()
    {
        LevelManager.instance.StopCombatMusic();
        currentDistance = 0;
        LevelManager.instance.ShowGoPrompt();
    }

    public void ShowLeftDustParticles(bool showParticles) => dustParticles[0].SetActive(showParticles);
    public void ShowRightDustParticles(bool showParticles) => dustParticles[1].SetActive(showParticles);

    public float GetBasePlayerSpeed() => (currentSpeed* currentEngineMultiplier)* currentTankWeightMultiplier;
    public float GetPlayerSpeed() => GetBasePlayerSpeed() * throttleMultiplier;

    public float GetThrottleMultiplier() => throttleMultiplier;
    public void SetThrottleMultiplier(float multiplier) => throttleMultiplier = multiplier;
    public int GetCurrentThrottleOption() => currentThrottleOption;
    public void SetCurrentThrottleOption(int throttleOption) => currentThrottleOption = throttleOption;
    public bool IsSteeringMoved() => steeringStickMoved;
    public void SetSteeringMoved(bool steeringMoved) => steeringStickMoved = steeringMoved;

    private void OnDestroy()
    {
        //Stop playing tank sound effects when destroyed
        if(FindObjectOfType<AudioManager>() != null)
        {
            FindObjectOfType<AudioManager>().Stop("TankIdle");

            //If the treads rolling sound effect is playing, stop showing the dust particles on the treads
            if (FindObjectOfType<AudioManager>().IsPlaying("TreadsRolling"))
            {
                FindObjectOfType<AudioManager>().Stop("TreadsRolling");
                ShowLeftDustParticles(false);
                ShowRightDustParticles(false);
            }

            //Play explosion sound effect
            FindObjectOfType<AudioManager>().Play("LargeExplosionSFX", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume), gameObject);
        }
    }
}
