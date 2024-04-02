using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    public float maxHealth;
    public float health;
    private SpriteMask[] damageMasks;

    public void Awake()
    {
        health = maxHealth;
        damageMasks = GetComponentsInChildren<SpriteMask>();
        if (damageMasks.Length > 0)
        {
            foreach(SpriteMask mask in damageMasks)
            {
                mask.enabled = false;
            }
        }
    }

    public void Damage(float amount)
    {
        health -= amount;
        if (health <= 0) Destroy(gameObject);

        EvaluateDamage();
    }

    private void EvaluateDamage()
    {
        float damageThresholdSegment = maxHealth / damageMasks.Length; //67
        for (int i = 0; i < damageMasks.Length; i++)
        {
            if ((i + 1) * Mathf.RoundToInt(damageThresholdSegment) > health)
            {
                damageMasks[i].enabled = true;
            }
        }
    }
}
