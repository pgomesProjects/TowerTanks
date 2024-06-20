using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCannonBrain : MonoBehaviour
{
    private GunController gunScript;
    public float fireCooldown;
    private float fireTimer;
    public float aimCooldown;
    private float aimTimer;

    public bool isRotating;
    private float currentForce = 0;

    private void Awake()
    {
        gunScript = GetComponent<GunController>();
    }

    void Start()
    {
        fireTimer = 0;
        aimTimer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (fireTimer < fireCooldown) fireTimer += Time.deltaTime;
        else {
            gunScript.Fire();
            float randomOffset = Random.Range(-2f, 2f);
            fireTimer = 0 + randomOffset;
        }

        if (aimTimer < aimCooldown) aimTimer += Time.deltaTime;
        else
        {
            float randomForce = Random.Range(-1.2f, 1.2f);
            //StartCoroutine(AimCannon(randomForce));

            float randomOffset = Random.Range(-2f, 2f);
            aimTimer = 0 + randomOffset;
        }

        if (isRotating)
        {
            gunScript.RotateBarrel(currentForce, false);
        }
    }

    public IEnumerator AimCannon(float force)
    {
        currentForce = 1.2f * Mathf.Sign(force);
        isRotating = true;
        yield return new WaitForSeconds(1.0f);
        isRotating = false;
    }
}
