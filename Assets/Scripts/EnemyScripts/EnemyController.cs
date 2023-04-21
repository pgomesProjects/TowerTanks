using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ENEMYBEHAVIOR {AGGRESSIVE, CALCULATING}

public class EnemyController : MonoBehaviour
{
    private enum MOVEMENTDIRECTION { DECELERATE, NEUTRAL, ACCELERATE}

    protected const float ENEMYLAYERSIZE = 7.65f;   //The y spacing between enemy layers
    protected const int MAXLAYERS = 8;  //The maximum amount of layers an enemy tank can have

    [Header("Enemy Settings")]
    [SerializeField, Tooltip("The base speed for the enemy tank.")] protected float speed = 1;
    [SerializeField, Tooltip("The maximum speed for the enemy tank.")] protected float maximumSpeed = 1.25f;
    [SerializeField, Tooltip("The acceleration per second in which the enemy can gain speed.")] protected float accelerationRate;
    [SerializeField, Tooltip("The force provided when colliding with the player tank.")] protected float collisionForce = 50;
    [SerializeField, Tooltip("The ideal distance for a calculated enemy tank to be from the player.")] protected float targetedDistance = 70;
    [SerializeField, Tooltip("The range buffer of the targeted distance.")] protected float targetRange = 10;
    [SerializeField, Tooltip("The elapsed amount of time it takes for a collision to start and end.")] protected float collisionForceSeconds = 0.1f;
    [SerializeField, Tooltip("The amount of waves for the enemy to increase the amount of layers")] protected int wavesMultiplier = 1;
    [SerializeField, Tooltip("The amount of resources given to the player when the entire tank is destroyed.")] protected int onDestroyResources = 100;
    [SerializeField] protected LayerManager spawnableLayer;    //The layer that a potential enemy tank could spawn
    [Space(10)]

    [Header("Debug Options")]
    [SerializeField, Tooltip("Automatically destroys the enemy.")] private bool debugDestroyEnemy;

    protected ENEMYBEHAVIOR enemyTrait; //the behavior trait of the enemy tank
    private float currentSpeed; //The current speed of the enemy tank
    private float directionMultiplier;  //Multiplies the distance to change the direction of the enemy tank's movement
    protected float combatDirectionMultiplier; //Multiplies the speed to change the global direction of the enemy tank's movement

    protected bool enemyColliding = false;  //If true, the enemy is colliding with a player tank. If false, they are not.

    protected PlayerTankController playerTank;  //The player tank

    protected int totalEnemyLayers; //The total amount of enemy layers
    protected float waveCounter;    //The current game wave

    protected bool canMove; //If true, the enemy tank can move. If false, they cannot move.

    private void Awake()
    {
        waveCounter = 1.0f / (wavesMultiplier * GameSettings.difficulty);
    }

    // Start is called before the first frame update
    protected void Start()
    {
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();

        currentSpeed = speed;
        canMove = true;
        directionMultiplier = 1;

        DetermineBehavior();
        FindObjectOfType<EnemySpawnManager>().AddToEnemyCounter(this);
    }

    /// <summary>
    /// Creates layers for the enemy tank.
    /// </summary>
    public virtual void CreateLayers(COMBATDIRECTION enemyDirection, int debugEnemyLayers = 0)
    {
        if (debugEnemyLayers <= 0)
        {
            float extraLayers = FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(0) * waveCounter;
            //Debug.Log("Extra Layers For Normal Tank #" + (FindObjectOfType<EnemySpawnManager>().GetEnemyCountAt(0) + 1).ToString() + ": " + extraLayers);
            totalEnemyLayers = 2 + Mathf.FloorToInt(extraLayers);
        }
        else
            totalEnemyLayers = debugEnemyLayers;

        totalEnemyLayers = Mathf.Clamp(totalEnemyLayers, 2, MAXLAYERS);

        LevelManager.instance.StartCombatMusic(totalEnemyLayers);

        bool specialLayerSpawned = false;

        for(int i = 0; i < totalEnemyLayers; i++)
        {
            int randomLayer;
            if (i % 2 == 1 && !specialLayerSpawned)
            {
                randomLayer = 1;
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
                    randomLayer = Random.Range(0, 1);
                    if (randomLayer != 0)
                        specialLayerSpawned = true;
                }
            }

            SpawnLayer(randomLayer, i, enemyDirection);
        }

        foreach (var cannon in GetComponentsInChildren<EnemyCannonController>())
        {
            StartCoroutine(cannon.FireAtDelay());
        }

        //If the enemy is to the left of the player, reverse the direction variable
        if (enemyDirection == COMBATDIRECTION.Left)
            combatDirectionMultiplier = -1f;
        else
            combatDirectionMultiplier = 1f;
    }

    /// <summary>
    /// Spawns a layer on the enemy tank.
    /// </summary>
    /// <param name="index">The type of layer to spawn on the tank.</param>
    /// <param name="layerNum">The current layer number being spawned.</param>
    /// <param name="enemyDirection">The direction that the enemy spawns at relative to the player.</param>
    protected void SpawnLayer(int index, int layerNum, COMBATDIRECTION enemyDirection)
    {
        GameObject newLayer = Instantiate(spawnableLayer.gameObject);
        newLayer.transform.parent = transform;
        newLayer.transform.localPosition = new Vector2(0, layerNum * ENEMYLAYERSIZE);
        newLayer.transform.SetAsFirstSibling();

        if (index == 1)
            SpawnWeapon(newLayer.GetComponent<LayerManager>(), enemyDirection);
    }

    /// <summary>
    /// Spawns a weapon on the left or right of the enemy.
    /// </summary>
    protected virtual void SpawnWeapon(LayerManager currentLayerManager, COMBATDIRECTION enemyDirection)
    {
        //Debug.Log("Spawn Cannon!");

        switch (enemyDirection)
        {
            case COMBATDIRECTION.Left:
                currentLayerManager.GetCannons().GetChild(1).gameObject.SetActive(true);
                break;
            case COMBATDIRECTION.Right:
                currentLayerManager.GetCannons().GetChild(0).gameObject.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Decides the behavior of the enemy tank.
    /// </summary>
    protected virtual void DetermineBehavior()
    {
        int randomBehavior = Random.Range(0, 1);
        enemyTrait = (ENEMYBEHAVIOR)randomBehavior;
    }

    protected virtual void OnEnable()
    {

    }

    // Update is called once per frame
    protected void Update()
    {
        if (Application.isEditor)
        {
            if (debugDestroyEnemy)
            {
                debugDestroyEnemy = false;
                OnEnemyKill();
            }
        }
    }

    protected void FixedUpdate()
    {
        if(LevelManager.instance.levelPhase != GAMESTATE.GAMEOVER)
        {
            if (!enemyColliding)
            {
                //currentRelativeSpeed = -speedRelativeToPlayer;

                CheckBehaviorStates();
                UpdateCannons();

                //Move the enemy horizontally
                transform.position += new Vector3(-GetDirectionalSpeed(), 0, 0) * Time.deltaTime;
            }
            else
            {
                if (!canMove)
                    CreateCollision();
            }
        }
    }

    private void CreateCollision()
    {
        float playerSpeed = playerTank.GetPlayerSpeed();

        //If the enemy is going right
        if (-GetDirectionalSpeed() < 0)
        {
            if (-GetDirectionalSpeed() <= playerSpeed)
            {
                transform.position += new Vector3(playerSpeed, 0, 0) * Time.deltaTime;
            }
            else
                transform.position += new Vector3(-GetDirectionalSpeed(), 0, 0) * Time.deltaTime;
        }
        //If the enemy is going left
        else
        {
            if (-GetDirectionalSpeed() >= playerSpeed)
            {
                transform.position += new Vector3(playerSpeed, 0, 0) * Time.deltaTime;
            }
            else
                transform.position += new Vector3(-GetDirectionalSpeed(), 0, 0) * Time.deltaTime;
        }
    }

    private float GetDirectionalSpeed() => currentSpeed * directionMultiplier * combatDirectionMultiplier;

    /// <summary>
    /// Checks to see how the enemy tank will behave based on its behavior.
    /// </summary>
    private void CheckBehaviorStates()
    {
        switch (enemyTrait)
        {
            //Enemy aggressive behavior
            case ENEMYBEHAVIOR.AGGRESSIVE:
                AccelerateTank();
                break;

            //Enemy calculating behavior
            case ENEMYBEHAVIOR.CALCULATING:
                float currentDistance = transform.position.x - playerTank.transform.position.x;
               // Debug.Log("Distance From Player: " + currentDistance);

                //If the tank is too close to the player, back up
                if (currentDistance < targetedDistance - targetRange)
                {
                    Debug.Log("Backing Up...");
                    directionMultiplier = -1f;
                    AccelerateTank();
                }
                //If the tank is too far from the player, speed up
                else if(currentDistance > targetedDistance + targetRange)
                {
                    Debug.Log("Moving Forward...");
                    directionMultiplier = 1f;
                    AccelerateTank();
                }
                else
                {
                    Debug.Log("In Target Range...");
                    DecelerateTank();
                }
                break;
        }
    }

    /// <summary>
    /// Accelerates the tank to its maximum speed.
    /// </summary>
    private void AccelerateTank()
    {
        if (currentSpeed >= maximumSpeed)
        {
            currentSpeed = maximumSpeed;
        }
        else
            currentSpeed += accelerationRate * Time.deltaTime;
    }

    /// <summary>
    /// Decelerates the tank until it stops.
    /// </summary>
    private void DecelerateTank()
    {
        if (currentSpeed <= 0)
        {
            currentSpeed = 0;
        }
        else
            currentSpeed -= accelerationRate * Time.deltaTime;
    }

    /// <summary>
    /// Update any cannons on the enemy tank so that they point towards one of the player tank's layers
    /// </summary>
    private void UpdateCannons()
    {
        //Current target is the top layer
        Vector3 currentTarget = playerTank.GetLayerAt(LevelManager.instance.totalLayers - 1).transform.position;
        currentTarget.y -= PlayerTankController.PLAYER_TANK_LAYER_HEIGHT / 2f;

        foreach(var i in GetComponentsInChildren<EnemyCannonController>())
        {
            i.AimAtTarget(currentTarget);
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("PlayerTankCollider"))
        {
            Debug.Log("Enemy Is At Player!");
            enemyColliding = true;
            DetermineCollisionForce();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("PlayerTankCollider"))
            canMove = false;
    }

    protected virtual void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("PlayerTankCollider"))
        {
            enemyColliding = false;
            canMove = true;
        }
    }

    /// <summary>
    /// Determines the collision force of the enemy and the player tank on the moment that they collide with each other.
    /// </summary>
    private void DetermineCollisionForce()
    {
        float enemyForce = collisionForce;
        float playerForce = collisionForce;

        float enemyAbsSpeed = Mathf.Abs(-GetDirectionalSpeed());
        float playerAbsSpeed = Mathf.Abs(playerTank.GetPlayerSpeed());

        float playerSpeed = playerTank.GetPlayerSpeed();

        //If the enemy is going right
        if (-GetDirectionalSpeed() < 0)
        {
            if (-GetDirectionalSpeed() <= playerTank.GetPlayerSpeed())
                enemyForce *= playerAbsSpeed / enemyAbsSpeed;
            else
                playerForce *= enemyAbsSpeed / playerAbsSpeed;
        }
        //If the enemy is going left
        else
        {
            if (-GetDirectionalSpeed() >= playerTank.GetPlayerSpeed())
                enemyForce *= playerAbsSpeed / enemyAbsSpeed;
            else
                playerForce *= enemyAbsSpeed / playerAbsSpeed;
        }

        Debug.Log("Enemy Force: " + enemyForce * combatDirectionMultiplier);
        Debug.Log("Player Force: " + playerForce * -combatDirectionMultiplier);

        StartCoroutine(CollideWithPlayerAni(enemyForce * combatDirectionMultiplier, collisionForceSeconds));
        StartCoroutine(playerTank.CollideWithEnemyAni(playerForce * -combatDirectionMultiplier, collisionForceSeconds));
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

    /// <summary>
    /// The event that occurs when an enemy layer is destroyed.
    /// </summary>
    public void EnemyLayerDestroyed()
    {
        //Get rid of one of the layers
        totalEnemyLayers--;

        LevelManager.instance.UpdateResources(30);

        //Debug.Log("Enemy Layers Left: " + totalEnemyLayers);

        //If there are no more layers, destroy the tank
        if (totalEnemyLayers == 0)
        {
            OnEnemyKill();
        }
    }

    private void OnEnemyKill()
    {
        //Debug.Log("Enemy Tank Is Destroyed!");
        LevelManager.instance.UpdateResources(onDestroyResources);
        LevelManager.instance.currentSessionStats.wavesCleared += 1;

        CameraEventController.instance.ResetCameraShake();
        LevelManager.instance.ResetPlayerCamera();
        CameraEventController.instance.ShakeCamera(10f, 1f);

        AddToList();
        transform.SetParent(null);
        Destroy(gameObject, 0.1f);
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

    public float GetCombatDirectionMultiplier() => combatDirectionMultiplier;

    private void OnDestroy()
    {
        if (FindObjectOfType<EnemySpawnManager>() != null && FindObjectOfType<EnemySpawnManager>().AllEnemiesGone())
        {
            FindObjectOfType<EnemySpawnManager>().enemySpawnerActive = false;
            if(GameObject.FindGameObjectWithTag("PlayerTank") != null)
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().ResetTankDistance();
        }

        if(FindObjectOfType<AudioManager>() != null)
            FindObjectOfType<AudioManager>().Play("MedExplosionSFX", gameObject);

        CameraEventController.instance.RemoveOnDestroy(gameObject);
    }
}
