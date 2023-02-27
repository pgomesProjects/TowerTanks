using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillController : MonoBehaviour
{
    [SerializeField] private float drillTickSeconds = 2;
    [SerializeField] private int damagePerTick = 5;
    [SerializeField] private int chanceForFire = 10;
    [SerializeField] private LayerMask drillLayerMask;
    private float currentTimer;

    private Vector3 drawRayPos;
    [SerializeField] private GameObject sparks;

    // Start is called before the first frame update
    void Start()
    {
        drawRayPos = new Vector3(transform.position.x - (transform.localScale.x / 2), transform.position.y, transform.position.z);
        currentTimer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //If the drill is colliding with the player tank
        if (GetComponentInParent<EnemyController>().IsEnemyCollidingWithPlayer())
        {
            //Check to see what the drill is hitting
            RaycastHit2D[] hits = Physics2D.RaycastAll(drawRayPos, Vector3.left * GetComponentInParent<EnemyController>().GetCombatDirectionMultiplier(), float.MaxValue, drillLayerMask);

            foreach (var hit in hits)
            {
                Instantiate(sparks, transform.position, Quaternion.identity);
                //If the drill is hitting a layer of the tank, deal damage to that layer
                if (hit.collider.CompareTag("Layer") && hit.collider.GetComponentInParent<LayerHealthManager>() != null)
                {
                    currentTimer += Time.deltaTime;

                    //Deal damage every tick
                    if (currentTimer > drillTickSeconds)
                    {
                        Debug.Log("Drill Damaging " + hit.collider.transform.parent.name + "!");
                        hit.collider.GetComponentInParent<LayerHealthManager>().DealDamage(damagePerTick, true, 3f);
                        hit.collider.GetComponentInParent<LayerHealthManager>().CheckForFireSpawn(chanceForFire);
                        currentTimer = 0;
                    }

                    break;
                }

            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 drayRaySelect = new Vector3(transform.position.x - (transform.localScale.x / 2), transform.position.y, transform.position.z);

        Debug.DrawRay(drayRaySelect, Vector3.left, Color.red);
    }
}
