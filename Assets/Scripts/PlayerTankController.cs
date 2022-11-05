using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
    [SerializeField] private float speed = 4;
    private float currentSpeed;
    [SerializeField] private float tankWeightMultiplier = 0.8f;
    private float currentTankWeightMultiplier;
    [SerializeField] internal float tankBarrierRange = 12;

    private void Start()
    {
        currentSpeed = speed;
        currentTankWeightMultiplier = 1;
    }

    public float GetPlayerSpeed()
    {
        return currentSpeed * currentTankWeightMultiplier;
    }

    public void AdjustTankWeight(int numberOfLayers)
    {
        float newTankWeight = 1;

        //If the number of layers in the tank is 2 or less, there is no weight change
        if (numberOfLayers <= 2)
        {
            currentTankWeightMultiplier = 1;
            return;
        }

        //Add the multiplier to the tank weight for every additional layer the tank has gotten
        for(int i = 0; i < numberOfLayers - 2; i++)
        {
            newTankWeight *= tankWeightMultiplier; 
        }

        currentTankWeightMultiplier = newTankWeight;
    }

    public IEnumerator CollideWithEnemyAni(float collideVelocity, float seconds)
    {
        float timeElapsed = 0;
        currentSpeed = 0;

        //Deal damage to all layers
        foreach(var i in GetComponentsInChildren<LayerHealthManager>())
        {
            i.DealDamage(10);
        }

        //Shake camera on collision
        CameraEventController.instance.ShakeCamera(10f, seconds);

        while (timeElapsed < seconds && this != null)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            float newX = Mathf.Lerp(0, collideVelocity, t);
            transform.position -= new Vector3(newX * Time.deltaTime, 0, 0);

            timeElapsed += Time.deltaTime;

            yield return null;
        }

        timeElapsed = 0;

        while (timeElapsed < seconds && this != null)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            float newX = Mathf.Lerp(collideVelocity, 0, t);
            transform.position -= new Vector3(newX * Time.deltaTime, 0, 0);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        if(this != null)
            StartCoroutine(ReturnToPosition());
    }

    private IEnumerator ReturnToPosition()
    {
        currentSpeed = speed;

        Debug.Log("Current Speed: " + currentSpeed * currentTankWeightMultiplier);

        while(transform.position.x < 0 && this != null)
        {
            transform.position += new Vector3(currentSpeed * currentTankWeightMultiplier, 0, 0) * Time.deltaTime;
            yield return null;
        }

        transform.position = Vector3.zero;
    }
}
