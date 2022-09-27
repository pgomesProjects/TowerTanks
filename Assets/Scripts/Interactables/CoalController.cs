using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoalController : MonoBehaviour
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

    private void CoalDepletion()
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
            LevelManager.instance.hasFuel = false;
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
            LevelManager.instance.hasFuel = true;
        }

        //Make sure the coal percentage does not pass 100%
        if(coalPercentage > 100)
        {
            coalPercentage = 100;
        }
    }
}
