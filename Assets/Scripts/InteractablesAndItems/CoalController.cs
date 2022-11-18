using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoalController : InteractableController
{
    [SerializeField] [Tooltip("The amount of seconds it takes for the coal to fully deplete")] private float depletionSeconds;
    [SerializeField] private Slider coalPercentageIndicator;
    private float depletionRate;
    private float coalPercentage;
    private bool hasCoal;

    // Start is called before the first frame update
    void Start()
    {
        coalPercentage = 100;
        depletionRate = depletionSeconds / coalPercentage;
        coalPercentageIndicator.value = coalPercentage;
        hasCoal = true;

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
            float timeToFillCoal = 5;

            //If there is no coal in the furnace, make the task of refilling it twice as long
            if (!HasCoal())
                timeToFillCoal *= 2;

            //Start a progress bar on the player
            currentPlayer.StartProgressBar(timeToFillCoal, () => FillCoal(50));
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
            currentPlayer.CancelProgressBar();
        }
    }

    private void CoalDepletion()
    {
        //If the player is not in the tutorial, deplete coal
/*        if(LevelManager.instance.levelPhase != GAMESTATE.TUTORIAL)
        {
            //If the coal percentage is greater than 0, constantly deplete it
            if (coalPercentage > 0)
            {
                coalPercentage -= (1 / depletionRate) * Time.deltaTime;
                coalPercentageIndicator.value = coalPercentage;
                hasCoal = true;
            }
            else
            {
                Debug.Log("Coal Is Out!");
                hasCoal = false;
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
            }
        }*/

        //If the coal percentage is greater than 0, constantly deplete it
        if (coalPercentage > 0)
        {
            coalPercentage -= (1 / depletionRate) * Time.deltaTime;
            coalPercentageIndicator.value = coalPercentage;
            hasCoal = true;
        }
        else
        {
            Debug.Log("Coal Is Out!");
            hasCoal = false;
            GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
        }
    }

    public void AddCoal(float addToCoal)
    {
        //Add to the coal percentage
        coalPercentage += addToCoal;

        //If there is now coal, make sure to update the coal
        if(coalPercentage > 0)
        {
            hasCoal = true;
            GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
        }

        //Make sure the coal percentage does not pass 100%
        if(coalPercentage > 100)
        {
            coalPercentage = 100;
        }
    }

    public bool HasCoal()
    {
        return hasCoal;
    }

    private void OnDestroy()
    {
        hasCoal = false;
        if (GameObject.FindGameObjectWithTag("PlayerTank"))
        {
            GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
        }
    }
}
