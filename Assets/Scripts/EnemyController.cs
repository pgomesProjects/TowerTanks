using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ENEMYTYPE {NORMAL, DRILL, MORTAR}
public enum ENEMYBEHAVIOR {AGGRESSIVE, CALCULATING}

public class EnemyController : MonoBehaviour
{
    [SerializeField] private ENEMYTYPE enemyType;
    [SerializeField] private float health = 100;
    [SerializeField] private float speed = 1;
    [SerializeField] private float collisionForce = 50;
    [SerializeField] private float collisionForceSeconds = 0.1f;
    private ENEMYBEHAVIOR enemyTrait;

    private const float minCalcDist = 60;
    private const float maxCalcDist = 85;

    private float speedRelativeToPlayer;
    private float currentSpeed;
    private float currentRelativeSpeed;
    private float directionMultiplier;

    private bool enemyColliding = false;

    private PlayerTankController playerTank;

    private int totalEnemyLayers;

    // Start is called before the first frame update
    void Start()
    {
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
        totalEnemyLayers = 2;
        currentSpeed = speed;
        directionMultiplier = 1;
        UpdateEnemySpeed();
        DetermineBehavior();
    }

    private void DetermineBehavior()
    {
        switch (enemyType)
        {
            //Randomly choose between aggressive or calculating
            case ENEMYTYPE.NORMAL:
                //int randomBehavior = Random.Range(0, System.Enum.GetValues(typeof(ENEMYBEHAVIOR)).Length - 1);
                int randomBehavior = 1;
                enemyTrait = (ENEMYBEHAVIOR)randomBehavior;
                break;
            //Always aggressive
            case ENEMYTYPE.DRILL:
                enemyTrait = ENEMYBEHAVIOR.AGGRESSIVE;
                break;
            //Always calculating
            case ENEMYTYPE.MORTAR:
                enemyTrait = ENEMYBEHAVIOR.CALCULATING;
                break;
        }
    }

    private void OnEnable()
    {
        //Deal Damage To Player
        StartCoroutine(DealDamage());
    }

    public void UpdateEnemySpeed()
    {
        speedRelativeToPlayer = ((playerTank.GetPlayerSpeed() * LevelManager.instance.gameSpeed) + currentSpeed) * directionMultiplier;
        //Debug.Log("Enemy Speed: " + speedRelativeToPlayer);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!enemyColliding)
        {
            currentRelativeSpeed = -speedRelativeToPlayer;

            CheckBehaviorStates();

            //Move the enemy horizontally
            transform.position += new Vector3(currentRelativeSpeed, 0, 0) * Time.deltaTime;
        }
    }

    private void CheckBehaviorStates()
    {
        directionMultiplier = 1;

        switch (enemyTrait)
        {
            //Enemy aggressive behavior
            case ENEMYBEHAVIOR.AGGRESSIVE:
                break;

            //Enemy calculating behavior
            case ENEMYBEHAVIOR.CALCULATING:
                float currentDistance = transform.position.x - playerTank.transform.position.x;
                Debug.Log("Distance From Player: " + currentDistance);
                //If the tank is too close to the player, back up
                if(currentDistance < minCalcDist)
                {
                    directionMultiplier = -2;
                    UpdateEnemySpeed();
                }
                //If the tank is too far from the player, speed up
                else if(currentDistance > maxCalcDist)
                {
                    directionMultiplier = 2;
                    UpdateEnemySpeed();
                }
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            Debug.Log("Enemy Is At Player!");

            DetermineCollisionForce();
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

        //Check for Game Over
        if (health <= 0)
        {
            Debug.Log("Enemy Tank Is Destroyed!");
            Destroy(gameObject);
        }
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

    IEnumerator DealDamage()
    {
        while (true)
        {
            //If the enemy has a cannon, fire the cannon
            if (GetComponentInChildren<CannonController>() != null)
            {
                Debug.Log("Enemy Fire!");
                GetComponentInChildren<CannonController>().Fire();
            }

            //Shoot at the player every 15 to 25 seconds
            int timeWait = Random.Range(15, 25);

            yield return new WaitForSeconds(timeWait);
        }
    }

    public void EnemyLayerDestroyed()
    {
        //Get rid of one of the layers
        totalEnemyLayers--;

        LevelManager.instance.UpdateResources(30);

        //If there are no more layers, destroy the tank
        if (totalEnemyLayers == 0)
        {
            LevelManager.instance.UpdateResources(100);
            Destroy(gameObject);
        }
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
            FindObjectOfType<EnemySpawnManager>().GetReadyForEnemySpawn();
        }
    }
}
