using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBehavior : MonoBehaviour
{
    [SerializeField, Tooltip("The amount of time it takes for the fire to deal damage.")] private float fireTickSeconds;
    [SerializeField] private int damagePerTick = 5;
    private float currentTimer;
    private bool layerOnFire;

    private void OnEnable()
    {
        layerOnFire = true;
        currentTimer = 0;
    }

    private void OnDisable()
    {
        layerOnFire = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (layerOnFire)
        {
            currentTimer += Time.deltaTime;

            if(currentTimer > fireTickSeconds)
            {
                GetComponentInParent<LayerHealthManager>().DealDamage(damagePerTick);
                currentTimer = 0;
            }
        }
    }

    public bool IsLayerOnFire() => layerOnFire;
}
