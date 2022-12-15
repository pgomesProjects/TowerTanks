using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
    [SerializeField] private float speed = 4;
    private float currentSpeed;
    public float displaySpeed = 0;
    [SerializeField] private float tankWeightMultiplier = 0.8f;
    [SerializeField] private float tankEngineMultiplier = 1.5f;
    private float currentTankWeightMultiplier;
    private float currentEngineMultiplier;
    [SerializeField] internal float tankBarrierRange = 12;
    [SerializeField] private float distanceUntilSpawn = 50;
    private float currentDistance;
    [SerializeField] private Transform itemContainer;

    private Animator treadAnimator;

    private List<LayerHealthManager> layers;

    private void Awake()
    {
        layers = new List<LayerHealthManager>(2);
        AdjustLayersInList();
        treadAnimator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        currentSpeed = speed;
        currentTankWeightMultiplier = 1;
        currentEngineMultiplier = 0;
    }

    public void AdjustLayersInList()
    {
        //Clear the list
        layers.Clear();

        //Insert each layer at the appropriate index
        foreach (var i in GetComponentsInChildren<LayerHealthManager>())
        {
            layers.Add(i);
        }

        //Sort by y position
        layers = layers.OrderBy(y => y.transform.position.y).ToList();

        PrintLayerList();
    }

    private void PrintLayerList()
    {

        for (int i = 0; i < layers.Count; i++)
        {
            Debug.Log("Index " + i + ": " + layers[i].name);
        }
    }

    public float GetPlayerSpeed()
    {
        return (currentSpeed * currentEngineMultiplier) * currentTankWeightMultiplier;
    }

    private void Update()
    {
        if(!SpawnDistanceReached())
        {
            currentDistance += GetPlayerSpeed() * LevelManager.instance.gameSpeed * Time.deltaTime;
        }

        treadAnimator.speed = GetPlayerSpeed() * Mathf.Abs(LevelManager.instance.gameSpeed) * Time.deltaTime * 15f;
        treadAnimator.SetFloat("Direction", LevelManager.instance.gameSpeed);
        displaySpeed = GetPlayerSpeed();
    }

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

    private int GetNumberOfWorkingEngines()
    {
        int counter = 0;

        foreach(var i in FindObjectsOfType<CoalController>())
        {
            if(i.HasCoal())
                counter++;
        }

        return counter;
    }

    public void AdjustEngineSpeedMultiplier()
    {
        float newEngineSpeed = 1;

        int numberOfEngines = GetNumberOfWorkingEngines();

        Debug.Log("Working Engines: " + numberOfEngines);

        for (int i = 1; i < numberOfEngines; i++)
        {
            newEngineSpeed *= tankEngineMultiplier;
        }

        if (numberOfEngines == 0)
            newEngineSpeed = 0;

        currentEngineMultiplier = newEngineSpeed;

        UpdateEngineSFX(numberOfEngines);
        UpdateTreadsSFX();
    }

    private void UpdateEngineSFX(int numberOfEngines)
    {
        //If there is at least one engine running, play the engine sound effect
        if(numberOfEngines > 0)
        {
            if(!FindObjectOfType<AudioManager>().IsPlaying("TankIdle"))
                FindObjectOfType<AudioManager>().Play("TankIdle", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        }
        //If not, stop the sound effect if it's currently playing
        else
        {
            if (FindObjectOfType<AudioManager>().IsPlaying("TankIdle"))
                FindObjectOfType<AudioManager>().Stop("TankIdle");
        }
    }

    public void UpdateTreadsSFX()
    {
        //If the current speed is stationary
        if (GetPlayerSpeed() * LevelManager.instance.gameSpeed == 0)
        {
            FindObjectOfType<AudioManager>().Stop("TreadsRolling");
        }
        //If the tank idle isn't already playing, play it
        else if (!FindObjectOfType<AudioManager>().IsPlaying("TreadsRolling"))
        {
            FindObjectOfType<AudioManager>().Play("TreadsRolling", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        }
    }

    public IEnumerator CollideWithEnemyAni(float collideVelocity, float seconds)
    {
        float timeElapsed = 0;
        currentSpeed = 0;

        //Deal damage to bottom layer
        GetLayerAt(0).DealDamage(10, false);

        //Shake camera on collision
        CameraEventController.instance.ShakeCamera(10f, seconds);

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

        if(this != null)
            StartCoroutine(ReturnToPosition());
    }

    private IEnumerator ReturnToPosition()
    {
        currentSpeed = speed;

        Debug.Log("Current Speed: " + currentSpeed * currentTankWeightMultiplier);

        while(transform.position.x < 0 && this != null)
        {
            transform.position += new Vector3(currentSpeed * currentTankWeightMultiplier, 0, 0) * Time.deltaTime;
            yield return null;
        }

        transform.position = Vector3.zero;
    }

    public List<LayerHealthManager> GetLayers()
    {
        return layers;
    }

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

    public void ResetTankDistance()
    {
        LevelManager.instance.StopCombatMusic();
        currentDistance = 0;
        LevelManager.instance.ShowGoPrompt();
    }

    private void OnDestroy()
    {
        if(FindObjectOfType<AudioManager>() != null)
        {
            FindObjectOfType<AudioManager>().Stop("TankIdle");
            if (FindObjectOfType<AudioManager>().IsPlaying("TreadsRolling"))
            {
                FindObjectOfType<AudioManager>().Stop("TreadsRolling");
            }

            FindObjectOfType<AudioManager>().PlayOneShot("LargeExplosionSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        }
    }
}
