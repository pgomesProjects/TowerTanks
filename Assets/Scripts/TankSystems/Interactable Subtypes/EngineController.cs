using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineController : TankInteractable
{
    //Objects & Components
    [Tooltip("Transforms to spawn particles from when used."), SerializeField] private Transform[] particleSpots;

    //Settings:
    [Header("Engine Settings:")]
    public int maxCoal;
    private int coal;
    [Tooltip("How long it takes for the engine to burn 1 coal"), SerializeField] public float coalBurnRate;
    private float coalTimer;
    private float smokePuffRate = 1f;
    private float smokePuffTimer = 0;

    [Header("Debug Controls:")]
    public bool loadCoal;

    //Runtime variables

    private void Start()
    {
        coalTimer = coalBurnRate;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug settings:
        if (loadCoal) { loadCoal = false; LoadCoal(1); }

        if (coal > 0)
        {

            coalTimer -= Time.deltaTime;
            if (coalTimer <= 0)
            {
                coal -= 1;
                if (coal <= 0)
                {
                    GameManager.Instance.AudioManager.Play("EngineDyingSFX"); //Play engine dying clip
                }
                coalTimer = coalBurnRate;
            }


            smokePuffTimer -= Time.deltaTime;
            if (smokePuffTimer <= 0)
            {
                GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[0], 0.1f, null);
                smokePuffTimer = smokePuffRate;
            }
        }
        else
        {
            smokePuffTimer = 0;
            coalTimer = coalBurnRate;
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Loads (amount) coal into the engine.
    /// </summary>
    public void LoadCoal(int amount)
    {
        //Increase coal total:
        coal += amount;
        if (coal > maxCoal)
        {
            GameManager.Instance.AudioManager.Play("InvalidAlert"); //Can't do that, sir
        }
        else
        {
            //Other effects:
            GameManager.Instance.ParticleSpawner.SpawnParticle(3, particleSpots[0], 0.1f, null);
            GameManager.Instance.AudioManager.Play("CoalLoad"); //Play loading clip
        }
    }
}
