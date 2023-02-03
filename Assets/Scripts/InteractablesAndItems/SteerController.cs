using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteerController : InteractableController
{
    [SerializeField] private AnimationCurve steerAniCurve;
    private IEnumerator steeringCoroutine;
    private const float steeringAniSeconds = 0.25f;

    // Start is called before the first frame update
    void Start()
    {
        steeringCoroutine = CheckForSteeringInput();
        transform.Find("LeverPivot").localRotation = Quaternion.Euler(0, 0, -(20 * LevelManager.instance.gameSpeed));
    }


    public void ChangeSteering()
    {
        if (interactionActive)
        {
            LevelManager.instance.isSteering = true;
            StartCoroutine(steeringCoroutine);
        }
        else
        {
            LevelManager.instance.isSteering = false;
            StopCoroutine(steeringCoroutine);
        }
    }

    IEnumerator CheckForSteeringInput()
    {
        while (true)
        {
            if (!LevelManager.instance.steeringStickMoved)
            {
                //Moving stick left
                if (currentPlayerLockedIn.steeringValue < -0.5f && LevelManager.instance.levelPhase != GAMESTATE.TUTORIAL)
                {
                    if (LevelManager.instance.speedIndex > (int)TANKSPEED.REVERSEFAST)
                    {
                        UpdateSteerLever(-1);
                        yield return new WaitForSeconds(steeringAniSeconds);
                    }
                }
                //Moving stick right
                else if (currentPlayerLockedIn.steeringValue > 0.5f)
                {
                    if (LevelManager.instance.speedIndex < (int)TANKSPEED.FORWARDFAST)
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
        LevelManager.instance.steeringStickMoved = true;

        float elapsedTime = 0;

        Quaternion startingRot = transform.Find("LeverPivot").localRotation;

        float startGameSpeed = LevelManager.instance.currentSpeed[LevelManager.instance.speedIndex];
        float endGameSpeed = LevelManager.instance.currentSpeed[LevelManager.instance.speedIndex + direction];

        Quaternion endingRot = Quaternion.Euler(0, 0, -(20 * LevelManager.instance.currentSpeed[LevelManager.instance.speedIndex + direction]));

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

            LevelManager.instance.UpdateSpeed(Mathf.Lerp(startGameSpeed, endGameSpeed, t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        LevelManager.instance.UpdateSpeed(LevelManager.instance.speedIndex + direction);

        foreach (var i in GameObject.FindGameObjectsWithTag("Throttle"))
        {
            i.transform.Find("LeverPivot").localRotation = endingRot;
        }

        FindObjectOfType<AudioManager>().PlayOneShot("ThrottleShift", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        LevelManager.instance.steeringStickMoved = false;
    }
}
