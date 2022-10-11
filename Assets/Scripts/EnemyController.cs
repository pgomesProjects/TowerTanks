using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    [SerializeField] private float health = 100;
    [SerializeField] private float speed = 1;
    private float speedRelativeToPlayer;

    private bool enemyLockedIn = false;

    private Rigidbody2D rb;
    private PlayerTankController player;

    private int totalEnemyLayers;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<PlayerTankController>();
        totalEnemyLayers = 2;
        UpdateEnemySpeed();
    }

    public void UpdateEnemySpeed()
    {
        speedRelativeToPlayer = (FindObjectOfType<PlayerTankController>().GetPlayerSpeed() * LevelManager.instance.gameSpeed) + speed;
        Debug.Log("Enemy Speed: " + speedRelativeToPlayer);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!enemyLockedIn)
        {
            float currentSpeed = -speedRelativeToPlayer;

            //Move the enemy horizontally
            //rb.velocity = new Vector2(currentSpeed, rb.velocity.y);

            transform.position += new Vector3(currentSpeed, 0, 0) * Time.deltaTime;
        }
        else
        {
            //If the player is moving backwards, don't lock the enemy
            if (speedRelativeToPlayer < 0)
            {
                enemyLockedIn = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            Debug.Log("Enemy Is At Player!");
            enemyLockedIn = true;
            rb.velocity = new Vector2(0, 0);
            //Deal Damage To Player
            StartCoroutine(DealDamage());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            Debug.Log("Enemy Is No Longer At Player!");

            //Deal Damage To Player
            StopCoroutine(DealDamage());
        }
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

            //Shoot at the player every 6 to 10 seconds
            int timeWait = Random.Range(6, 10);

            yield return new WaitForSeconds(timeWait);
        }
    }

    public void EnemyLayerDestroyed()
    {
        //Get rid of one of the layers
        totalEnemyLayers--;

        //If there are no more layers, destroy the tank
        if(totalEnemyLayers == 0)
            Destroy(gameObject);
    }

    public int GetEnemyLayers()
    {
        return totalEnemyLayers;
    }

    public void SetEnemyLayers(int enemyLayers)
    {
        totalEnemyLayers = enemyLayers;
    }
}
