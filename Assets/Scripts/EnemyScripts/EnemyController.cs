using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ENEMYBEHAVIOR {AGGRESSIVE, CALCULATING}

public class EnemyController : MonoBehaviour
{
    private enum MOVEMENTDIRECTION { DECELERATE, NEUTRAL, ACCELERATE}

    protected const float ENEMYLAYERSIZE = 7.65f;
    protected const int MAXLAYERS = 8;

    [SerializeField] protected float health = 100;
    [SerializeField] protected float speed = 1;
    [SerializeField] protected float collisionForce = 50;
    [SerializeField] protected float targetedDistance = 70;
    [SerializeField] protected float targetRange = 10;
    [SerializeField] protected float collisionForceSeconds = 0.1f;
    [SerializeField, Tooltip("The acceleration per second in which the tank changes direction.")] protected float changeDirectionAccelerationSpeed = 0.1f;
    [SerializeField] protected float maxAcceleration = 1.25f;
    [SerializeField, Tooltip("The amount of waves for the enemy to increase the amount of layers")] protected int wavesMultiplier = 1;
    [SerializeField] protected int onDestroyResources = 100;
    protected ENEMYBEHAVIOR enemyTrait;
    private MOVEMENTDIRECTION currentDirection;
    private MOVEMENTDIRECTION previousDirection;

    private float speedRelativeToPlayer;
    private float currentSpeed;
    private float currentRelativeSpeed;
    private float directionMultiplier;

    protected bool enemyColliding = false;

    protected PlayerTankController playerTank;

    [SerializeField] protected LayerHealthManager[] spawnableLayers; 
    protected int totalEnemyLayers;
    protected float waveCounter;

    // Start is called before the first frame update
    protected void Start()
    {
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
        currentSpeed = speed;
        directionMultiplier = 1;
        previousDirection = MOVEMENTDIRECTION.NEUTRAL;
        currentDirection = MOVEMENTDIRECTION.NEUTRAL;
        waveCounter = 1.0f / (wavesMultiplier * GameSettings.difficulty);
        CreateLayers();
        UpdateEnemySpeed();
        DetermineBehavior();
        FindObjectOfType<EnemySpawnManager>().AddToEnemyCounter(this);
    }

    protected virtual void CreateLayers()
    {
        float extraLayers = FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(0) * waveCounter;
        //Debug.Log("Extra Layers For Normal Tank #" + (FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(0) + 1).ToString() + ": " + extraLayers);
        totalEnemyLayers = 2 + Mathf.FloorToInt(extraLayers);
        totalEnemyLayers = Mathf.Clamp(totalEnemyLayers, 2, MAXLAYERS);

        //If the game is on debug mode, override the enemy layers
        if (GameSettings.debugMode)
            LayerSpawnDebugMode();

        LevelManager.instance.StartCombatMusic(totalEnemyLayers);

        bool specialLayerSpawned = false;

        for(int i = 0; i < totalEnemyLayers; i++)
        {
            int randomLayer;
            if (i % 2 == 1 && !specialLayerSpawned)
            {
                randomLayer = spawnableLayers.Length - 1;
            }
            else
            {
                if (i > 0 && i % 2 == 0)
                {
                    specialLayerSpawned = false;
                }
                if (specialLayerSpawned)
                    randomLayer = 0;
                else
                {
                    randomLayer = Random.Range(0, spawnableLayers.Length);
                    if (randomLayer != 0)
                        specialLayerSpawned = true;
                }
            }

            SpawnLayer(randomLayer, i);
        }

        foreach (var cannon in GetComponentsInChildren<CannonController>())
        {
            StartCoroutine(cannon.FireAtDelay());
        }
    }

    protected void LayerSpawnDebugMode()
    {
        switch (LevelManager.instance.currentRound)
        {
            case 1:
                totalEnemyLayers = 2;
                break;
            case 2:
                totalEnemyLayers = 4;
                break;
            case 3:
                totalEnemyLayers = 6;
                break;
            default:
                totalEnemyLayers = 8;
                break;
        }
    }

    protected void SpawnLayer(int index, int layerNum)
    {
        GameObject newLayer = Instantiate(spawnableLayers[index].gameObject);
        newLayer.transform.parent = transform;
        newLayer.transform.localPosition = new Vector2(0, layerNum * ENEMYLAYERSIZE);
        newLayer.transform.SetAsFirstSibling();
    }

    protected virtual void DetermineBehavior()
    {
        //int randomBehavior = Random.Range(0, System.Enum.GetValues(typeof(ENEMYBEHAVIOR)).Length - 1);
        int randomBehavior = 0;
        enemyTrait = (ENEMYBEHAVIOR)randomBehavior;
    }

    protected virtual void OnEnable()
    {

    }

    public void UpdateEnemySpeed()
    {
        speedRelativeToPlayer = ((playerTank.GetPlayerSpeed() * LevelManager.instance.gameSpeed) + currentSpeed) * directionMultiplier;

        //Debug.Log("Enemy Speed: " + speedRelativeToPlayer);
    }

    // Update is called once per frame
    protected void FixedUpdate()
    {
        if(LevelManager.instance.levelPhase != GAMESTATE.GAMEOVER)
        {
            if (!enemyColliding)
            {
                currentRelativeSpeed = -speedRelativeToPlayer;

                CheckBehaviorStates();
                UpdateDirection();
                UpdateCannons();

                //Move the enemy horizontally
                transform.position += new Vector3(currentRelativeSpeed, 0, 0) * Time.deltaTime;
            }
            else
            {
                //If the enemy is moving backwards, they are not colliding with the player tank
                if (speedRelativeToPlayer < 0)
                    enemyColliding = false;
            }
        }
    }

    private void CheckBehaviorStates()
    {
        switch (enemyTrait)
        {
            //Enemy aggressive behavior
            case ENEMYBEHAVIOR.AGGRESSIVE:
                directionMultiplier += changeDirectionAccelerationSpeed * Time.deltaTime;

                if (directionMultiplier > maxAcceleration)
                {
                    directionMultiplier = maxAcceleration;
                }
                break;

            //Enemy calculating behavior
            case ENEMYBEHAVIOR.CALCULATING:
                float currentDistance = transform.position.x - playerTank.transform.position.x;
               // Debug.Log("Distance From Player: " + currentDistance);

                //If the tank is too close to the player, back up
                if (currentDistance < targetedDistance - targetRange)
                {
                    currentDirection = MOVEMENTDIRECTION.DECELERATE;
                    previousDirection = MOVEMENTDIRECTION.DECELERATE;
                }
                //If the tank is too far from the player, speed up
                else if (currentDistance > targetedDistance + targetRange)
                {
                    currentDirection = MOVEMENTDIRECTION.ACCELERATE;
                    previousDirection = MOVEMENTDIRECTION.ACCELERATE;
                }
                else
                {
                    currentDirection = MOVEMENTDIRECTION.NEUTRAL;
                }
                break;
        }

        //Debug.Log("Current Direction Multiplier: " + directionMultiplier);
        //Debug.Log("State: " + currentDirection);
    }

    private void UpdateDirection()
    {
        switch(currentDirection)
        {
            case MOVEMENTDIRECTION.DECELERATE:
                directionMultiplier -= changeDirectionAccelerationSpeed * Time.deltaTime;

                if (directionMultiplier < -maxAcceleration)
                {
                    directionMultiplier = -maxAcceleration;
                }

                if (directionMultiplier > maxAcceleration)
                {
                    directionMultiplier = maxAcceleration;
                }
                break;
            case MOVEMENTDIRECTION.NEUTRAL:
                if(previousDirection == MOVEMENTDIRECTION.ACCELERATE)
                {
                    if (Mathf.Abs(directionMultiplier - 1) < 0.05f)
                        directionMultiplier = 1;
                    else if(directionMultiplier != 1)
                        directionMultiplier -= (changeDirectionAccelerationSpeed) * Time.deltaTime;
                }
                else if(previousDirection == MOVEMENTDIRECTION.DECELERATE)
                {
                    if (Mathf.Abs(directionMultiplier - (-1)) < 0.05f)
                        directionMultiplier = -1;
                    else if (directionMultiplier != -1)
                        directionMultiplier += (changeDirectionAccelerationSpeed) * Time.deltaTime;
                }
                break;
            case MOVEMENTDIRECTION.ACCELERATE:
                directionMultiplier += changeDirectionAccelerationSpeed * Time.deltaTime;

                if (directionMultiplier < -maxAcceleration)
                {
                    directionMultiplier = -maxAcceleration;
                }

                if (directionMultiplier > maxAcceleration)
                {
                    directionMultiplier = maxAcceleration;
                }
                break;
        }

        UpdateEnemySpeed();
    }

    private void UpdateCannons()
    {
        //Current target is the top layer
        Vector3 currentTarget = playerTank.GetLayerAt(LevelManager.instance.totalLayers - 1).transform.position;

        foreach(var i in GetComponentsInChildren<CannonController>())
        {
            i.CannonLookAt(currentTarget);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            Debug.Log("Enemy Is At Player!");
            enemyColliding = true;
            DetermineCollisionForce();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            enemyColliding = false;
        }
    }

    private void DetermineCollisionForce()
    {
        float enemyForce = collisionForce;
        float playerForce = collisionForce;

        //If the player is going slower than the enemy, add extra force to the player
        if(speedRelativeToPlayer > playerTank.GetPlayerSpeed() * LevelManager.instance.gameSpeed)
        {
            playerForce *= (playerTank.GetPlayerSpeed() * LevelManager.instance.gameSpeed) / speedRelativeToPlayer;
        }

        //If the enemy is going slower than the player, add extra force to the enemy
        else if(playerTank.GetPlayerSpeed() * LevelManager.instance.gameSpeed > speedRelativeToPlayer)
        {
            enemyForce *= (playerTank.GetPlayerSpeed() * LevelManager.instance.gameSpeed) / speedRelativeToPlayer;
        }

        StartCoroutine(CollideWithPlayerAni(enemyForce, collisionForceSeconds));
        StartCoroutine(playerTank.CollideWithEnemyAni(playerForce, collisionForceSeconds));
    }

    public void DealDamage(int dmg)
    {
        health -= dmg;

        Debug.Log("Enemy Tank Health: " + health);

/*        //Check for Game Over
        if (health <= 0)
        {
            Debug.Log("Enemy Tank Is Destroyed!");
            LevelManager.instance.currentSessionStats.wavesCleared += 1;
            LevelManager.instance.ResetPlayerCamera();
            AddToList();
            Destroy(gameObject, 0.1f);
        }*/
    }

    protected virtual void AddToList()
    {
        LevelManager.instance.currentSessionStats.normalTanksDefeated += 1;
    }

    IEnumerator CollideWithPlayerAni(float collideVelocity, float seconds)
    {
        float timeElapsed = 0;
        enemyColliding = true;

        while (timeElapsed < seconds)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            transform.position += new Vector3(Mathf.Lerp(0, collideVelocity, t) * Time.deltaTime, 0, 0);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        timeElapsed = 0;

        while (timeElapsed < seconds)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            transform.position += new Vector3(Mathf.Lerp(collideVelocity, 0, t) * Time.deltaTime, 0, 0);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        enemyColliding = false;
    }

    public void EnemyLayerDestroyed()
    {
        //Get rid of one of the layers
        totalEnemyLayers--;

        LevelManager.instance.UpdateResources(30);

        Debug.Log("Enemy Layers Left: " + totalEnemyLayers);

        //If there are no more layers, destroy the tank
        if (totalEnemyLayers == 0)
        {
            //Debug.Log("Enemy Tank Is Destroyed!");
            LevelManager.instance.UpdateResources(onDestroyResources);
            LevelManager.instance.currentSessionStats.wavesCleared += 1;
            LevelManager.instance.ResetPlayerCamera();
            AddToList();
            Destroy(gameObject, 0.1f);
        }
    }

    public bool IsEnemyCollidingWithPlayer()
    {
        return enemyColliding;
    }

    public int GetEnemyLayers()
    {
        return totalEnemyLayers;
    }

    public void SetEnemyLayers(int enemyLayers)
    {
        totalEnemyLayers = enemyLayers;
    }

    private void OnDestroy()
    {
        if (FindObjectOfType<EnemySpawnManager>() != null)
        {
            FindObjectOfType<EnemySpawnManager>().enemySpawnerActive = false;
            if(GameObject.FindGameObjectWithTag("PlayerTank") != null)
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().ResetTankDistance();
        }

        if(FindObjectOfType<AudioManager>() != null)
            FindObjectOfType<AudioManager>().PlayOneShot("MedExplosionSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
    }
}
