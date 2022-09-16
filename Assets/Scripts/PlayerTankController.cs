using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
    [SerializeField] private float health = 100;
    [SerializeField] private float speed = 5;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DealDamage(int dmg)
    {
        health -= dmg;

        Debug.Log("Player Tank Health: " + health);

        //Check for Game Over
        if(health <= 0)
        {
            Debug.Log("Tank Is Destroyed!");
        }
    }
}
