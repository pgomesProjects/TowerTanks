using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteerController : InteractableController
{

    private IEnumerator steeringCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        steeringCoroutine = CheckForSteeringInput();
        UpdateSteerLever();
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
            Debug.Log(currentPlayerColliding.steeringValue);

            //Moving stick left
            if (currentPlayerColliding.steeringValue < -0.01f)
            {
                if (LevelManager.instance.speedIndex > (int)TANKSPEED.REVERSEFAST)
                {
                    LevelManager.instance.UpdateSpeed(-1);
                    UpdateSteerLever();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            //Moving stick right
            else if (currentPlayerColliding.steeringValue > 0.01f)
            {
                if (LevelManager.instance.speedIndex < (int)TANKSPEED.FORWARDFAST)
                {
                    LevelManager.instance.UpdateSpeed(1);
                    UpdateSteerLever();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield return null;
        }
    }

    private void UpdateSteerLever()
    {
        Transform leverPivot = transform.Find("LeverPivot");

        if (leverPivot != null)
        {
            leverPivot.localRotation = Quaternion.Euler(0, 0, -(20 * LevelManager.instance.gameSpeed));
        }
    }
}
