using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

    [SerializeField] private float health = 100;
    [SerializeField] private float speed = 5;

    private bool inRangeOfPlayer = false;

    private Rigidbody2D rb;
    private PlayerTankController player;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<PlayerTankController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!inRangeOfPlayer){
        //Move the enemy horizontally
        rb.velocity = new Vector2(-speed, rb.velocity.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            Debug.Log("Enemy Is At Player!");
            inRangeOfPlayer = true;
            rb.velocity = new Vector2(0, 0);
            //Deal Damage To Player Tank Every 0.5 Seconds
            StartCoroutine(DealDamage());
        }
    }

    IEnumerator DealDamage()
    {
        while (true)
        {
            player.DealDamage(5);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
