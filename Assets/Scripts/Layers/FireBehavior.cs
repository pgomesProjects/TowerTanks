using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBehavior : MonoBehaviour
{
    [SerializeField, Tooltip("The amount of time it takes for the fire to deal damage.")] private float fireTickSeconds;
    [SerializeField] private int damagePerTick = 5;
    public GameObject fireParticle;
    private GameObject[] currentParticles;
    private float currentTimer;
    private bool layerOnFire;

    private void OnEnable()
    {
        CreateFires(1);
        layerOnFire = true;
        currentTimer = 0;
        FindObjectOfType<AudioManager>().Play("FireBurningSFX", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume), gameObject);
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

        if(FindObjectOfType<AudioManager>() != null)
            FindObjectOfType<AudioManager>().Stop("FireBurningSFX", gameObject);
    }

    private void CreateFires(int firesToCreate)
    {
        float randomX = Random.Range(-5f, 5f);
        float randomY = Random.Range(-2f, 2f);

        for (int f = 0; f < firesToCreate; f++)
        {
            var childFire = Instantiate(fireParticle, new Vector3(transform.position.x + randomX, transform.position.y + randomY, transform.position.z), Quaternion.identity, this.transform);
            childFire.transform.localScale = Vector3.one;
        }
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
                    GetComponentInParent<LayerHealthManager>().DealDamage(damagePerTick, false);
                currentTimer = 0;
            }
        }
    }

    public bool IsLayerOnFire() => layerOnFire;
}
