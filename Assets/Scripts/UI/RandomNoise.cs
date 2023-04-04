using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomNoise : MonoBehaviour
{
    [SerializeField, Range(0, 600)] private float minNoiseIntervalSeconds = 60;
    [SerializeField, Range(0, 600)] private float maxNoiseIntervalSeconds = 120;
    [SerializeField] private string soundName;

    private float currentTimer;
    private float timeToPlaySound;

    private void Start()
    {
        timeToPlaySound = Random.Range(minNoiseIntervalSeconds, maxNoiseIntervalSeconds);   
    }

    // Update is called once per frame
    void Update()
    {
        if(currentTimer > timeToPlaySound)
        {
            PlaySound();
            currentTimer = 0;
            timeToPlaySound = Random.Range(minNoiseIntervalSeconds, maxNoiseIntervalSeconds);
        }
        else
            currentTimer += Time.deltaTime;
    }

    private void PlaySound()
    {
        FindObjectOfType<AudioManager>().Play(soundName);
    }
}
