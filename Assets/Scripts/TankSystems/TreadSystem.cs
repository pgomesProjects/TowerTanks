using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreadSystem : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("Rigidbody for affecting tank movement.")]                                                           internal Rigidbody2D r;
    [Tooltip("Wheels controlled by this system.")]                                                                private TreadWheel[] wheels;
    [SerializeField, Tooltip("Prefab which will be used to generate caterpillar treads (should be 1 unit long)")] private GameObject treadPrefab;
    [Tooltip("Array of all tread segments in system (one per wheel).")]                                           private Transform[] treads;

    //Settings:
    [Header("Debug Methods:")]
    [Range(-1 , 1)] public float debugDrive;
    [Min(0)] public float drivePower;

    //Runtime Variables:
    //NOTE: Make drag proportional to number of wheels touching the ground

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        r = GetComponent<Rigidbody2D>();                //Get rigidbody component
        wheels = GetComponentsInChildren<TreadWheel>(); //Get array of all wheels in system

        //Generate treads:
        List<Transform> newTreads = new List<Transform>(); //Instantiate list to store spawned treads
        for (int x = 0; x < wheels.Length; x++) //Iterate once for each wheel in tank
        {
            Transform newTread = Instantiate(treadPrefab, transform).transform; //Instantiate new tread object
            newTreads.Add(newTread);                                            //Add new tread to list
        }
        treads = newTreads.ToArray(); //Commit generated list to array
    }
    private void Update()
    {
        //Update treads:
        for (int wheelIndex = 0; wheelIndex < wheels.Length; wheelIndex++) //Iterate once for each wheel
        {
            int nextWheelIndex = wheelIndex + 1; if (nextWheelIndex == wheels.Length) nextWheelIndex = 0; //Get index for wheel after current one (wrap around at last wheel)
            Transform wheel1 = wheels[wheelIndex].transform;                                              //Get transform from first wheel
            Transform wheel2 = wheels[nextWheelIndex].transform;                                          //Get transform from second wheel
            float treadWidth = Vector2.Distance(wheel1.position, wheel2.position);                        //Tread width is the exact distance between both wheels
            Vector2 treadPos = Vector2.Lerp(wheel1.position, wheel2.position, 0.5f);                      //Position of tread starts exactly between both wheels
            Vector2 treadNormal = Vector2.Perpendicular(wheel1.position - wheel2.position).normalized;    //Get normal of tread so that it can be moved later (using difference in position between both wheels)
            treadPos += treadNormal * wheels[wheelIndex].radius;                                          //Move tread position to account for radius of wheel

            Transform tread = treads[wheelIndex];                                               //Get tread at current wheel index
            tread.position = treadPos;                                                          //Move tread to target position
            tread.eulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.up, treadNormal); //Rotate tread to target rotation
            tread.localScale = new Vector3(treadWidth, tread.localScale.y, 1);                  //Scale tread to target length
        }
    }
    private void FixedUpdate()
    {
        //Add wheel force:
        Vector2 avgWheelPos = Vector2.zero;     //System needs to get the average position of each wheel to determine drive characteristics
        Vector2 avgGroundNormal = Vector2.zero; //System needs to get the average directionality of each grounded wheel to determine drive characteristics
        int groundedWheels = 0;                 //System needs to track number of grounded wheels to get averages
        foreach (TreadWheel wheel in wheels) //Iterate through wheel list
        {
            if (wheel.grounded) //Only apply force from grounded wheels
            {
                //Get wheel data:
                avgWheelPos += (Vector2)wheel.transform.position; //Add wheel position to total
                avgGroundNormal += wheel.lastGroundNormal;        //Add ground normal to total
                groundedWheels++;                                 //Index grounded wheel counter

                //Apply wheel force:
                float suspensionMagnitude = wheel.settings.stiffnessCurve.Evaluate(wheel.compressionValue) * wheel.settings.stiffness; //Use wheel compression value and stiffness to determine magnitude of exerted force
                Vector2 suspensionForce = transform.up * suspensionMagnitude;                                                          //Get directional force to apply to rigidbody
                r.AddForceAtPosition(suspensionForce, wheel.transform.position, ForceMode2D.Force);                                    //Apply force to rigidbody at position of wheel
            }
        }
        avgWheelPos /= groundedWheels;     //Divide by wheel number to get average grounded wheel position
        avgGroundNormal /= groundedWheels; //Divide by wheel number to get average ground normal

        //Add drive force:
        if (Mathf.Abs(debugDrive) > 0.15f) //Add dead zone to debug drive stick so tank can be parked
        {
            float driveMagnitude = ((drivePower * -debugDrive) * groundedWheels) / wheels.Length; //Get magnitude of drive force from debug controls (also affected by number of wheels engaging with surface) //NOTE: Integrate into tank-controllable method later
            Vector2 driveForce = Vector2.Perpendicular(avgGroundNormal) * driveMagnitude;         //Get directional force based on normal of ground touched by wheels
            r.AddForceAtPosition(driveForce, avgWheelPos, ForceMode2D.Force);                     //Apply force to tank
        }
    }
}
