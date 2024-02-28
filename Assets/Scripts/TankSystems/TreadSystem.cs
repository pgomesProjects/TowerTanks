using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreadSystem : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("Controller for this tread's parent tank.")]                                                         private TankController tankController;
    [Tooltip("Rigidbody for affecting tank movement.")]                                                           internal Rigidbody2D r;
    [Tooltip("Wheels controlled by this system.")]                                                                private TreadWheel[] wheels;
    [SerializeField, Tooltip("Prefab which will be used to generate caterpillar treads (should be 1 unit long)")] private GameObject treadPrefab;
    [Tooltip("Array of all tread segments in system (one per wheel).")]                                           private Transform[] treads;

    //Settings:
    [Header("Center of Gravity Settings:")]
    [Tooltip("Height at which center of gravity is locked (higher heights make tank more wobbly).")] public float COGHeight;
    [Tooltip("Extents of center of gravity (affects how far tank can lean).")]                       public float COGWidth;
    [Header("Drive Settings:")]
    [Tooltip("Maximum motor torque exerted by tread motor (speed and power)"), Min(0)]                               public float drivePower;
    [Tooltip("Value used for T = 1 in drivePowerCurve. Should functionally be vehicle's max speed."), Min(0)]        public float optimalDriveSpeed;
    [Tooltip("Curve describing torque increase and falloff depending on motor speed")]                               public AnimationCurve drivePowerCurve;
    [Header("Traction Settings:")]
    [Tooltip("Angular drag when all (non-extra) wheels are on the ground."), Min(0)]                                 public float maxAngularDrag;
    [Tooltip("Amount of linear drag exerted by treads (against ground normal) when engine is not engaged."), Min(0)] public float traction;
    [Tooltip("How many wheels are by default off the ground."), Min(0)]                                              public int extraWheels;
    [Header("Debug Controls:")]
    [Range(-1 , 1)] public float debugDrive;
    

    //Runtime Variables:
    //NOTE: Make drag proportional to number of wheels touching the ground

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        tankController = GetComponentInParent<TankController>(); //Get tank controller object from parent
        r = GetComponent<Rigidbody2D>();                         //Get rigidbody component
        wheels = GetComponentsInChildren<TreadWheel>();          //Get array of all wheels in system

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
        float driveMagnitude = (drivePower * -debugDrive) / (wheels.Length - extraWheels); //Get force being exerted by each grounded drive wheel
        if (Mathf.Abs(debugDrive) < 0.15f) driveMagnitude = 0;                             //Add dead zone to debug drive controller

        int groundedWheels = 0; //Initialize variable to track how many wheels are grounded
        foreach (TreadWheel wheel in wheels) //Iterate through wheel list
        {
            if (wheel.grounded) //Only apply force from grounded wheels
            {
                //Apply suspension force:
                float suspensionMagnitude = wheel.settings.stiffnessCurve.Evaluate(wheel.compressionValue) * wheel.settings.stiffness; //Use wheel compression value and stiffness to determine magnitude of exerted force
                Vector2 suspensionForce = transform.up * suspensionMagnitude;                                                          //Get directional force to apply to rigidbody
                r.AddForceAtPosition(suspensionForce, wheel.transform.position, ForceMode2D.Force);                                    //Apply force to rigidbody at position of wheel
                groundedWheels++;                                                                                                      //Indicate that this wheel is grounded

                if (wheel.lastGroundHit.collider != null) //Wheel has valid information about hit ground
                {
                    //Apply drive torque:
                    if (driveMagnitude != 0) //Tank is currently being driven
                    {
                        Vector2 driveForce = Vector2.Perpendicular(wheel.lastGroundHit.normal) * driveMagnitude; //Get force being applied by this wheel
                        r.AddForceAtPosition(driveForce, wheel.lastGroundHit.point, ForceMode2D.Force);          //Add force at wheel's position of contact
                    }

                    //Apply drag:

                }
            }
        }

        //Add drag:
        groundedWheels = Mathf.Min(groundedWheels, wheels.Length - extraWheels);              //Cap grounded wheels in case extras would push number over calculated maximum
        r.angularDrag = maxAngularDrag * Mathf.Min((float)groundedWheels / wheels.Length, 1); //Make angular drag proportional to number of grounded (non-extra) wheels
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetComponent<Rigidbody2D>().worldCenterOfMass, 0.2f);
    }

    //UTILITY METHODS:
    /// <summary>
    /// Evaluates mass and center of gravity for tank depending on position and quantity of cells.
    /// </summary>
    public void ReCalculateMass()
    {
        //Initialization:
        int cellCount = 0;                  //Initialize value to store number of cells counted by evaluation
        Vector2 avgCellPos = new Vector2(); //Initialize value to store average cell position

        //Cell roundup:
        foreach (Room room in tankController.rooms) //Iterate through each room in tank
        {
            foreach (Cell cell in room.cells) //Iterate through each cell in room
            {
                cellCount++;                                    //Add to cell count
                avgCellPos += (Vector2)cell.transform.position; //Add local position of cell (relative to tank) to average
            }
        }

        //Calculation:
        avgCellPos /= cellCount; //Get average position of cells
        //NOTE: Add cell mass calc here
        r.centerOfMass = transform.InverseTransformPoint(avgCellPos); //Set rigidbody center of mass
    }
}
