using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrottleController : TankInteractable
{
    //Objects & Components
    [Tooltip("Transforms to spawn particles from when used."), SerializeField] private Transform[] particleSpots;
    [Tooltip("Joint around which throttle shaft rotates."), SerializeField] private Transform pivot;

    //Settings:
    [Header("Throttle Settings:")]
    private float maxAngle = 40f; //maximum rotation throttle shaft can shift to
    private float currentAngle = 0f; //current rotation throttle shaft is set to
    private float previousAngle = 0f; //used for throttle animation lerp
    private float shiftTimer; //how fast the throttle lerps
 
    public int speedSettings; //number of different speed settings in either direction
    public int currentSpeed;
    private int gear; //what gear the throttle is currently in (default: neutral (0))

    [Header("Debug Controls:")]
    public bool shiftRight;
    public bool shiftLeft;

    //Runtime variables
   
    // RUNTIME METHODS:
    void Update()
    {
        UpdateThrottle();

        //Debug 
        if (shiftLeft) { shiftLeft = false; UseThrottle(-1); }
        if (shiftRight) { shiftRight = false; UseThrottle(1); }

        currentSpeed = -gear;
    }

    private void UpdateThrottle()
    {
        if (shiftTimer > 0)
        {
            shiftTimer -= Time.deltaTime;
            if (shiftTimer < 0) shiftTimer = 0;
        }
        float updatedAngle = Mathf.Lerp(currentAngle, previousAngle, (shiftTimer / 0.1f));
        pivot.localEulerAngles = new Vector3(0, 0, updatedAngle); //updates the throttle shaft to its correct rotation

        if (tank != null) tank.treadSystem.debugDrive = -(updatedAngle / maxAngle);
    }

    public void UseThrottle(int direction) //called from operator -> sends message to tankController to change gears in all throttles
    {
        if (tank != null) tank.ChangeAllGear(direction);
    }


    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Shifts the throttle one gear in given direction (-1 = left, 0 = don't shift, 1 = right)
    /// </summary>
    public void ChangeGear(int direction) //called from TankController
    {
        previousAngle = gear * (maxAngle / speedSettings);
        gear -= direction;
        if (Mathf.Abs(gear) > speedSettings)
        {
            gear += direction;
            GameManager.Instance.AudioManager.Play("ThrottleShift");
        }
        else
        {
            //other effects
            GameManager.Instance.AudioManager.Play("ThrottleClick"); //Play shift audioclip
        }
        currentAngle = gear * (maxAngle / speedSettings);

        shiftTimer = 0.1f;
        if (tank != null)  tank.treadSystem.gear = -gear;
    }
}
