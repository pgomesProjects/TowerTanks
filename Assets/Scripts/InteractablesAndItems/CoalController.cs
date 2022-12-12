using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoalController : InteractableController
{
    [SerializeField] [Tooltip("The amount of seconds it takes for the coal to fully deplete")] private float depletionSeconds;
    [SerializeField] private Slider coalPercentageIndicator;
    [SerializeField] private int framesForCoalFill = 5;
    [SerializeField] private float angleRange = 102;
    private int currentCoalFrame;
    private float currentIndicatorAngle;
    private float depletionRate;
    private float coalPercentage;
    private bool hasCoal;
    private Animator engineAnimator;

    private float coalLoadAudioLength = 1.5f;

    private Transform indicatorPivot;

    // Start is called before the first frame update
    void Start()
    {
        engineAnimator = GetComponentInChildren<Animator>();

        if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            coalPercentage = 0f;
        }
        else
        {
            coalPercentage = 100f;
            hasCoal = true;
        }

        currentCoalFrame = 0;
        depletionRate = depletionSeconds / 100f;
        coalPercentageIndicator.value = coalPercentage;
        indicatorPivot = transform.Find("IndicatorPivot");

        FindObjectOfType<PlayerTankController>().AdjustEngineSpeedMultiplier();

        AdjustIndicatorAngle();
    }

    // Update is called once per frame
    void Update()
    {
        engineAnimator.SetFloat("Percentage", coalPercentage);

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

            currentCoalFrame = 0;
        }
    }

    public void ProgressCoalFill()
    {
        //If there is a player
        if (currentPlayer != null)
        {
            currentPlayer.AddToProgressBar(100f / framesForCoalFill);
            currentCoalFrame++;

            AudioManager audio = FindObjectOfType<AudioManager>();
            float totalAudioTime = audio.GetSoundLength("LoadingCoal");

            float startAudioTime;
            float endAudioTime;

            startAudioTime = (coalLoadAudioLength / framesForCoalFill) * (currentCoalFrame - 1);
            endAudioTime = (coalLoadAudioLength / framesForCoalFill) * currentCoalFrame;

            Debug.Log("Total Audio Time: " + totalAudioTime);

            Debug.Log("Start Audio Time: " + startAudioTime);
            Debug.Log("End Audio Time: " + endAudioTime);

            if (currentPlayer.IsProgressBarFull())
            {
                FillCoal(15f);
                currentPlayer.ShowProgressBar();
                currentCoalFrame = 0;

                startAudioTime = coalLoadAudioLength;
                endAudioTime = totalAudioTime;
            }

            audio.PlayAtSection("LoadingCoal", PlayerPrefs.GetFloat("SFXVolume", 0.5f), startAudioTime, endAudioTime);
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
            currentCoalFrame = 0;
        }
    }

    private void AdjustIndicatorAngle()
    {
        currentIndicatorAngle = -angleRange + ((angleRange * 2f) - (angleRange * 2f * (coalPercentage / 100f)));
        currentIndicatorAngle = Mathf.Clamp(currentIndicatorAngle, -angleRange, angleRange);

        indicatorPivot.eulerAngles = new Vector3(0, 0, currentIndicatorAngle);
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
                //coalPercentageIndicator.value = coalPercentage;
                hasCoal = true;
            }
            else
            {
                Debug.Log("Coal Is Out!");
                hasCoal = false;
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
                FindObjectOfType<AudioManager>().PlayOneShot("EngineDyingSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
            }

            AdjustIndicatorAngle();
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

        AdjustIndicatorAngle();

        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.ADDFUEL)
            {
                //coalPercentageIndicator.value = coalPercentage;
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
        currentCoalFrame = 0;
        if (GameObject.FindGameObjectWithTag("PlayerTank"))
        {
            GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().AdjustEngineSpeedMultiplier();
        }
    }
}
