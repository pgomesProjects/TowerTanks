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
        FindObjectOfType<AudioManager>().Play("FireBurningSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
    }

    private void OnDisable()
    {
        layerOnFire = false;
        if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.PUTOUTFIRE)
            {
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }

        FindObjectOfType<AudioManager>().Stop("FireBurningSFX");
    }

    // Update is called once per frame
    void Update()
    {
        if (layerOnFire)
        {
            currentTimer += Time.deltaTime;

            if(currentTimer > fireTickSeconds)
            {
                if(LevelManager.instance.levelPhase == GAMESTATE.GAMEACTIVE)
                    GetComponentInParent<LayerHealthManager>().DealDamage(damagePerTick);
                currentTimer = 0;
            }
        }
    }

    public bool IsLayerOnFire() => layerOnFire;
}
