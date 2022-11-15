using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillEnemyController : EnemyController
{
    protected override void CreateLayers()
    {
        totalEnemyLayers = 2;
    }

    protected override void DetermineBehavior()
    {
        enemyTrait = ENEMYBEHAVIOR.AGGRESSIVE;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerTank"))
        {
            Debug.Log("Enemy Is At Player!");
            enemyColliding = true;
        }
    }
}
