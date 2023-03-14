using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteerController : InteractableController
{
    [SerializeField] private AnimationCurve steerAniCurve;
    private IEnumerator steeringCoroutine;
    private const float steeringAniSeconds = 0.25f;

    private PlayerTankController playerTank;

    // Start is called before the first frame update
    void Start()
    {
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
        steeringCoroutine = CheckForSteeringInput();
        transform.Find("LeverPivot").localRotation = Quaternion.Euler(0, 0, -(20 * playerTank.GetThrottleMultiplier()));
    }


    public void ChangeSteering()
    {
        if (interactionActive)
        {
            StartCoroutine(steeringCoroutine);
        }
        else
        {
            StopCoroutine(steeringCoroutine);
        }
    }

    IEnumerator CheckForSteeringInput()
    {
        while (true && currentPlayerLockedIn != null)
        {
            if (!playerTank.IsSteeringMoved())
            {
                //Moving stick left
                if (currentPlayerLockedIn.steeringValue < -0.5f && LevelManager.instance.levelPhase != GAMESTATE.TUTORIAL)
                {
                    if (playerTank.GetCurrentThrottleOption() > (int)TANKSPEED.REVERSEFAST)
                    {
                        UpdateSteerLever(-1);
                        yield return new WaitForSeconds(steeringAniSeconds);
                    }
                }
                //Moving stick right
                else if (currentPlayerLockedIn.steeringValue > 0.5f)
                {
                    if (playerTank.GetCurrentThrottleOption() < (int)TANKSPEED.FORWARDFAST)
                    {
                        if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
                        {
                            //If the tutorial calls to move the throttle, move the throttle
                            if (TutorialController.main.currentTutorialState == TUTORIALSTATE.MOVETHROTTLE)
                            {
                                UpdateSteerLever(1);
                                yield return new WaitForSeconds(steeringAniSeconds);

                                //Tell tutorial that task is complete
                                TutorialController.main.OnTutorialTaskCompletion();
                            }
                        }
                        else
                        {
                            UpdateSteerLever(1);
                            yield return new WaitForSeconds(steeringAniSeconds);
                        }
                    }
                }
            }
            yield return null;
        }
    }

    public void UpdateSteerLever(int dir)
    {
        StartCoroutine(SteerLeverAnim(steeringAniSeconds, dir));
    }

    private IEnumerator SteerLeverAnim(float seconds, int direction)
    {
        playerTank.SetSteeringMoved(true);

        float elapsedTime = 0;

        Quaternion startingRot = transform.Find("LeverPivot").localRotation;

        float startGameSpeed = PlayerTankController.throttleSpeedOptions[playerTank.GetCurrentThrottleOption()];
        float endGameSpeed = PlayerTankController.throttleSpeedOptions[playerTank.GetCurrentThrottleOption() + direction];

        Quaternion endingRot = Quaternion.Euler(0, 0, -(20 * PlayerTankController.throttleSpeedOptions[playerTank.GetCurrentThrottleOption() + direction]));

        //Play sound effect
        FindObjectOfType<AudioManager>().PlayOneShot("ThrottleClick", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        while (elapsedTime < seconds && direction != 0)
        {
            //Animation curve to make the lever feel smoother
            float t = steerAniCurve.Evaluate(elapsedTime / seconds);

            foreach(var i in GameObject.FindGameObjectsWithTag("Throttle"))
            {
                i.transform.Find("LeverPivot").localRotation = Quaternion.Lerp(startingRot, endingRot, t);
            }

            playerTank.UpdateSpeed(Mathf.Lerp(startGameSpeed, endGameSpeed, t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        playerTank.UpdateSpeed(playerTank.GetCurrentThrottleOption() + direction);

        foreach (var i in GameObject.FindGameObjectsWithTag("Throttle"))
        {
            i.transform.Find("LeverPivot").localRotation = endingRot;
        }

        FindObjectOfType<AudioManager>().PlayOneShot("ThrottleShift", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        playerTank.SetSteeringMoved(false);
    }
}
