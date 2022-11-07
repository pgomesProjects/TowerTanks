using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ENEMYTYPE {NORMAL, DRILL}

public class EnemyController : MonoBehaviour
{
    [SerializeField] private ENEMYTYPE enemyType;
    [SerializeField] private float health = 100;
    [SerializeField] private float speed = 1;
    [SerializeField] private float collisionForce = 50;
    [SerializeField] private float collisionForceSeconds = 0.1f;
    private float speedRelativeToPlayer;

    private bool enemyColliding = false;

    private PlayerTankController playerTank;

    private int totalEnemyLayers;

    // Start is called before the first frame update
    void Start()
    {
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
        totalEnemyLayers = 2;
        UpdateEnemySpeed();
    }

    private void OnEnable()
    {
        //Deal Damage To Player
        StartCoroutine(DealDamage());
    }

    public void UpdateEnemySpeed()
    {
        speedRelativeToPlayer = (playerTank.GetPlayerSpeed() * LevelManager.instance.gameSpeed) + speed;
        //Debug.Log("Enemy Speed: " + speedRelativeToPlayer);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!enemyColliding)
        {
            float currentSpeed = -speedRelativeToPlayer;

            //Move the enemy horizontally
            transform.position += new Vector3(currentSpeed, 0, 0) * Time.deltaTime;
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
