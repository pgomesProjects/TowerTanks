using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoalController : InteractableController
{
    [SerializeField] [Tooltip("The amount of seconds it takes for the coal to fully deplete")] private float depletionSeconds;
    [SerializeField] private Slider coalPercentageIndicator;
    [SerializeField] private int framesForCoalFill = 5;
    private float depletionRate;
    private float coalPercentage;
    private bool hasCoal;

    // Start is called before the first frame update
    void Start()
    {
        if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            coalPercentage = 0f;
        }
        else
        {
            coalPercentage = 100f;
            hasCoal = true;
        }

        depletionRate = depletionSeconds / 100f;
        coalPercentageIndicator.value = coalPercentage;

        FindObjectOfType<PlayerTankController>().AdjustEngineSpeedMultiplier();
    }

    // Update is called once per frame
    void Update()
    {
        //If there is coal, use it
        if (hasCoal)
        {
            CoalDepletion();
        }
    }

    /// <summary>
    /// Fills coal so that the tank can continue moving
    /// </summary>
    public void StartCoalFill()
    {
        //If there is a player
        if (currentPlayer != null)
        {
            if (currentPlayer.IsProgressBarActive())
            {
                currentPlayer.HideProgressBar();
            }
            else
            {
                currentPlayer.ShowProgressBar();
            }
        }
    }

    public void ProgressCoalFill()
    {
        //If there is a player
        if (currentPlayer != null)
        {
            currentPlayer.AddToProgressBar(100f / framesForCoalFill);

            if (currentPlayer.IsProgressBarFull())
            {
                FillCoal(15f);
                currentPlayer.ShowProgressBar();
            }
        }
    }

    private void FillCoal(float percent)
    {
        //Add a percentage of the necessary coal to the furnace
        Debug.Log("Coal Has Been Added To The Furnace!");
        AddCoal(percent);
    }

    /// <summary>
    /// Cancels the filling of coal when the player lets go of the interact button
    /// </summary>
    public void CancelCoalFill()
    {
        //If there is a player
        if (currentPlayer != null)
        {
            LockPlayer(false);
        }
    }

    private void CoalDepletion()
    {
        //If the player is not in the tutorial, deplete coal
        if(LevelManager.instance.levelPhase == GAMESTATE.GAMEACTIVE)
        {
            //If the coal percentage is greater than 0, constantly deplete it
            if (coalPercentage > 0f)
            {
                coalPercentage -= (1f / depletionRate) * Time.deltaTime;
                coalPercentageIndicator.value = coalPercentage;
                hasCoal = true;
            }
            else
            {
                Debug.Log("Coal Is Out!");
                hasCoal = false;
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
                FindObjectOfType<AudioManager>().PlayOneShot("EngineDyingSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
            }
        }
    }

    public void AddCoal(float addToCoal)
    {
        //Add to the coal percentage
        coalPercentage += addToCoal;

        //If there is now coal, make sure to update the coal
        if(coalPercentage > 0f)
        {
            hasCoal = true;
            GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
        }

        //Make sure the coal percentage does not pass 100%
        if(coalPercentage > 100f)
        {
            coalPercentage = 100f;
        }

        if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.ADDFUEL)
            {
                coalPercentageIndicator.value = coalPercentage;
                if (IsCoalFull())
                {
                    TutorialController.main.OnTutorialTaskCompletion();
                }
            }
        }
    }

    public bool HasCoal()
    {
        return hasCoal;
    }

    public bool IsCoalFull() => coalPercentage >= 100f;

    private void OnDestroy()
    {
        hasCoal = false;
        if (GameObject.FindGameObjectWithTag("PlayerTank"))
        {
            GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
        }
    }
}
